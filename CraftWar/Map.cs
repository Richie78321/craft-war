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
    public class Map
    {
        public const int desiredTilesViewed = 15;

        public int seed = 0;
        public Random syncedRandom;
        public Tile[,] tileMap;
        public BackgroundTile[,] backgroundTileMap;
        public int tileSize;
        public int[] positionSelected;
        public Background currentBackground;

        //Decorative
        public List<SurfaceTile>[,] decorativeTileMap;

        //Drop
        public List<Entity> mapEntities = new List<Entity>();
        public List<Entity> entityAddQueue = new List<Entity>();
        public List<Entity> entityRemoveQueue = new List<Entity>();

        //Particle
        private List<Particle> mapParticles = new List<Particle>();
        public List<Particle> particleAddQueue = new List<Particle>();
        public List<Particle> particleRemoveQueue = new List<Particle>();

        public Map(int width, int height, int windowHeight, int windowWidth, Background currentBackground, int seed = 0)
        {
            //Generate seed
            if (seed == 0)
            {
                //Create own seed
                Random random = new Random();
                this.seed = random.Next();
            }
            else
            {
                this.seed = seed;
            }
            syncedRandom = new Random(seed);

            //Adds two to height and width for borders
            tileMap = new Tile[width + 2, height + 2];
            backgroundTileMap = new BackgroundTile[width + 2, height + 2];
            tileSize = windowHeight / desiredTilesViewed;

            //Initializes decorative tiles
            decorativeTileMap = new List<SurfaceTile>[width + 2, height + 2];
            for (int i = 0; i < decorativeTileMap.GetLength(0); i++)
            {
                for (int j = 0; j < decorativeTileMap.GetLength(1); j++)
                {
                    decorativeTileMap[i, j] = new List<SurfaceTile>();
                }
            }

            //Create player's draw radius
            if (windowWidth > windowHeight)
            {
                //Prioritize width
                Player.tileDrawRadius = ((windowWidth / tileSize) / 2) + 2;
            }
            else
            {
                //Prioritize height
                Player.tileDrawRadius = ((windowHeight / tileSize) / 2) + 2;
            }

            this.currentBackground = currentBackground;
        }

        public void placePlayer(Player player)
        {
            //Move rigidbody MAKE NOT LAZY
            player.rigidBody.nonRelativeVelocity.Y = 1000;
            player.updatePhysics(true);
        }

        public void interpretServerMessage(string[] data)
        { 
            if (data[1] == ((int)GameServer.NetworkKeyword.tileInfo).ToString())
            {
                interpretTileServerMessage(data);
            }
            else if (data[1] == ((int)GameServer.NetworkKeyword.entityAdd).ToString())
            {
                interpretEntityAddServerMessage(data);
            }
        }

        private void interpretEntityAddServerMessage(string[] data)
        {
            if (data[2] == ((int)GameServer.NetworkKeyword.dropAdd).ToString())
            {
                //Adding drop
                ItemType dropItemType = null;
                foreach (ItemType b in ItemType.itemTypes)
                {
                    if (b.name == data[4])
                    {
                        dropItemType = b;
                    }
                }

                if (dropItemType != null)
                {
                    Item[] items = JsonConvert.DeserializeObject<Item[]>(data[3]);
                    foreach (Item b in items) b.itemType = dropItemType;

                    entityAddQueue.Add(new Drop(items, new RigidBody(Drop.massesOfPlayer), new Vector2(float.Parse(data[5]), float.Parse(data[6]))));
                }
            }
        }

        private void interpretTileServerMessage(string[] data)
        {
            if (data[2] == ((int)GameServer.NetworkKeyword.tileChange).ToString())
            {
                //Change tile
                if (data.Length >= 6 && data[5] == ((int)GameServer.NetworkKeyword.tileNull).ToString())
                {
                    //Remove tile
                    int[] mapPosition = { int.Parse(data[3]), int.Parse(data[4]) };
                    if (tileMap[mapPosition[0], mapPosition[1]] != null)
                    {
                        if (data[6] == ((int)GameServer.NetworkKeyword.trueIdentifier).ToString())
                        {
                            //Tagged by server
                            tileMap[mapPosition[0], mapPosition[1]].breakTile(false);
                        }
                        else
                        {
                            //Not tagged
                            tileMap[mapPosition[0], mapPosition[1]].breakTile(true);
                        }
                    }

                    //Update lighting
                    LightingManager.updateSurroundingTiles(mapPosition);
                }
                else
                {
                    //Update tile to specified TileType
                    Tile newTile = JsonConvert.DeserializeObject<Tile>(data[4]);
                    foreach (TileType b in TileType.tileTypes)
                    {
                        if (b.name == data[3])
                        {
                            //Matching tiletype
                            newTile.tileType = b;
                        }
                    }

                    tileMap[newTile.mapPosition[0], newTile.mapPosition[1]] = newTile;
                    
                    //Update lighting
                    LightingManager.updateSurroundingTiles(new int[] { newTile.mapPosition[0], newTile.mapPosition[1] });
                }
            }
            else if (data[2] == ((int)GameServer.NetworkKeyword.tileBreakOverlay).ToString())
            {
                //Break overlay
                int[] mapPosition = { int.Parse(data[3]), int.Parse(data[4]) };
                if (tileMap[mapPosition[0], mapPosition[1]] != null)
                {
                    if (int.Parse(data[5]) != -1)
                    {
                        tileMap[mapPosition[0], mapPosition[1]].currentBreakOverlay = tileMap[mapPosition[0], mapPosition[1]].tileType.breakOverlay[int.Parse(data[5])];
                    }
                    else
                    {
                        tileMap[mapPosition[0], mapPosition[1]].currentBreakOverlay = null;
                    }
                }
            }
        }

        public void clearMap()
        {
            backgroundTileMap = new BackgroundTile[backgroundTileMap.GetLength(0), backgroundTileMap.GetLength(1)];
            tileMap = new Tile[tileMap.GetLength(0), tileMap.GetLength(1)];
            mapParticles.Clear();
            mapEntities.Clear();
        }

        public bool isContainingEntities(Rectangle rectangle)
        {
            foreach (Entity b in mapEntities)
            {
                if (b.rigidBody != null)
                {
                    if (rectangle.Intersects(b.rigidBody.collisionRectangle))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void generateTerrain(int terrainHeight, int levelVariation, int dirtDepth)
        {
            Random random = syncedRandom;

            int currentTerrainHeight = terrainHeight;
            //Adds initial ground
            for (int i = 0; i < tileMap.GetLength(0); i++)
            {
                //Variate terrain height
                currentTerrainHeight += random.Next(-levelVariation, levelVariation + 1);
                currentTerrainHeight = (int)GameMath.clamp(currentTerrainHeight, 0, tileMap.GetLength(1));

                for (int j = 0; j < tileMap.GetLength(1); j++)
                {
                    if (j >= currentTerrainHeight)
                    {
                        //Create new tile
                        if (j - currentTerrainHeight < dirtDepth)
                        {
                            //Create dirt
                            if (j == currentTerrainHeight)
                            {
                                //Create grass
                                tileMap[i, j] = new Tile(TerrainTileType.grassTile, new[] { i, j }, tileSize, true);
                                if (j - 1 >= 0 && tileMap[i, j - 1] == null)
                                {
                                    //Create grass surface tile
                                    tileMap[i, j - 1] = new Tile(TerrainTileType.grassSurfaceTile, new[] { i, j - 1 }, tileSize, collidable: false, overridable: true, requiresSupport: true, canSupport: false);
                                }
                            }
                            else
                            {
                                //Create dirt
                                tileMap[i, j] = new Tile(TerrainTileType.dirtTile, new[] { i, j }, tileSize, true);
                            }
                        }
                        else
                        {
                            //Create stone or ore
                            tileMap[i, j] = new Tile(TerrainTileType.stoneTile, new[] { i, j }, tileSize, true);
                        }
                    }
                }
            }

            //Adds borders
            for (int i = 0; i < tileMap.GetLength(0); i++)
            {
                tileMap[i, 0] = new Tile(TerrainTileType.borderTile, new[] { i, 0 }, tileSize);
                tileMap[i, tileMap.GetLength(1) - 1] = new Tile(TerrainTileType.borderTile, new[] { i, tileMap.GetLength(1) - 1 }, tileSize);
            }
            for (int j = 0; j < tileMap.GetLength(1); j++)
            {
                tileMap[0, j] = new Tile(TerrainTileType.borderTile, new[] { 0, j }, tileSize);
                tileMap[tileMap.GetLength(0) - 1, j] = new Tile(TerrainTileType.borderTile, new[] { tileMap.GetLength(0) - 1, j }, tileSize);
            }
        }

        private const int treeRandomMax = 10;
        private const int maxLeafVariance = 10;
        private const int branchRandomMax = 10;
        public void generateTrees(int treeDensity, int treeHeight, int treeHeightVariance, int leafDensity, int branchDensity)
        {
            Random random = syncedRandom;

            bool treeGeneratedBefore = false;
            for (int i = 0; i < tileMap.GetLength(0); i++)
            {
                if (random.Next(0, treeRandomMax + 1) <= treeDensity && !treeGeneratedBefore)
                {
                    //Generate tree in this column
                    treeGeneratedBefore = true;
                    bool levelFound = false;
                    for (int j = 0; j < tileMap.GetLength(1) && !levelFound; j++)
                    {
                        if (tileMap[i, j] != null && tileMap[i, j].tileType != TerrainTileType.borderTile && !tileMap[i, j].overridable)
                        {
                            if (tileMap[i, j].tileType == TerrainTileType.grassTile || tileMap[i, j].tileType == TerrainTileType.dirtTile)
                            {
                                //Generate tree
                                int currentTreeHeight = treeHeight + random.Next(-treeHeightVariance, treeHeightVariance + 1);
                                int treeBuildHeight;
                                bool obstructionEncountered = false;
                                for (treeBuildHeight = j - 1; j >= 0 && j - (treeBuildHeight - 1) <= currentTreeHeight && !obstructionEncountered; treeBuildHeight--)
                                {
                                    if (tileMap[i, treeBuildHeight] == null || (tileMap[i, treeBuildHeight] != null && tileMap[i, treeBuildHeight].overridable))
                                    {
                                        //Add wood
                                        if (treeBuildHeight == j - 1)
                                        {
                                            tileMap[i, treeBuildHeight] = new Tile(TerrainTileType.logTile, new[] { i, treeBuildHeight }, tileSize, collidable: false);
                                            tileMap[i, treeBuildHeight].textureArrayLocation = random.Next(0, 2);

                                            //Add roots
                                            if (i + 1 < tileMap.GetLength(0) && (tileMap[i + 1, treeBuildHeight] == null || !tileMap[i + 1, treeBuildHeight].collidable) && (tileMap[i + 1, treeBuildHeight + 1] != null && tileMap[i + 1, treeBuildHeight + 1].collidable))
                                            {
                                                //Open space to the right of stone. Add surface dirt
                                                decorativeTileMap[i + 1, treeBuildHeight].Add(new SurfaceTile(SurfaceTiles.rootRight, SurfaceTile.SupportDependence.Left, new[] { i, treeBuildHeight }));
                                            }
                                            if (i - 1 >= 0 && (tileMap[i - 1, treeBuildHeight] == null || !tileMap[i - 1, treeBuildHeight].collidable) && (tileMap[i - 1, treeBuildHeight + 1] != null && tileMap[i - 1, treeBuildHeight + 1].collidable))
                                            {
                                                //Open space to the left of stone. Add surface dirt
                                                decorativeTileMap[i - 1, treeBuildHeight].Add(new SurfaceTile(SurfaceTiles.rootLeft, SurfaceTile.SupportDependence.Right, new[] { i, treeBuildHeight }));
                                            }
                                        }
                                        else
                                        {
                                            tileMap[i, treeBuildHeight] = new Tile(TerrainTileType.logTile, new[] { i, treeBuildHeight }, tileSize, collidable: false);
                                            tileMap[i, treeBuildHeight].textureArrayLocation = random.Next(0, 2);
                                            
                                            //Add chance for branch
                                            if (random.Next(0, branchRandomMax + 1) <= branchDensity)
                                            {
                                                //Branch will be made
                                                if (random.Next(0, 2) == 0)
                                                {
                                                    //Left
                                                    if ((i - 1 >= 0 && tileMap[i - 1, treeBuildHeight] == null) && (i - 2 >= 0 && tileMap[i - 2, treeBuildHeight] == null))
                                                    {
                                                        tileMap[i - 1, treeBuildHeight] = new Tile(TerrainTileType.branchTile, new[] { i - 1, treeBuildHeight }, tileSize, collidable: false);

                                                        tileMap[i - 2, treeBuildHeight] = new Tile(TerrainTileType.leavesTile, new[] { i - 2, treeBuildHeight }, tileSize, collidable: false);
                                                    }
                                                }
                                                else
                                                {
                                                    //Right
                                                    if ((i + 1 < tileMap.GetLength(0) && tileMap[i + 1, treeBuildHeight] == null) && (i + 2 < tileMap.GetLength(0) && tileMap[i + 2, treeBuildHeight] == null))
                                                    {
                                                        tileMap[i + 1, treeBuildHeight] = new Tile(TerrainTileType.branchTile, new[] { i + 1, treeBuildHeight }, tileSize, collidable: false);
                                                        tileMap[i + 1, treeBuildHeight].textureArrayLocation = 1;

                                                        tileMap[i + 2, treeBuildHeight] = new Tile(TerrainTileType.leavesTile, new[] { i + 2, treeBuildHeight }, tileSize, collidable: false);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        obstructionEncountered = true;
                                    }
                                }

                                //Tree finished, generate leaves
                                for (int l = treeBuildHeight - 1; l < treeBuildHeight + 1 && l >= 0 && l < tileMap.GetLength(1); l++)
                                {
                                    for (int x = i - 1; x <= i + 1 && x >= 0 && x < tileMap.GetLength(0); x++)
                                    {
                                        if (random.Next(0, maxLeafVariance + 1) <= leafDensity && tileMap[x, l] == null)
                                        {
                                            //Add leaf
                                            tileMap[x, l] = new Tile(TerrainTileType.leavesTile, new[] { x, l }, tileSize, collidable: false);
                                        }
                                    }
                                }
                            }

                            levelFound = true;
                        }
                    }
                }
                else
                {
                    treeGeneratedBefore = false;
                }
            }
        }

        private const int maxCaveStepDecay = 1000;
        public void generateCaves(int numberOfCaves, int caveRadius, int maxCaveSteps)
        {
            Random random = syncedRandom;
            for (int p = 0; p < numberOfCaves; p++)
            {
                int xValue = random.Next(0, tileMap.GetLength(0));
                int yValue = random.Next(0, tileMap.GetLength(1));
                int currentCaveSteps = random.Next(0, maxCaveSteps + 1);
                for (int i = 0; i < currentCaveSteps; i++)
                {
                    xValue += random.Next(-1, 2);
                    yValue += random.Next(-1, 2);
                    for (int k = (int)GameMath.clamp(xValue - caveRadius, 1, tileMap.GetLength(0) - 2); k <= (int)GameMath.clamp(xValue, 1, tileMap.GetLength(0) - 2); k++)
                    {
                        for (int l = (int)GameMath.clamp(yValue - caveRadius, 1, tileMap.GetLength(1) - 2); l <= (int)GameMath.clamp(yValue, 1, tileMap.GetLength(1) - 2); l++)
                        {
                            if (tileMap[k, l] != null && tileMap[k, l].tileType != TerrainTileType.borderTile)
                            {
                                //Break tile
                                tileMap[k, l].breakTile(false);
                            }
                        }
                    }
                }
            }
        }

        private const int orePocketSize = 4;
        private const int orePocketRarity = 8;
        private const int orePocketRarityMax = 20;
        private const int orePocketStep = 15;
        private const int orePocketStepVariety = 2;
        private const int orePlaceRarityMax = 100;
        public void generateOre(int terrainHeight, int coalMinDepth, int copperMinDepth, int ironMinDepth, int goldMinDepth, int diamondMinDepth, int coalRarity, int copperRarity, int ironRarity, int goldRarity, int diamondRarity)
        {
            Random random = syncedRandom;
            for (int j = terrainHeight + random.Next(0, orePocketStep + 1); j < tileMap.GetLength(1); j+= random.Next(orePocketStep - orePocketStepVariety, orePocketStep + orePocketStepVariety + 1))
            {
                List<TileType> placeableOres = new List<TileType>();
                List<int> oreRarities = new List<int>();
                if (j >= coalMinDepth) { placeableOres.Add(OreTileType.coalOre); oreRarities.Add(coalRarity); }
                if (j >= copperMinDepth) { placeableOres.Add(OreTileType.copperOre); oreRarities.Add(copperRarity); }
                if (j >= ironMinDepth) { placeableOres.Add(OreTileType.ironOre); oreRarities.Add(ironRarity); }
                if (j >= goldMinDepth) { placeableOres.Add(OreTileType.goldOre); oreRarities.Add(goldRarity); }
                if (j >= diamondMinDepth) { placeableOres.Add(OreTileType.diamondOre); oreRarities.Add(diamondRarity); };

                for (int i = random.Next(0, orePocketStep + 1); i < tileMap.GetLength(0); i+= random.Next(orePocketStep - orePocketStepVariety, orePocketStep + orePocketStepVariety + 1))
                {
                    //Ask if stone exists
                    if (tileMap[i, j] != null && tileMap[i, j].tileType == TerrainTileType.stoneTile)
                    {
                        //Stone is here. Run chances
                        if (random.Next(0, orePocketRarityMax + 1) <= orePocketRarity)
                        {
                            //Place ore pocket
                            int oreID = random.Next(0, placeableOres.Count);
                            for (int k = (int)GameMath.clamp(i - orePocketSize, 0, tileMap.GetLength(0) - 1); k < (int)GameMath.clamp(i, 0, tileMap.GetLength(0) - 1); k++)
                            {
                                for (int l = (int)GameMath.clamp(j - orePocketSize, 0, tileMap.GetLength(1) - 1); l < (int)GameMath.clamp(j, 0, tileMap.GetLength(1) - 1); l++)
                                {
                                    //Place ore by random chance
                                    if (random.Next(0, orePlaceRarityMax + 1) <= oreRarities[oreID])
                                    {
                                        //Place ore if it is overwriting stone
                                        if (tileMap[k, l] != null && tileMap[k, l].tileType == TerrainTileType.stoneTile)
                                        {
                                            tileMap[k, l] = new Tile(placeableOres[oreID], new[] { k, l }, tileSize, terrain: true);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void addSurfaceTiles()
        {
            List<SurfaceTileRequest> surfaceTileRequests = new List<SurfaceTileRequest>();
            //Add surface tile requests here
            surfaceTileRequests.Add(new SurfaceTileRequest(TerrainTileType.dirtTile, TerrainTileType.surfaceDirtSurfaceTile.textures[0], SurfaceTiles.dirtBottom, SurfaceTiles.dirtLeft, SurfaceTiles.dirtRight));
            surfaceTileRequests.Add(new SurfaceTileRequest(TerrainTileType.grassTile, null, SurfaceTiles.dirtBottom, SurfaceTiles.dirtLeft, SurfaceTiles.dirtRight, placedOnNoCollide: true));
            surfaceTileRequests.Add(new SurfaceTileRequest(TerrainTileType.stoneTile, SurfaceTiles.stoneTop, SurfaceTiles.stoneBottom, SurfaceTiles.stoneLeft, SurfaceTiles.stoneRight));
            surfaceTileRequests.Add(new SurfaceTileRequest(TerrainTileType.leavesTile, SurfaceTiles.leavesTop, SurfaceTiles.leavesBottom, SurfaceTiles.leavesLeft, SurfaceTiles.leavesRight, placedOnNoCollide: true));
            surfaceTileRequests.Add(new SurfaceTileRequest(TerrainTileType.logTile, null, null, SurfaceTiles.logLeft, SurfaceTiles.logRight, placedOnNoCollide: true));

            completeSurfaceTileRequests(surfaceTileRequests);
        }

        private void completeSurfaceTileRequests(List<SurfaceTileRequest> surfaceTileRequests)
        {
            for (int i = 0; i < tileMap.GetLength(0); i++)
            {
                for (int j = 0; j < tileMap.GetLength(1); j++)
                {
                    if (tileMap[i, j] != null)
                    {
                        foreach (SurfaceTileRequest b in surfaceTileRequests)
                        {
                            if (tileMap[i, j].tileType == b.tileTypeToAddTo)
                            {
                                if (b.up != null && j - 1 >= 0 && (tileMap[i, j - 1] == null || (b.placedOnNoCollide && !tileMap[i, j - 1].collidable)))
                                {
                                    //Open space above tile. Add surface tile
                                    decorativeTileMap[i, j - 1].Add(new SurfaceTile(b.up, SurfaceTile.SupportDependence.Down, new[] { i, j }));
                                }
                                if (b.right != null && i + 1 < tileMap.GetLength(0) && (tileMap[i + 1, j] == null || (b.placedOnNoCollide && !tileMap[i + 1, j].collidable)))
                                {
                                    //Open space to the right of stone. Add surface dirt
                                    decorativeTileMap[i + 1, j].Add(new SurfaceTile(b.right, SurfaceTile.SupportDependence.Left, new[] { i, j }));
                                }
                                if (b.left != null && i - 1 >= 0 && (tileMap[i - 1, j] == null || (b.placedOnNoCollide && !tileMap[i - 1, j].collidable)))
                                {
                                    //Open space to the left of stone. Add surface dirt
                                    decorativeTileMap[i - 1, j].Add(new SurfaceTile(b.left, SurfaceTile.SupportDependence.Right, new[] { i, j }));
                                }
                                if (b.down != null && j + 1 < tileMap.GetLength(1) && (tileMap[i, j + 1] == null || (b.placedOnNoCollide && !tileMap[i, j + 1].collidable)))
                                {
                                    //Open space below the stone. Add surface dirt
                                    decorativeTileMap[i, j + 1].Add(new SurfaceTile(b.down, SurfaceTile.SupportDependence.Up, new[] { i, j }));
                                }
                            }
                        }
                    }
                }
            }
        }

        public void drawMap(SpriteBatch spriteBatch, GameTime gameTime)
        {
            Rectangle playerCollisionRectangle = new Rectangle((int)(Game1.mainPlayer.rigidBody.collisionRectangle.X - Game1.mainPlayer.relativeOffset.X), (int)(Game1.mainPlayer.rigidBody.collisionRectangle.Y - Game1.mainPlayer.relativeOffset.Y), Game1.mainPlayer.rigidBody.collisionRectangle.Width, Game1.mainPlayer.rigidBody.collisionRectangle.Height);
            int[] playerMapPos = new int[] { (int)Math.Floor((float)(playerCollisionRectangle.Center.X - 1) / Game1.currentMap.tileSize), (int)Math.Floor((float)(playerCollisionRectangle.Center.Y - 1) / Game1.currentMap.tileSize) };

            Color backgroundTileColor = new Color(.4F * LightingManager.skyLightIntensity, .4F * LightingManager.skyLightIntensity, .4F * LightingManager.skyLightIntensity);
            for (int i = (int)GameMath.clamp(playerMapPos[0] - Player.tileDrawRadius, 0, tileMap.GetLength(0)); i < (int)GameMath.clamp(playerMapPos[0] + Player.tileDrawRadius, 0, tileMap.GetLength(0)); i++)
            {
                for (int j = (int)GameMath.clamp(playerMapPos[1] - Player.tileDrawRadius, 0, tileMap.GetLength(1)); j < (int)GameMath.clamp(playerMapPos[1] + Player.tileDrawRadius, 0, tileMap.GetLength(1)); j++)
                {
                    //Draw background tiles
                    if (backgroundTileMap[i, j] != null)
                    {
                        spriteBatch.Draw(backgroundTileMap[i, j].texture, new Rectangle(i * tileSize, j * tileSize, tileSize, tileSize), backgroundTileMap[i, j].lightingManager.tileColor);
                    }

                    //Draw tiles
                    if (tileMap[i, j] != null)
                    {
                        tileMap[i, j].updateAnimation(gameTime);
                        tileMap[i, j].draw(spriteBatch);
                    }

                    //Draw decorative tiles
                    foreach (SurfaceTile b in decorativeTileMap[i, j])
                    {
                        spriteBatch.Draw(b.texture, new Rectangle(i * tileSize, j * tileSize, tileSize, tileSize), b.drawColor);
                    }
                }
            }

            //Draw entities
            foreach (Entity b in mapEntities)
            {
                b.draw(spriteBatch);
            }

            //Draw particles
            foreach (Particle b in mapParticles)
            {
                b.draw(spriteBatch);
            }
        }

        public bool isConnectedTile(int[] requestedPosition, bool requiresSupport = false)
        {
            if (!requiresSupport)
            {
                if (backgroundTileMap[requestedPosition[0], requestedPosition[1]] != null)
                {
                    return true;
                }

                if (requestedPosition[0] - 1 >= 0)
                {
                    if (tileMap[requestedPosition[0] - 1, requestedPosition[1]] != null && tileMap[requestedPosition[0] - 1, requestedPosition[1]].canSupport)
                    {
                        if (tileMap[requestedPosition[0] - 1, requestedPosition[1]].tileType != TerrainTileType.borderTile)
                        {
                            return true;
                        }
                    }
                }
                if (requestedPosition[0] + 1 < tileMap.GetLength(0))
                {
                    if (tileMap[requestedPosition[0] + 1, requestedPosition[1]] != null && tileMap[requestedPosition[0] + 1, requestedPosition[1]].canSupport)
                    {
                        if (tileMap[requestedPosition[0] + 1, requestedPosition[1]].tileType != TerrainTileType.borderTile)
                        {
                            return true;
                        }
                    }
                }

                if (requestedPosition[1] - 1 >= 0)
                {
                    if (tileMap[requestedPosition[0], requestedPosition[1] - 1] != null && tileMap[requestedPosition[0], requestedPosition[1] - 1].canSupport)
                    {
                        if (tileMap[requestedPosition[0], requestedPosition[1] - 1].tileType != TerrainTileType.borderTile)
                        {
                            return true;
                        }
                    }
                }
            }
            if (requestedPosition[1] + 1 >= 0)
            {
                if (tileMap[requestedPosition[0], requestedPosition[1] + 1] != null && tileMap[requestedPosition[0], requestedPosition[1] + 1].canSupport)
                {
                    if (tileMap[requestedPosition[0], requestedPosition[1] + 1].tileType != TerrainTileType.borderTile)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void update(GameTime gameTime, int screenWidth)
        {
            //Removes entities in queue
            foreach (Entity b in entityRemoveQueue) mapEntities.Remove(b);
            entityRemoveQueue.Clear();

            //Adds entities in queue
            mapEntities.AddRange(entityAddQueue);
            entityAddQueue.Clear();

            //Updates entities
            foreach (Entity b in mapEntities)
            {
                b.update();
            }

            //Removes particles in queue
            foreach (Particle b in particleRemoveQueue) mapParticles.Remove(b);
            particleRemoveQueue.Clear();

            //Adds particles in queue
            mapParticles.AddRange(particleAddQueue);
            particleAddQueue.Clear();

            //Updates particles
            foreach (Particle b in mapParticles)
            {
                b.updateParticle(gameTime);
            }

            //Updates background
            currentBackground.updateSkyTiles(screenWidth);
        }

        public void addInitialLighting()
        {
            for (int i = 0; i < tileMap.GetLength(0); i++)
            {
                for (int j = 0; j < tileMap.GetLength(1); j++)
                {
                    tileLighting(i, j);
                }
            }
        }

        private void tileLighting(int i, int j)
        {
            Task task = new Task(() =>
            {
                if (tileMap[i, j] != null)
                {
                    tileMap[i, j].lightingManager.checkForLighting();
                }
                if (backgroundTileMap[i, j] != null)
                {
                    backgroundTileMap[i, j].lightingManager.checkForLighting();
                }
            });

            task.Start();
        }
    }

    class SurfaceTileRequest
    {
        public TileType tileTypeToAddTo;
        public Texture2D up;
        public Texture2D down;
        public Texture2D left;
        public Texture2D right;
        public bool placedOnNoCollide;

        public SurfaceTileRequest(TileType tileToAddTo, Texture2D up, Texture2D down, Texture2D left, Texture2D right, bool placedOnNoCollide = false)
        {
            this.tileTypeToAddTo = tileToAddTo;
            this.up = up;
            this.down = down;
            this.left = left;
            this.right = right;
            this.placedOnNoCollide = placedOnNoCollide;
        }
    }
}

