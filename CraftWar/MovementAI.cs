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
    public class MovementAI
    {
        public float nonRelativeWakeDistance;
        public float relativeWakeDistance
        {
            get
            {
                return nonRelativeWakeDistance * Game1.currentMap.tileSize;
            }
        }
        private bool _dormant = false;
        public bool dormant
        {
            get
            {
                return _dormant;
            }
            set
            {
                if (value != _dormant)
                {
                    if (!value)
                    {
                        addWakeEvent(remove: false);
                    }
                    else
                    {
                        addWakeEvent();
                    }
                }

                _dormant = value;
            }
        }
        private bool _left = false;
        public bool left
        {
            get
            {
                return _left;
            }
            set
            {
                if (value != _left)
                {
                    movementDirectionChanged = true;
                }

                _left = value;
            }
        }
        bool playerAggression;
        public float movementSpeed;
        public int maxAttemptedJumps;
        private int attemptedJumps = 0;
        public float jumpVelocity;
        public int minDistanceFromTarget;
        public RigidBody rigidBody;

        public MovementAI (float nonRelativeWakeDistance, float movementSpeed, float jumpVelocity, int minDistanceFromTarget, bool playerAggression = false, int maxAttemptedJumps = 2, RigidBody worldCollisionRigidbody = null)
        {
            this.rigidBody = worldCollisionRigidbody;
            this.nonRelativeWakeDistance = nonRelativeWakeDistance;
            this.playerAggression = playerAggression;
            this.movementSpeed = movementSpeed;
            this.maxAttemptedJumps = maxAttemptedJumps;
            this.jumpVelocity = jumpVelocity;
            this.minDistanceFromTarget = minDistanceFromTarget;
            OnDestinationReached = new EventHandler((sender, e) => { });
        }

        //Rigidbody on faces L = 2, R = 3
        private bool movementDirectionChanged = false;
        public void updateAI()
        {
            if (findTarget() && (!dormant || movementDirectionChanged))
            {
                //Reset movementDirectionChanged
                movementDirectionChanged = false;
                dormant = false;

                if (left)
                {
                    //Left movement
                    if (rigidBody.onFaces[2])
                    {
                        //Left collision. Attempt jump
                        jump();
                    }
                    else
                    {
                        //No jump needed. Reset jump attempts
                        attemptedJumps = 0;
                    }

                    //Apply movement velocity
                    rigidBody.nonRelativeVelocity.X = -movementSpeed;
                }
                else
                {
                    //Right movement
                    if (rigidBody.onFaces[3])
                    {
                        //Right collision. Attempt jump
                        jump();
                    }
                    else
                    {
                        //No jump needed. Reset jump attempts
                        attemptedJumps = 0;
                    }

                    //Apply movement velocity
                    rigidBody.nonRelativeVelocity.X = movementSpeed;
                }
            }
            else
            {
                //Reset velocity
                rigidBody.nonRelativeVelocity.X = 0;
            }
        }

        public void updateRigidbody(SpritesheetManager spritesheetManager = null)
        {
            rigidBody.applyGravity();
            rigidBody.collisionRectangle.Y += (int)rigidBody.yMovementPossible();

            float netXMovement = rigidBody.xMovementPossible();
            if (spritesheetManager != null)
            {
                spritesheetManager.updateSpritesheetPos(netXMovement / Game1.currentMap.tileSize);
            }
            rigidBody.collisionRectangle.X += (int)netXMovement;
        }

        private void jump()
        {
            if (rigidBody.onGround)
            {
                if (attemptedJumps == maxAttemptedJumps)
                {
                    //Too many jump attempts. Become dormant
                    attemptedJumps = 0;
                    dormant = true;

                    //ADD LOGIC FOR TILES WAKING


                    return;
                }
                attemptedJumps++;

                //Jump
                rigidBody.nonRelativeVelocity.Y -= jumpVelocity;
            }
        }

        public EventHandler OnDestinationReached;
        public object targetObject = null;
        private bool findTarget()
        {
            //OPTIMIZE
            //Find min distance target

            targetObject = null;
            int minDist = (int)(relativeWakeDistance + 1);
            int xValueOfTarget = 0;
            int trueDistanceOfTarget = minDistanceFromTarget + 1;
            Rectangle targetRectangle = new Rectangle();
            
            if (playerAggression)
            {            
                //Check player
                int playerDist = GameMath.distance(rigidBody.getOffsetCollisionRectangle().Center, Game1.mainPlayer.rigidBody.collisionRectangle.Center);
                if (playerDist <= relativeWakeDistance)
                {
                    //Player is in range
                    if (playerDist < minDist)
                    {
                        minDist = playerDist;
                        xValueOfTarget = (int)(Game1.mainPlayer.rigidBody.collisionRectangle.Center.X - Game1.mainPlayer.relativeOffset.X);
                        targetObject = Game1.mainPlayer;
                        trueDistanceOfTarget = playerDist;
                        targetRectangle = Game1.mainPlayer.rigidBody.collisionRectangle;
                        //Set left or right
                        if (Game1.mainPlayer.rigidBody.collisionRectangle.Center.X < rigidBody.getOffsetCollisionRectangle().Center.X)
                        {
                            //Left
                            left = true;
                        }
                        else
                        {
                            //Right
                            left = false;
                        }
                    }
                }

                //Check otherplayers
                foreach (OtherPlayer b in OtherPlayer.otherPlayers)
                {
                    int otherPlayerDist = GameMath.distance(rigidBody.collisionRectangle.Center, b.drawRectangle.Center);
                    if (otherPlayerDist <= relativeWakeDistance)
                    {
                        //Player is in range
                        if (otherPlayerDist < minDist)
                        {
                            minDist = otherPlayerDist;
                            xValueOfTarget = b.drawRectangle.Center.X;
                            targetRectangle = b.collisionRectangle;
                            trueDistanceOfTarget = otherPlayerDist;
                            //Set left or right
                            if (b.drawRectangle.Center.X < rigidBody.collisionRectangle.Center.X)
                            {
                                //Left
                                left = true;
                            }
                            else
                            {
                                //Right
                                left = false;
                            }
                        }
                    }
                }
            }

            //Check for MIN destination reached
            if (trueDistanceOfTarget <= minDistanceFromTarget)
            {
                //Min destination distance has been reached
                OnDestinationReached.Invoke(this, null);
                return false;
            }

            //Check for XVALUE destination reached
            if (xValueOfTarget != 0)
            {
                if (Math.Abs(xValueOfTarget - rigidBody.collisionRectangle.X) <= (targetRectangle.Width / 2) + (rigidBody.collisionRectangle.Width / 2))
                {
                    //Should not continue x movement
                    return false;
                }
            }

            if (minDist == (int)(relativeWakeDistance + 1))
            {
                //No targets selected
                return false;
            }
            else
            {
                return true;
            }
        }

        private void addWakeEvent(bool remove = false)
        {
            //Find rows and columns containing the rigidbody
            List<int> columnsContaining = new List<int>();
            columnsContaining.Add((int)Math.Floor((float)(rigidBody.collisionRectangle.Left - 1) / Game1.currentMap.tileSize));
            columnsContaining.Add((int)Math.Floor((float)(rigidBody.collisionRectangle.Right - 1) / Game1.currentMap.tileSize));
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
            List<int> rowsContaining = new List<int>();
            rowsContaining.Add((int)Math.Floor((float)(rigidBody.collisionRectangle.Top - 1) / Game1.currentMap.tileSize));
            rowsContaining.Add((int)Math.Floor((float)(rigidBody.collisionRectangle.Bottom - 1) / Game1.currentMap.tileSize));
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

            //Add wake event to all tiles that surround the rigidbody if they are not equal to null
            if (columnsContaining.Count > 0 && rowsContaining.Count > 0)
            {
                //Find majors of columns and rows
                int leftestColumn = columnsContaining[0], rightestColumn = columnsContaining[0];
                foreach (int b in columnsContaining)
                {
                    if (b < leftestColumn)
                    {
                        leftestColumn = b;
                    }
                    if (b > rightestColumn)
                    {
                        rightestColumn = b;
                    }
                }
                int highestRow = rowsContaining[0], lowestRow = rowsContaining[0];
                foreach (int b in rowsContaining)
                {
                    if (b < highestRow)
                    {
                        highestRow = b;
                    }
                    if (b > lowestRow)
                    {
                        lowestRow = b;
                    }
                }

                //Tiles above
                int yAboveValue = (int)GameMath.clamp((int)Math.Floor((float)(rigidBody.collisionRectangle.Top + relativeJumpHeight) / Game1.currentMap.tileSize), 0, Game1.currentMap.tileMap.GetLength(1));
                for (int i = (int)GameMath.clamp(leftestColumn - 1, 0, Game1.currentMap.tileMap.GetLength(0)); i <= (int)GameMath.clamp(rightestColumn + 1, 0, Game1.currentMap.tileMap.GetLength(1)); i++)
                {
                    for (int j = yAboveValue; j < highestRow; j++)
                    {
                        if (Game1.currentMap.tileMap[i, j] != null)
                        {
                            //Add wake event
                            if (!remove)
                            {
                                Game1.currentMap.tileMap[i, j].OnBreak += (sender, e) =>
                                {
                                    //Wake event
                                    dormant = false;
                                };
                            }
                            else
                            {
                                Game1.currentMap.tileMap[i, j].OnBreak -= (sender, e) =>
                                {
                                    //Wake event
                                    dormant = false;
                                };
                            }
                        }
                    }
                }

                //Tiles to the left
                if (leftestColumn > 0)
                {
                    for (int j = (int)GameMath.clamp(highestRow - 1, 0, Game1.currentMap.tileMap.GetLength(1)); j <= (int)GameMath.clamp(lowestRow + 1, 0, Game1.currentMap.tileMap.GetLength(1)); j++)
                    {
                        if (Game1.currentMap.tileMap[leftestColumn - 1, j] != null)
                        {
                            //Add wake event
                            if (!remove)
                            {
                                Game1.currentMap.tileMap[leftestColumn - 1, j].OnBreak += (sender, e) =>
                                {
                                    //Wake event
                                    dormant = false;
                                };
                            }
                            else
                            {
                                Game1.currentMap.tileMap[leftestColumn - 1, j].OnBreak -= (sender, e) =>
                                {
                                    //Wake event
                                    dormant = false;
                                };
                            }
                        }
                    }
                }

                //Tiles to the right
                if (rightestColumn < Game1.currentMap.tileMap.GetLength(0) - 1)
                {
                    for (int j = (int)GameMath.clamp(highestRow - 1, 0, Game1.currentMap.tileMap.GetLength(1)); j <= (int)GameMath.clamp(lowestRow + 1, 0, Game1.currentMap.tileMap.GetLength(1)); j++)
                    {
                        if (Game1.currentMap.tileMap[rightestColumn + 1, j] != null)
                        {
                            //Add wake event
                            if (!remove)
                            {
                                Game1.currentMap.tileMap[rightestColumn + 1, j].OnBreak += (sender, e) =>
                                {
                                    //Wake event
                                    dormant = false;
                                };
                            }
                            else
                            {
                                Game1.currentMap.tileMap[rightestColumn + 1, j].OnBreak -= (sender, e) =>
                                {
                                    //Wake event
                                    dormant = false;
                                };
                            }
                        }
                    }
                }

                //Tiles below
                if (lowestRow < Game1.currentMap.tileMap.GetLength(1) - 1)
                {
                    for (int i = (int)GameMath.clamp(leftestColumn - 1, 0, Game1.currentMap.tileMap.GetLength(0)); i <= (int)GameMath.clamp(rightestColumn + 1, 0, Game1.currentMap.tileMap.GetLength(1)); i++)
                    {
                        if (Game1.currentMap.tileMap[i, lowestRow + 1] != null)
                        {
                            //Add wake event
                            if (!remove)
                            {
                                Game1.currentMap.tileMap[i, lowestRow + 1].OnBreak += (sender, e) =>
                                {
                                    //Wake event
                                    dormant = false;
                                };
                            }
                            else
                            {
                                Game1.currentMap.tileMap[i, lowestRow + 1].OnBreak -= (sender, e) =>
                                {
                                    //Wake event
                                    dormant = false;
                                };
                            }
                        }
                    }
                }
            }
        }

        private int relativeJumpHeight
        {
            get
            {
                return (int)((-Math.Pow((jumpVelocity * Game1.currentMap.tileSize), 2)) / (2 * (Physics.gravityAcceleration * Game1.currentMap.tileSize)));
            }
        }
    }
}
