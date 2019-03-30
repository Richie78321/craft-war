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
    public class RigidBody
    {
        public delegate void CollisionEvent(object objectCollided);

        //Object
        public Vector2 nonRelativeVelocity;
        public Vector2 relativeVelocity
        {
            get
            {
                return new Vector2(nonRelativeVelocity.X * Game1.currentMap.tileSize, nonRelativeVelocity.Y * Game1.currentMap.tileSize);
            }
            set
            {
                nonRelativeVelocity.X = value.X / Game1.currentMap.tileSize;
                nonRelativeVelocity.Y = value.Y / Game1.currentMap.tileSize;
            }
        }
        public bool onGround
        {
            get
            {
                return onFaces[1];
            }
            set
            {
                onFaces[1] = value;
            }
        }
        //UP, DOWN, LEFT, RIGHT
        public bool[] onFaces = new bool[4];
        private Tile[] tilesCollidingWith = new Tile[4];
        public Rectangle collisionRectangle;
        public float massesOfPlayer;
        public CollisionEvent OnEntityCollisionEvent;
        public CollisionEvent OnTileCollisionEvent;

        public RigidBody(float massesOfPlayer, Rectangle collisionRectangle = new Rectangle(), bool onGround = false)
        {
            this.massesOfPlayer = massesOfPlayer;
            this.collisionRectangle = collisionRectangle;

            OnEntityCollisionEvent = new CollisionEvent((objectCollided) => { });
            OnTileCollisionEvent = new CollisionEvent((objectCollided) => { });
        }

        public void applyGravity()
        {
            //if (!onGround)
            //{

            //}
            nonRelativeVelocity.Y += Physics.gravityAcceleration * massesOfPlayer;
        }

        public Rectangle getOffsetCollisionRectangle()
        {
            return new Rectangle((int)(collisionRectangle.X + Game1.mainPlayer.relativeOffset.X), (int)(collisionRectangle.Y + Game1.mainPlayer.relativeOffset.Y), collisionRectangle.Width, collisionRectangle.Height);
        }

        public float yMovementPossible(bool player = false)
        {
            if (nonRelativeVelocity.Y != 0)
            {
                onFaces[0] = false; onFaces[1] = false;
                tilesCollidingWith[0] = null; tilesCollidingWith[1] = null;

                Rectangle collisionRectangle;
                if (player)
                {
                    collisionRectangle = new Rectangle((int)(this.collisionRectangle.X - Game1.mainPlayer.relativeOffset.X), (int)(this.collisionRectangle.Y - Game1.mainPlayer.relativeOffset.Y), this.collisionRectangle.Width, this.collisionRectangle.Height);
                }
                else
                {
                    collisionRectangle = this.collisionRectangle;
                }

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
                if (relativeVelocity.Y > 0)
                {
                    //Finds highest tile under
                    Tile highestTile = null;
                    foreach (int i in columnsContaining)
                    {
                        bool found = false;
                        for (int j = (int)Math.Floor((float)collisionRectangle.Bottom / Game1.currentMap.tileSize); j < Game1.currentMap.tileMap.GetLength(1) && !found; j++)
                        {
                            if (Game1.currentMap.tileMap[i, j] != null && Game1.currentMap.tileMap[i, j].collidable)
                            {
                                if (Game1.currentMap.tileMap[i, j].rectangle.Top >= collisionRectangle.Bottom)
                                {
                                    //Highest tile under
                                    found = true;
                                    if (highestTile == null || highestTile.rectangle.Y > Game1.currentMap.tileMap[i, j].rectangle.Y)
                                    {
                                        highestTile = Game1.currentMap.tileMap[i, j];
                                    }
                                }
                            }
                        }
                    }

                    if (highestTile != null)
                    {
                        int distanceToTile = highestTile.rectangle.Top - collisionRectangle.Bottom;

                        if (Math.Abs(relativeVelocity.Y) < distanceToTile)
                        {
                            //No collisions. Full velocity
                            return relativeVelocity.Y;
                        }
                        else
                        {
                            //Collisions. Partial velocity (Velocity is removed)
                            nonRelativeVelocity.Y = 0;
                            onGround = true;
                            tilesCollidingWith[1] = highestTile;
                            OnTileCollisionEvent.Invoke(highestTile);
                            return distanceToTile;
                        }
                    }
                    else
                    {
                        //No collisions possible. Full velocity
                        return relativeVelocity.Y;
                    }
                }
                else if (relativeVelocity.Y < 0)
                {
                    //Finds lowest tile above
                    Tile lowestTile = null;
                    foreach (int i in columnsContaining)
                    {
                        bool found = false;
                        for (int j = (int)Math.Floor((float)collisionRectangle.Top / Game1.currentMap.tileSize); j >= 0 && !found; j--)
                        {
                            if (Game1.currentMap.tileMap[i, j] != null && Game1.currentMap.tileMap[i, j].collidable)
                            {
                                if (Game1.currentMap.tileMap[i, j].rectangle.Bottom <= collisionRectangle.Top)
                                {
                                    //Highest tile under
                                    found = true;
                                    if (lowestTile == null || lowestTile.rectangle.Y < Game1.currentMap.tileMap[i, j].rectangle.Y)
                                    {
                                        lowestTile = Game1.currentMap.tileMap[i, j];
                                    }
                                }
                            }
                        }
                    }

                    if (lowestTile != null)
                    {
                        int distanceToTile = collisionRectangle.Top - lowestTile.rectangle.Bottom;

                        if (Math.Abs(relativeVelocity.Y) < distanceToTile)
                        {
                            //No collisions. Full velocity
                            return relativeVelocity.Y;
                        }
                        else
                        {
                            //Collisions. Partial velocity (Velocity is removed)
                            nonRelativeVelocity.Y = 0;
                            onFaces[0] = true;
                            tilesCollidingWith[0] = lowestTile;
                            OnTileCollisionEvent.Invoke(lowestTile);
                            return -distanceToTile + 1;
                        }
                    }
                    else
                    {
                        //No collisions possible. Full velocity
                        return relativeVelocity.Y;
                    }
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                return 0;
            }
        }

        public float xMovementPossible(bool player = false)
        {
            if (nonRelativeVelocity.X != 0)
            {
                onFaces[2] = false; onFaces[3] = false;
                tilesCollidingWith[2] = null; tilesCollidingWith[3] = null;

                Rectangle collisionRectangle;
                if (player)
                {
                    collisionRectangle = new Rectangle((int)(this.collisionRectangle.X - Game1.mainPlayer.relativeOffset.X), (int)(this.collisionRectangle.Y - Game1.mainPlayer.relativeOffset.Y), this.collisionRectangle.Width, this.collisionRectangle.Height);
                }
                else
                {
                    collisionRectangle = this.collisionRectangle;
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
                if (relativeVelocity.X > 0)
                {
                    //Most left tile on right
                    Tile leftestTile = null;
                    foreach (int j in rowsContaining)
                    {
                        bool found = false;
                        for (int i = (int)Math.Floor((float)collisionRectangle.Right / Game1.currentMap.tileSize); i < Game1.currentMap.tileMap.GetLength(0) && !found; i++)
                        {
                            if (Game1.currentMap.tileMap[i, j] != null && Game1.currentMap.tileMap[i, j].collidable)
                            {
                                if (Game1.currentMap.tileMap[i, j].rectangle.Left >= collisionRectangle.Right)
                                {
                                    //Highest tile under
                                    found = true;
                                    if (leftestTile == null || leftestTile.rectangle.X > Game1.currentMap.tileMap[i, j].rectangle.X)
                                    {
                                        leftestTile = Game1.currentMap.tileMap[i, j];
                                    }
                                }
                            }
                        }
                    }


                    if (leftestTile != null)
                    {
                        int distanceToTile = leftestTile.rectangle.Left - collisionRectangle.Right;

                        if (Math.Abs(relativeVelocity.X) < distanceToTile)
                        {
                            //No collisions. Full velocity
                            return relativeVelocity.X;
                        }
                        else
                        {
                            //Collisions. Partial velocity (Velocity is removed)
                            nonRelativeVelocity.X = 0;
                            onFaces[3] = true;
                            tilesCollidingWith[3] = leftestTile;
                            OnTileCollisionEvent.Invoke(leftestTile);
                            return distanceToTile;
                        }
                    }
                    else
                    {
                        //No collisions possible. Full velocity
                        return relativeVelocity.X;
                    }
                }
                else if (relativeVelocity.X < 0)
                {
                    //Most right tile on left
                    Tile rightestTile = null;
                    foreach (int j in rowsContaining)
                    {
                        bool found = false;
                        for (int i = (int)Math.Floor((float)collisionRectangle.Left / Game1.currentMap.tileSize); i >= 0 && !found; i--)
                        {
                            if (Game1.currentMap.tileMap[i, j] != null && Game1.currentMap.tileMap[i, j].collidable)
                            {
                                if (Game1.currentMap.tileMap[i, j].rectangle.Right <= collisionRectangle.Left)
                                {
                                    //Highest tile under
                                    found = true;
                                    if (rightestTile == null || rightestTile.rectangle.X < Game1.currentMap.tileMap[i, j].rectangle.X)
                                    {
                                        rightestTile = Game1.currentMap.tileMap[i, j];
                                    }
                                }
                            }
                        }
                    }

                    if (rightestTile != null)
                    {
                        int distanceToTile = collisionRectangle.Left - rightestTile.rectangle.Right;

                        if (Math.Abs(relativeVelocity.X) < distanceToTile)
                        {
                            //No collisions. Full velocity
                            return relativeVelocity.X;
                        }
                        else
                        {
                            //Collisions. Partial velocity (Velocity is removed)
                            nonRelativeVelocity.X = 0;
                            onFaces[2] = true;
                            tilesCollidingWith[2] = rightestTile;
                            OnTileCollisionEvent.Invoke(rightestTile);
                            return -distanceToTile + 1;
                        }
                    }
                    else
                    {
                        //No collisions possible. Full velocity
                        return relativeVelocity.X;
                    }
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                return 0;
            }
        }

        public void applyFriction()
        {
            //Only apply to DOWN (because force must be acting towards for friction)
            if (onFaces[1] && tilesCollidingWith[1] != null)
            {
                nonRelativeVelocity.X += (-nonRelativeVelocity.X * tilesCollidingWith[1].tileType.tileMaterial.friction);
            }
        }

        //OPTIMIZE (Maybe implement chunks?)
        public List<Entity> entityCollisionBlacklist = new List<Entity>();
        public void checkForEntityCollision()
        {
            foreach (Entity b in Game1.currentMap.mapEntities)
            {
                if (!entityCollisionBlacklist.Contains(b))
                {
                    //Check for collision
                    if (b.rigidBody != this && b.rigidBody.collisionRectangle.Intersects(collisionRectangle))
                    {
                        OnEntityCollisionEvent.Invoke(b);
                    }
                }
            }
        }
    }
}

