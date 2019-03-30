using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace CraftWar
{
    public class TileType
    {
        public static List<TileType> tileTypes = new List<TileType>();

        //Object
        public Texture2D[] textures;
        public string name;
        public short breakTime;
        public Texture2D[] breakOverlay;
        public bool breakable;
        public ItemType tileItemType;
        public ItemType droppedItemType;
        public TileMaterial tileMaterial;
        public bool lightSource;
        public byte radianceLevel;
        public bool collidable;
        public bool requiresSupport;
        public int millisecondsBetweenFrames;
        public EventHandler OnInteraction;
        public int tileTypeID;

        public TileType(Texture2D[] textures, string name, short breakTime, Texture2D[] breakOverlay, TileMaterial tileMaterial, EventHandler OnInteraction = null, int millisecondsBetweenFrames = 0, bool requiresSupport = false, bool collidable = true, bool lightSource = false, byte radianceLevel = 0, ItemType droppedItemType = null, bool breakable = true, object[,,] craftingRecipe = null, int amountFromCraft = 1)
        {
            this.textures = textures;
            this.name = name;
            this.breakTime = breakTime;
            this.breakOverlay = breakOverlay;
            this.breakable = breakable;
            this.tileMaterial = tileMaterial;
            this.lightSource = lightSource;
            this.radianceLevel = radianceLevel;
            this.collidable = collidable;
            this.requiresSupport = requiresSupport;
            this.millisecondsBetweenFrames = millisecondsBetweenFrames;
            
            if (OnInteraction == null)
            {
                this.OnInteraction = new EventHandler((sender, e) => { });
            }
            else
            {
                this.OnInteraction = OnInteraction;
            }

            //Create item type and action
            ItemType.ItemAction rightClickAction = () =>
            {
                if (Game1.currentMap.isConnectedTile(Game1.currentMap.positionSelected, requiresSupport))
                {
                    Game1.currentMap.tileMap[Game1.currentMap.positionSelected[0], Game1.currentMap.positionSelected[1]] = new Tile(this, Game1.currentMap.positionSelected, Game1.currentMap.tileSize, collidable: collidable, requiresSupport: requiresSupport);

                    //Remove decorative tiles
                    if (collidable) Game1.currentMap.decorativeTileMap[Game1.currentMap.positionSelected[0], Game1.currentMap.positionSelected[1]].Clear();

                    //Update server
                    string tileSerialized = JsonConvert.SerializeObject(Game1.currentMap.tileMap[Game1.currentMap.positionSelected[0], Game1.currentMap.positionSelected[1]]);
                    Game1.networkManager.messagesToSendToServer += ((int)GameServer.NetworkKeyword.mapInfo).ToString() + GameServer.dataSeparator + ((int)GameServer.NetworkKeyword.tileInfo).ToString() + GameServer.dataSeparator + ((int)GameServer.NetworkKeyword.tileChange).ToString() + GameServer.dataSeparator + Game1.mainPlayer.playerGUI.playerInventory.selectedCell.items[0].itemType.name + GameServer.dataSeparator + tileSerialized + GameServer.messageSeparator;

                    //Update lighting
                    LightingManager.updateSurroundingTiles(Game1.currentMap.positionSelected);

                    return true;
                }

                return false;
            };
            tileItemType = new TileItemType(name, this, Color.White, rightClickAction: rightClickAction, craftingRecipe: craftingRecipe, amountMadeFromCraft: amountFromCraft);

            if (droppedItemType == null)
            {
                this.droppedItemType = tileItemType;
            }
            else
            {
                this.droppedItemType = droppedItemType;
            }

            //Add to registry
            tileTypes.Add(this);
            //Give tiletype ID
            tileTypeID = tileTypes.Count - 1;
        }   
    }

    public class TileMaterial
    {
        public float friction;

        public TileMaterial(float friction)
        {
            this.friction = friction;
        }
    }

    public class Tile
    {
        public const int particlesFromBreak = 3;
        public const float maxParticleFromBreakVelocity = .035F;
        public const int particleSize = 6;
        public const int particleSizeVariation = 2;

        //Object
        [JsonIgnore]
        public TileType tileType;
        public Rectangle rectangle;
        public int[] mapPosition;
        public short currentBreakTime = 0;
        public Texture2D currentBreakOverlay = null;
        public bool terrain;
        public bool collidable;
        public bool overridable;
        public bool requiresSupport;
        public bool canSupport;
        public bool interactedThisTick = false;
        [JsonIgnore]
        public EventHandler OnBreak;
        public LightingManager lightingManager;

        public Tile(TileType tileType, int[] mapPosition, int tileSize, bool terrain = false, bool collidable = true, bool overridable = false, bool requiresSupport = false, bool canSupport = true)
        {
            this.tileType = tileType;
            this.mapPosition = mapPosition;
            this.terrain = terrain;
            this.collidable = collidable;
            this.requiresSupport = requiresSupport;
            this.overridable = overridable;
            rectangle = new Rectangle(mapPosition[0] * tileSize, mapPosition[1] * tileSize, tileSize, tileSize);
            OnBreak = new EventHandler((sender, e) => { });
            this.canSupport = canSupport;
            if (tileType != null)
            {
                lightingManager = new LightingManager(mapPosition, tileType.lightSource);
                lightingManager.radianceLevel = tileType.radianceLevel;
            }          

            if (Game1.gameTime != null)
            {
                millisecondsFromLastFrame = (uint)Game1.gameTime.TotalGameTime.TotalMilliseconds;
            }
            else
            {
                millisecondsFromLastFrame = 0;
            }
        }

        public void draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(tileType.textures[textureArrayLocation], rectangle, lightingManager.tileColor);

            if (currentBreakOverlay != null)
            {
                spriteBatch.Draw(currentBreakOverlay, rectangle, Color.DarkGray);
            }
        }

        public void attemptBreakTile(GameTime gameTime, Player player)
        {
            if (tileType.breakable)
            {
                //Adds elapsed time of attempted break
                currentBreakTime += (short)(gameTime.ElapsedGameTime.Milliseconds * Game1.mainPlayer.tileDestructionMultiplier);

                if (currentBreakTime >= tileType.breakTime)
                {
                    //Tile has been broken
                    breakTile(true, player);
                }
                else
                {
                    //Gives corresponding overlay
                    int overlayIncrement = tileType.breakTime / tileType.breakOverlay.Length;
                    for (int i = 0; i < tileType.breakOverlay.Length; i++)
                    {
                        if (currentBreakTime > overlayIncrement * i)
                        {
                            if (currentBreakOverlay != tileType.breakOverlay[i])
                            {
                                currentBreakOverlay = tileType.breakOverlay[i];
                                //Update to server
                                Game1.networkManager.messagesToSendToServer += ((int)GameServer.NetworkKeyword.mapInfo).ToString() + GameServer.dataSeparator + ((int)GameServer.NetworkKeyword.tileInfo).ToString() + GameServer.dataSeparator + ((int)GameServer.NetworkKeyword.tileBreakOverlay).ToString() + GameServer.dataSeparator + mapPosition[0] + GameServer.dataSeparator + mapPosition[1] + GameServer.dataSeparator + i + GameServer.messageSeparator;
                            }
                        }
                    }
                }
            }
        }

        private void removeDependentSurfaceTiles()
        {
            if (mapPosition[0] - 1 >= 0)
            {
                List<SurfaceTile> surfaceTilesToRemove = new List<SurfaceTile>();
                foreach (SurfaceTile b in Game1.currentMap.decorativeTileMap[mapPosition[0] - 1, mapPosition[1]])
                {
                    if (b.supportDependence == SurfaceTile.SupportDependence.Right)
                    {
                        surfaceTilesToRemove.Add(b);
                    }
                }
                foreach (SurfaceTile b in surfaceTilesToRemove)
                {
                    Game1.currentMap.decorativeTileMap[mapPosition[0] - 1, mapPosition[1]].Remove(b);
                }
            }
            if (mapPosition[0] + 1 < Game1.currentMap.tileMap.GetLength(0))
            {
                List<SurfaceTile> surfaceTilesToRemove = new List<SurfaceTile>();
                foreach (SurfaceTile b in Game1.currentMap.decorativeTileMap[mapPosition[0] + 1, mapPosition[1]])
                {
                    if (b.supportDependence == SurfaceTile.SupportDependence.Left)
                    {
                        surfaceTilesToRemove.Add(b);
                    }
                }
                foreach (SurfaceTile b in surfaceTilesToRemove)
                {
                    Game1.currentMap.decorativeTileMap[mapPosition[0] + 1, mapPosition[1]].Remove(b);
                }
            }
            if (mapPosition[1] - 1 >= 0)
            {
                List<SurfaceTile> surfaceTilesToRemove = new List<SurfaceTile>();
                foreach (SurfaceTile b in Game1.currentMap.decorativeTileMap[mapPosition[0], mapPosition[1] - 1])
                {
                    if (b.supportDependence == SurfaceTile.SupportDependence.Down)
                    {
                        surfaceTilesToRemove.Add(b);
                    }
                }
                foreach (SurfaceTile b in surfaceTilesToRemove)
                {
                    Game1.currentMap.decorativeTileMap[mapPosition[0], mapPosition[1] - 1].Remove(b);
                }
            }
            if (mapPosition[1] + 1 < Game1.currentMap.tileMap.GetLength(1))
            {
                List<SurfaceTile> surfaceTilesToRemove = new List<SurfaceTile>();
                foreach (SurfaceTile b in Game1.currentMap.decorativeTileMap[mapPosition[0], mapPosition[1] + 1])
                {
                    if (b.supportDependence == SurfaceTile.SupportDependence.Up)
                    {
                        surfaceTilesToRemove.Add(b);
                    }
                }
                foreach (SurfaceTile b in surfaceTilesToRemove)
                {
                    Game1.currentMap.decorativeTileMap[mapPosition[0], mapPosition[1] + 1].Remove(b);
                }
            }
        }

        public void breakTile(bool brokenByPlayer, Player playerBrokenBy = null)
        {
            currentBreakTime = 0;
            currentBreakOverlay = null;

            //Remove dependent surface tile
            removeDependentSurfaceTiles();

            if (brokenByPlayer)
            {
                if (playerBrokenBy != null)
                {
                    playerBrokenBy.breakTile();
                }

                //Update server
                Game1.networkManager.messagesToSendToServer += ((int)GameServer.NetworkKeyword.mapInfo).ToString() + GameServer.dataSeparator + ((int)GameServer.NetworkKeyword.tileInfo).ToString() + GameServer.dataSeparator + ((int)GameServer.NetworkKeyword.tileChange).ToString() + GameServer.dataSeparator + mapPosition[0] + GameServer.dataSeparator + mapPosition[1] + GameServer.dataSeparator + ((int)GameServer.NetworkKeyword.tileNull).ToString() + GameServer.dataSeparator + ((int)GameServer.NetworkKeyword.falseIdentifier).ToString() + GameServer.messageSeparator;

                //Drop to map
                if (tileType.droppedItemType != null)
                {
                    Game1.currentMap.entityAddQueue.Add(new Drop(new Item[] { new Item(tileType.droppedItemType) }, new RigidBody(Drop.massesOfPlayer), new Vector2(rectangle.Center.X, rectangle.Center.Y)));
                }

                Random random = Game1.currentMap.syncedRandom;
                for (int i = 0; i < particlesFromBreak; i++)
                {
                    Game1.currentMap.particleAddQueue.Add(new TileParticle(random.Next(particleSize - particleSizeVariation, particleSize + particleSizeVariation + 1), new Vector2(rectangle.Center.X, rectangle.Center.Y), tileType.textures[textureArrayLocation], 2000, random, initialVelocity: new Vector2(random.Next(-1, 2) * maxParticleFromBreakVelocity, random.Next(-1, 2) * maxParticleFromBreakVelocity)));
                }
            }
            if (terrain)
            {
                //Add background tile
                Game1.currentMap.backgroundTileMap[mapPosition[0], mapPosition[1]] = new BackgroundTile(tileType.textures[0], mapPosition);
            }
            Game1.currentMap.tileMap[mapPosition[0], mapPosition[1]] = null;

            OnBreak.Invoke(this, null);

            //Check tile above for requiring support
            if (mapPosition[1] - 1 >= 0 && Game1.currentMap.tileMap[mapPosition[0], mapPosition[1] - 1] != null)
            {
                //Check
                if (Game1.currentMap.tileMap[mapPosition[0], mapPosition[1] - 1].requiresSupport)
                {
                    if (brokenByPlayer)
                    {
                        Game1.currentMap.tileMap[mapPosition[0], mapPosition[1] - 1].breakTile(true, playerBrokenBy);
                    }
                    else
                    {
                        Game1.currentMap.tileMap[mapPosition[0], mapPosition[1] - 1].breakTile(false);
                    }
                }
            }

            if (brokenByPlayer)
            {
                //Update lighting
                LightingManager.updateSurroundingTiles(mapPosition);
            }
        }

        public int textureArrayLocation = 0;
        private uint millisecondsFromLastFrame;
        public void updateAnimation(GameTime gameTime)
        {
            if (tileType.millisecondsBetweenFrames > 0)
            {
                if (gameTime.TotalGameTime.TotalMilliseconds - millisecondsFromLastFrame >= tileType.millisecondsBetweenFrames)
                {
                    millisecondsFromLastFrame = (uint)gameTime.TotalGameTime.TotalMilliseconds;
                    textureArrayLocation++;
                    if (textureArrayLocation >= tileType.textures.Length)
                    {
                        textureArrayLocation = 0;
                    }
                }
            }
        }

        public void tileInteracted()
        {
            interactedThisTick = true;
            tileType.OnInteraction.Invoke(this, null);
            interactedThisTick = false;
        }
    }
}

