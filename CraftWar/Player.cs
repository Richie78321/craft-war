using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace CraftWar
{
    public class OtherPlayer
    {
        public static Texture2D[,] otherPlayerSpritesheet;
        public static List<OtherPlayer> otherPlayers = new List<OtherPlayer>();

        public static void interpretPlayerServerMessage(string[] data)
        {
            foreach (OtherPlayer b in otherPlayers)
            {
                if (b.clientID == int.Parse(data[1]))
                {
                    //Correct player found. Update information
                    b.location.X = float.Parse(data[2]) * Game1.currentMap.tileSize;
                    b.location.Y = float.Parse(data[3]) * Game1.currentMap.tileSize;
                    b.spritesheetLocation[0] = int.Parse(data[4]);
                    b.spritesheetLocation[1] = int.Parse(data[5]);
                    b.currentEffect = (SpriteEffects)int.Parse(data[6]);

                    return;
                }
            }

            //Player does not exist. Create new player
            otherPlayers.Add(new OtherPlayer(int.Parse(data[1]), new Vector2(float.Parse(data[2]) * Game1.currentMap.tileSize, float.Parse(data[3])) * Game1.currentMap.tileSize, new[] { int.Parse(data[4]), int.Parse(data[5]) }, (SpriteEffects)int.Parse(data[6]), data[7]));
        }

        public static bool intersectsOtherPlayers(Rectangle rectangle)
        {
            foreach (OtherPlayer b in otherPlayers)
            {
                if (rectangle.Intersects(new Rectangle((int)(b.location.X + Game1.mainPlayer.relativeOffset.X), (int)(b.location.Y + Game1.mainPlayer.relativeOffset.Y), Game1.mainPlayer.drawRectangle.Width, Game1.mainPlayer.drawRectangle.Height)))
                {
                    return true;
                }
            }

            return false;
        }

        //Object
        public int clientID;
        public int[] spritesheetLocation = new int[2];
        public Vector2 location;
        public SpriteEffects currentEffect;
        public string username;

        public Rectangle drawRectangle
        {
            get
            {
                return new Rectangle((int)location.X, (int)location.Y, Game1.mainPlayer.drawRectangle.Width, Game1.mainPlayer.drawRectangle.Height);
            }
        }

        public Rectangle collisionRectangle
        {
            get
            {
                return new Rectangle((int)(location.X - ((Game1.mainPlayer.drawRectangle.Width - Game1.mainPlayer.rigidBody.collisionRectangle.Width) / 2)), (int)(location.Y - ((Game1.mainPlayer.drawRectangle.Height - Game1.mainPlayer.rigidBody.collisionRectangle.Height) / 2)), Game1.mainPlayer.rigidBody.collisionRectangle.Width, Game1.mainPlayer.rigidBody.collisionRectangle.Height);
            }
        }

        public OtherPlayer(int clientID, Vector2 location, int[] spritesheetLocation, SpriteEffects currentEffect, string username)
        {
            this.clientID = clientID;
            this.spritesheetLocation = spritesheetLocation;
            this.location = location;
            this.username = username;
        }

        public void draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(otherPlayerSpritesheet[spritesheetLocation[0], spritesheetLocation[1]], drawRectangle, null, LightingManager.entityLightingColor(collisionRectangle), 0, Vector2.Zero, currentEffect, 0);
            spriteBatch.DrawString(GUI.GUIFont, username, new Vector2(drawRectangle.Center.X - (GUI.GUIFont.MeasureString(username).X / 2), drawRectangle.Y - GUI.GUIFont.MeasureString(username).Y), Color.White);
        }
    }

    public class Player : Entity
    {
        public static int tileDrawRadius;
        public const byte playerRadianceLevel = 2;

        //Object
        public GUI playerGUI;
        public Inventory inventory;
        public Vector2 relativeOffset;
        public Vector2 nonRelativeOffset
        {
            get
            {
                return new Vector2(relativeOffset.X / Game1.currentMap.tileSize, relativeOffset.Y / Game1.currentMap.tileSize);
            }
            set
            {
                value.X *= Game1.currentMap.tileSize;
                value.Y *= Game1.currentMap.tileSize;
                relativeOffset = value;
            }
        }
        public float speed = .1F;
        public float jumpVelocity = .21F;
        private const float _tileInteractionRadius = 4;
        public int tileInteractionRadius
        {
            get
            {
                return (int)(_tileInteractionRadius * Game1.currentMap.tileSize);
            }
        }
        public float tileDestructionMultiplier = 1;
        private const float _dropPickupRadius = .75F;
        public int dropPickupRadius
        {
            get
            {
                return (int)(_dropPickupRadius * Game1.currentMap.tileSize);
            }
        }
        public event EventHandler OnTileBreak;
        public Tile[] tilesContainedIn
        {
            get
            {
                List<Tile> tilesContainingPlayer = new List<Tile>();
                Rectangle collisionRectangle = new Rectangle((int)(rigidBody.collisionRectangle.X - Game1.mainPlayer.relativeOffset.X), (int)(rigidBody.collisionRectangle.Y - Game1.mainPlayer.relativeOffset.Y), rigidBody.collisionRectangle.Width, rigidBody.collisionRectangle.Height);

                //Find columns containing player
                List<int> columnsContaining = new List<int>();
                columnsContaining.Add((int)Math.Floor((float)(collisionRectangle.Left - 1) / Game1.currentMap.tileSize));
                columnsContaining.Add((int)Math.Floor((float)(collisionRectangle.Right - 1) / Game1.currentMap.tileSize));

                //Fill in inside border columns
                for (int i = 0; i < columnsContaining.Count; i++)
                {
                    if (i + 1 < columnsContaining.Count)
                    {
                        //Still in bounds of array
                        for (int j = columnsContaining[i] + 1; j < columnsContaining[i + 1]; j++)
                        {
                            columnsContaining.Insert(i + 1, j);
                        }
                    }
                }

                //Find rows containing player
                List<int> rowsContaining = new List<int>();
                rowsContaining.Add((int)Math.Floor((float)(collisionRectangle.Top - 1) / Game1.currentMap.tileSize));
                rowsContaining.Add((int)Math.Floor((float)(collisionRectangle.Bottom - 1) / Game1.currentMap.tileSize));

                //Fill in inside border rows
                for (int i = 0; i < rowsContaining.Count; i++)
                {
                    if (i + 1 < rowsContaining.Count)
                    {
                        //Still in bounds of array
                        for (int j = rowsContaining[i] + 1; j < rowsContaining[i + 1]; j++)
                        {
                            rowsContaining.Insert(i + 1, j);
                        }
                    }
                }

                //Add tiles
                for (int i = 0; i < columnsContaining.Count; i++)
                {
                    for (int j = 0; j < rowsContaining.Count; j++)
                    {
                        tilesContainingPlayer.Add(Game1.currentMap.tileMap[columnsContaining[i], rowsContaining[j]]);
                    }
                }

                return tilesContainingPlayer.ToArray();
            }
        }
        public BackgroundTile[] backgroundTilesContainedIn
        {
            get
            {
                List<BackgroundTile> backgroundTilesContainingPlayer = new List<BackgroundTile>();
                Rectangle collisionRectangle = new Rectangle((int)(rigidBody.collisionRectangle.X - Game1.mainPlayer.relativeOffset.X), (int)(rigidBody.collisionRectangle.Y - Game1.mainPlayer.relativeOffset.Y), rigidBody.collisionRectangle.Width, rigidBody.collisionRectangle.Height);

                //Find columns containing player
                List<int> columnsContaining = new List<int>();
                columnsContaining.Add((int)Math.Floor((float)(collisionRectangle.Left - 1) / Game1.currentMap.tileSize));
                columnsContaining.Add((int)Math.Floor((float)(collisionRectangle.Right - 1) / Game1.currentMap.tileSize));

                //Fill in inside border columns
                for (int i = 0; i < columnsContaining.Count; i++)
                {
                    if (i + 1 < columnsContaining.Count)
                    {
                        //Still in bounds of array
                        for (int j = columnsContaining[i] + 1; j < columnsContaining[i + 1]; j++)
                        {
                            columnsContaining.Insert(i + 1, j);
                        }
                    }
                }

                //Find rows containing player
                List<int> rowsContaining = new List<int>();
                rowsContaining.Add((int)Math.Floor((float)(collisionRectangle.Top - 1) / Game1.currentMap.tileSize));
                rowsContaining.Add((int)Math.Floor((float)(collisionRectangle.Bottom - 1) / Game1.currentMap.tileSize));

                //Fill in inside border rows
                for (int i = 0; i < rowsContaining.Count; i++)
                {
                    if (i + 1 < rowsContaining.Count)
                    {
                        //Still in bounds of array
                        for (int j = rowsContaining[i] + 1; j < rowsContaining[i + 1]; j++)
                        {
                            rowsContaining.Insert(i + 1, j);
                        }
                    }
                }

                //Add tiles
                for (int i = 0; i < columnsContaining.Count; i++)
                {
                    for (int j = 0; j < rowsContaining.Count; j++)
                    {
                        if (Game1.currentMap.backgroundTileMap[columnsContaining[i], rowsContaining[j]] != null)
                        {
                            backgroundTilesContainingPlayer.Add(Game1.currentMap.backgroundTileMap[columnsContaining[i], rowsContaining[j]]);
                        }
                    }
                }

                return backgroundTilesContainingPlayer.ToArray();
            }
        }

        public Player(Texture2D standTexture, Texture2D[] walkTexture, Texture2D jumpTexture, float walkFrameDist, int screenWidth, int screenHeight, Inventory inventory, StatManager statManager, Vector2 startingRelativeOffset = new Vector2(), SpriteEffects currentEffect = SpriteEffects.None)
        {
            this.screenWidth = screenWidth;
            this.screenHeight = screenHeight;
            this.inventory = inventory;
            playerGUI = new GUI(this);
            rigidBody = new RigidBody(1);
            OnTileBreak = new EventHandler((sender, e) => { });
            this.statManager = statManager;

            spritesheetManager = new SpritesheetManager(this, walkFrameDist, new Texture2D[3, 2] { { standTexture, null }, { walkTexture[0], walkTexture[1] }, { jumpTexture, null } });
            float heightToWidthRatio = (float)spritesheetManager.spritesheet[0, 0].Height / spritesheetManager.spritesheet[0, 0].Width;
            drawRectangle = new Rectangle((screenWidth - Game1.currentMap.tileSize) / 2, (int)(screenHeight - (heightToWidthRatio * Game1.currentMap.tileSize)) / 2, Game1.currentMap.tileSize, (int)(heightToWidthRatio * Game1.currentMap.tileSize));
            rigidBody.collisionRectangle = new Rectangle((screenWidth - ((Game1.currentMap.tileSize / 3) * 2)) / 2, (int)(screenHeight - (heightToWidthRatio * Game1.currentMap.tileSize)) / 2, ((Game1.currentMap.tileSize / 3) * 2), (int)(heightToWidthRatio * Game1.currentMap.tileSize));
        }

        public Rectangle offsetRectangle(Rectangle rectangle)
        {
            return new Rectangle((int)(rectangle.X + relativeOffset.X), (int)(rectangle.Y + relativeOffset.Y), rectangle.Width, rectangle.Height);
        }

        private int screenWidth, screenHeight;
        public void keyboardController(KeyboardState keyboardState, MouseState mouseState)
        {
            //if (keyboardState.IsKeyDown(Keys.S) && rigidBody.onGround)
            //{
            //    //Is ducking
            //    if (!ducking)
            //    {
            //        //Initialize ducking collision rectangle
            //        ducking = true;
            //        float heightToWidthRatio = (float)spritesheet[0, 0].Height / spritesheet[0, 0].Width;
            //        rigidBody.collisionRectangle = new Rectangle((screenWidth - ((Game1.currentMap.tileSize / 3) * 2)) / 2, (int)(((screenHeight - (heightToWidthRatio * Game1.currentMap.tileSize)) / 2) + ((heightToWidthRatio * Game1.currentMap.tileSize) / 3)), ((Game1.currentMap.tileSize / 3) * 2), (int)(((heightToWidthRatio * Game1.currentMap.tileSize) / 3) * 2));
            //    }
            //}
            //else
            //{
            //    if (ducking)
            //    {
            //        //Restore past collision rectangle
            //        float heightToWidthRatio = (float)spritesheet[0, 0].Height / spritesheet[0, 0].Width;
            //        rigidBody.collisionRectangle = new Rectangle((screenWidth - ((Game1.currentMap.tileSize / 3) * 2)) / 2, (int)(screenHeight - (heightToWidthRatio * Game1.currentMap.tileSize)) / 2, ((Game1.currentMap.tileSize / 3) * 2), (int)(heightToWidthRatio * Game1.currentMap.tileSize));
            //        ducking = false;
            //    }
            //}

            rigidBody.nonRelativeVelocity.X = 0;
            if (keyboardState.IsKeyDown(Keys.A) && !ducking)
            {
                //Move visuals left
                //nonRelativeOffset = new Vector2(nonRelativeOffset.X + speed, nonRelativeOffset.Y);
                rigidBody.nonRelativeVelocity.X = -speed;
            }
            if (keyboardState.IsKeyDown(Keys.D) && !ducking)
            {
                //Move visuals right
                //nonRelativeOffset = new Vector2(nonRelativeOffset.X - speed, nonRelativeOffset.Y);
                rigidBody.nonRelativeVelocity.X = speed;
            }
            if (keyboardState.IsKeyDown(Keys.Space) && !ducking)
            {
                if (rigidBody.onGround)
                {
                    rigidBody.nonRelativeVelocity.Y -= jumpVelocity;
                }
            }

            playerGUI.keyboardInput(keyboardState, mouseState);
        }
        private MouseState pastMouseState;
        private Tile tileTryingToBreak = null;
        public void mouseController(MouseState mouseState, GameTime gameTime, KeyboardState keyboardState)
        {
            //Optimize
            if (!playerGUI.mouseInput(mouseState, keyboardState))
            {
                Point mouseNonRelativeLocation = new Point((int)(mouseState.X - relativeOffset.X), (int)(mouseState.Y - relativeOffset.Y));
                int[] tilePosContainedIn = new int[] { (int)Math.Floor((float)mouseNonRelativeLocation.X / Game1.currentMap.tileSize), (int)Math.Floor((float)mouseNonRelativeLocation.Y / Game1.currentMap.tileSize) };
                //Clamp to keep in bounds
                tilePosContainedIn[0] = (int)GameMath.clamp(tilePosContainedIn[0], 0, Game1.currentMap.tileMap.GetLength(0) - 1);
                tilePosContainedIn[1] = (int)GameMath.clamp(tilePosContainedIn[1], 0, Game1.currentMap.tileMap.GetLength(1) - 1);

                //LEFT
                if (mouseState.LeftButton == ButtonState.Pressed)
                {
                    //Attempt break
                    if (Game1.currentMap.tileMap[tilePosContainedIn[0], tilePosContainedIn[1]] != null && GameMath.distance(offsetRectangle(Game1.currentMap.tileMap[tilePosContainedIn[0], tilePosContainedIn[1]].rectangle).Center, rigidBody.collisionRectangle.Center) <= tileInteractionRadius)
                    {
                        Game1.currentMap.tileMap[tilePosContainedIn[0], tilePosContainedIn[1]].attemptBreakTile(gameTime, this);
                        if (tileTryingToBreak != Game1.currentMap.tileMap[tilePosContainedIn[0], tilePosContainedIn[1]])
                        {
                            if (tileTryingToBreak != null)
                            {
                                tileTryingToBreak.currentBreakTime = 0;
                                tileTryingToBreak.currentBreakOverlay = null;
                                //Update server
                                Game1.networkManager.messagesToSendToServer += ((int)GameServer.NetworkKeyword.mapInfo).ToString() + GameServer.dataSeparator + ((int)GameServer.NetworkKeyword.tileInfo).ToString() + GameServer.dataSeparator + ((int)GameServer.NetworkKeyword.tileBreakOverlay).ToString() + GameServer.dataSeparator + tileTryingToBreak.mapPosition[0] + GameServer.dataSeparator + tileTryingToBreak.mapPosition[1] + GameServer.dataSeparator + (-1).ToString() + GameServer.messageSeparator;
                            }
                            tileTryingToBreak = Game1.currentMap.tileMap[tilePosContainedIn[0], tilePosContainedIn[1]];
                        }
                    }
                    else
                    {
                        if (tileTryingToBreak != null)
                        {
                            tileTryingToBreak.currentBreakTime = 0;
                            tileTryingToBreak.currentBreakOverlay = null;
                            //Update server
                            Game1.networkManager.messagesToSendToServer += ((int)GameServer.NetworkKeyword.mapInfo).ToString() + GameServer.dataSeparator + ((int)GameServer.NetworkKeyword.tileInfo).ToString() + GameServer.dataSeparator + ((int)GameServer.NetworkKeyword.tileBreakOverlay).ToString() + GameServer.dataSeparator + tileTryingToBreak.mapPosition[0] + GameServer.dataSeparator + tileTryingToBreak.mapPosition[1] + GameServer.dataSeparator + (-1).ToString() + GameServer.messageSeparator;
                            tileTryingToBreak = null;
                        }
                    }
                }
                else
                {
                    if (tileTryingToBreak != null)
                    {
                        tileTryingToBreak.currentBreakTime = 0;
                        tileTryingToBreak.currentBreakOverlay = null;
                        //Update server
                        Game1.networkManager.messagesToSendToServer += ((int)GameServer.NetworkKeyword.mapInfo).ToString() + GameServer.dataSeparator + ((int)GameServer.NetworkKeyword.tileInfo).ToString() + GameServer.dataSeparator + ((int)GameServer.NetworkKeyword.tileBreakOverlay).ToString() + GameServer.dataSeparator + tileTryingToBreak.mapPosition[0] + GameServer.dataSeparator + tileTryingToBreak.mapPosition[1] + GameServer.dataSeparator + (-1).ToString() + GameServer.messageSeparator;
                        tileTryingToBreak = null;
                    }
                }

                //RIGHT
                if (mouseState.RightButton == ButtonState.Released && pastMouseState.RightButton == ButtonState.Pressed && !ducking)
                {
                    if (Game1.currentMap.tileMap[tilePosContainedIn[0], tilePosContainedIn[1]] == null || (Game1.currentMap.tileMap[tilePosContainedIn[0], tilePosContainedIn[1]] != null && Game1.currentMap.tileMap[tilePosContainedIn[0], tilePosContainedIn[1]].overridable) && (playerGUI.playerInventory.selectedCell != null && playerGUI.playerInventory.selectedCell.isContainingTiles()))
                    {
                        Rectangle tileRectangle = new Rectangle((int)((tilePosContainedIn[0] * Game1.currentMap.tileSize) + Game1.mainPlayer.relativeOffset.X), (int)((tilePosContainedIn[1] * Game1.currentMap.tileSize) + Game1.mainPlayer.relativeOffset.Y), Game1.currentMap.tileSize, Game1.currentMap.tileSize);

                        if (!tileRectangle.Intersects(rigidBody.collisionRectangle) && GameMath.distance(tileRectangle.Center, rigidBody.collisionRectangle.Center) <= tileInteractionRadius && !Game1.currentMap.isContainingEntities(new Rectangle(tilePosContainedIn[0] * Game1.currentMap.tileSize, tilePosContainedIn[1] * Game1.currentMap.tileSize, Game1.currentMap.tileSize, Game1.currentMap.tileSize)) && !OtherPlayer.intersectsOtherPlayers(tileRectangle))
                        {
                            //Null tile has been right clicked
                            placeTile(new[] { tilePosContainedIn[0], tilePosContainedIn[1] });
                        }
                    }
                    else if (playerGUI.playerInventory.selectedCell != null && !playerGUI.playerInventory.selectedCell.isContainingTiles())
                    {
                        //Completes the item's right click action
                        playerGUI.playerInventory.selectedCell.useItem(InventoryCell.useEventType.rightClick);
                    }
                }
            }

            pastMouseState = mouseState;
        }

        private void placeTile(int[] mapPosition)
        {
            //Sets selected coordinates
            if (playerGUI.playerInventory.selectedCell != null)
            {
                //Place tile
                Game1.currentMap.positionSelected = mapPosition;

                //Update to server
                playerGUI.playerInventory.selectedCell.useItem(InventoryCell.useEventType.rightClick);
            }
        }

        public SpritesheetManager spritesheetManager;
        public Rectangle drawRectangle;
        public override void draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(spritesheetManager.currentTexture, drawRectangle, null, LightingManager.entityLightingColor(new Rectangle((int)(rigidBody.collisionRectangle.X - Game1.mainPlayer.relativeOffset.X), (int)(rigidBody.collisionRectangle.Y - Game1.mainPlayer.relativeOffset.Y), rigidBody.collisionRectangle.Width, rigidBody.collisionRectangle.Height)), 0, Vector2.Zero, spritesheetManager.currentEffect, 0);
            //DEBUG COLLISION
            //spriteBatch.Draw(TileType.grassTile.texture, rigidBody.collisionRectangle, Color.White);
        }

        public bool ducking = false;
        public void updatePhysics(bool preBake = false)
        {
            rigidBody.applyGravity();
            relativeOffset = new Vector2(relativeOffset.X, relativeOffset.Y - rigidBody.yMovementPossible(true));
            float netXMovement = rigidBody.xMovementPossible(true);
            relativeOffset = new Vector2(relativeOffset.X - netXMovement, relativeOffset.Y);

            //Spritesheet
            //netXMovement is made non-relative
            spritesheetManager.updateSpritesheetPos(-(netXMovement / Game1.currentMap.tileSize), player: true);
        }

        public string serverInformationString
        {
            get
            {
                return ((int)GameServer.NetworkKeyword.playerInfo).ToString() + GameServer.dataSeparator + Game1.networkManager.localGameClient.clientID + GameServer.dataSeparator + (((float)(rigidBody.collisionRectangle.X - ((drawRectangle.Width - rigidBody.collisionRectangle.Width) / 2)) / Game1.currentMap.tileSize) - nonRelativeOffset.X) + GameServer.dataSeparator + (((float)(rigidBody.collisionRectangle.Y - ((drawRectangle.Height - rigidBody.collisionRectangle.Height))) / Game1.currentMap.tileSize) - nonRelativeOffset.Y) + GameServer.dataSeparator + spritesheetManager.spritesheetPos[0] + GameServer.dataSeparator + spritesheetManager.spritesheetPos[1] + GameServer.dataSeparator + (int)spritesheetManager.currentEffect + GameServer.dataSeparator + Game1.networkManager.username + GameServer.messageSeparator;
            }
        }

        public void breakTile()
        {
            OnTileBreak.Invoke(this, null);
        }

        public override void update()
        {
            throw new NotImplementedException();
        }
    }
}

