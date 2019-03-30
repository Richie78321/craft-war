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
    public class SpritesheetManager
    {
        public SpriteEffects currentEffect;
        private Entity parentEntity;
        public float walkFrameDist;
        private float currentWalkFrameDist = 0;

        public Texture2D[,] spritesheet;
        public Texture2D currentTexture
        {
            get
            {
                return spritesheet[spritesheetPos[0], spritesheetPos[1]];
            }
        }

        public int[] spritesheetPos = new int[2];
        public SpritesheetManager(Entity parentEntity, float walkFrameDist, Texture2D[,] spritesheet)
        {
            this.parentEntity = parentEntity;
            this.walkFrameDist = walkFrameDist;
            this.spritesheet = spritesheet;
        }

        public void updateSpritesheetPos(float netXMovement, bool player = false)
        {
            if ((player && netXMovement > 0) || (!player && netXMovement < 0))
            {
                //Left player movement
                currentEffect = SpriteEffects.FlipHorizontally;
            }
            else if ((player && netXMovement < 0) || (!player && netXMovement > 0))
            {
                //Right player movement
                currentEffect = SpriteEffects.None;
            }

            if (parentEntity.rigidBody.onGround)
            {
                if (netXMovement == 0)
                {
                    //Standing
                    spritesheetPos = new[] { 0, 0 };
                }
                else
                {
                    //Walking
                    if (spritesheetPos[0] != 1)
                    {
                        //Initial walking
                        spritesheetPos = new[] { 1, 0 };
                    }

                    currentWalkFrameDist += Math.Abs(netXMovement);
                    if (currentWalkFrameDist >= walkFrameDist)
                    {
                        //Change frame
                        currentWalkFrameDist -= walkFrameDist;
                        spritesheetPos[1]++;

                        if (spritesheetPos[1] > spritesheet.GetLength(1) - 1)
                        {
                            //Reset walkTexturePos
                            spritesheetPos[1] = 0;
                        }
                    }
                }
            }
            else
            {
                //Jumping
                spritesheetPos = new[] { 2, 0 };
            }
        }
    }
}
