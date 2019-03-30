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
    class Projectile : Entity
    {
        public static Texture2D flyingBulletTexture;

        //Object
        public Texture2D texture;
        public bool affectedByGravity;
        private float textureHeightToWidthRatio;
        private float collisionRectangleHeightToWidthRatio;

        public Projectile(Texture2D texture, Vector2 initialVelocity, RigidBody rigidBody, int damage, bool affectedByGravity = true, Entity[] entityBlacklist = null)
        {
            this.rigidBody = rigidBody;
            this.texture = texture;
            this.affectedByGravity = affectedByGravity;
            textureHeightToWidthRatio = (float)texture.Height / texture.Width;
            collisionRectangleHeightToWidthRatio = (float)rigidBody.collisionRectangle.Height / rigidBody.collisionRectangle.Width;
            rigidBody.nonRelativeVelocity = initialVelocity;

            rigidBody.OnEntityCollisionEvent += (entityCollided) =>
            {
                //Add event for collision with entity
                Entity entityCollidedWith = (Entity)entityCollided;
                if (entityCollidedWith.statManager != null) entityCollidedWith.statManager.health -= damage;

                //Remove self
                Game1.currentMap.entityRemoveQueue.Add(this);
            };
            rigidBody.OnTileCollisionEvent += (tileCollided) =>
            {
                //Add event for collision with tile
                //Remove self
                Game1.currentMap.entityRemoveQueue.Add(this);
            };

            //Adds shooter to blacklist
            if (entityBlacklist != null)
            {
                rigidBody.entityCollisionBlacklist.AddRange(entityBlacklist);
            }
        }

        public override void draw(SpriteBatch spriteBatch)
        {
            float currentAngle = currentRotation;
            pastAngle = currentAngle;

            spriteBatch.Draw(texture, new Rectangle(rigidBody.collisionRectangle.Center.X, rigidBody.collisionRectangle.Center.Y, rigidBody.collisionRectangle.Width, (int)(textureHeightToWidthRatio * rigidBody.collisionRectangle.Width)), null, Color.White, currentAngle, new Vector2(texture.Width / 2, texture.Height - ((collisionRectangleHeightToWidthRatio * texture.Width) / 2)), SpriteEffects.None, 0);
            //Debug collision rectangle
            //spriteBatch.Draw(TerrainTileType.dirtTile.texture, rigidBody.collisionRectangle, Color.White);
        }

        private float pastAngle = 0;
        private float currentRotation
        {
            get
            {
                float angle = (float)Math.Abs(Math.Atan(rigidBody.nonRelativeVelocity.X / rigidBody.nonRelativeVelocity.Y));

                if (rigidBody.nonRelativeVelocity.X > 0)
                {
                    if (rigidBody.nonRelativeVelocity.Y > 0)
                    {
                        return (float)((360 * (Math.PI / 180)) - angle);
                    }
                    else if (rigidBody.nonRelativeVelocity.Y < 0)
                    {
                        return (float)((180 * (Math.PI / 180)) + angle);
                    }
                    else
                    {
                        //Y velocity is zero
                        return (float)(270 * (Math.PI / 180));
                    }
                }
                else if (rigidBody.nonRelativeVelocity.X < 0)
                {
                    if (rigidBody.nonRelativeVelocity.Y > 0)
                    {
                        return angle;
                    }
                    else if (rigidBody.nonRelativeVelocity.Y < 0)
                    {
                        return (float)((180 * (Math.PI / 180)) - angle);
                    }
                    else
                    {
                        //Y velocity is zero
                        return (float)(90 * (Math.PI / 180));
                    }
                }
                else
                {
                    //X velocity is zero
                    if (rigidBody.nonRelativeVelocity.Y > 0)
                    {
                        return 0;
                    }
                    else if (rigidBody.nonRelativeVelocity.Y < 0)
                    {
                        return (float)(180 * (Math.PI / 180));
                    }
                    else
                    {
                        //No velocity
                        return pastAngle;
                    }
                }
            }
        }

        public override void update()
        {
            if (affectedByGravity) rigidBody.applyGravity();
            rigidBody.collisionRectangle.X += (int)rigidBody.xMovementPossible();
            rigidBody.collisionRectangle.Y += (int)rigidBody.yMovementPossible();
            rigidBody.checkForEntityCollision();
        }
    }
}
