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
    public class Particle
    {
        public const float massesOfPlayer = .25F;

        //Object
        public RigidBody rigidBody;
        public Texture2D texture;
        public int lifetime;

        public Particle(int particleSize, Vector2 startLocation, Texture2D texture, int lifetime, Vector2 initialVelocity = new Vector2())
        {
            startLocation.X -= particleSize / 2;
            startLocation.Y -= particleSize / 2;

            this.lifetime = lifetime;
            this.texture = texture;

            rigidBody = new RigidBody(massesOfPlayer);
            rigidBody.collisionRectangle = new Rectangle((int)startLocation.X, (int)startLocation.Y, particleSize, particleSize);
            rigidBody.nonRelativeVelocity += initialVelocity;
        }

        public void updateParticle(GameTime gameTime)
        {
            lifetime -= gameTime.ElapsedGameTime.Milliseconds;
            if (lifetime <= 0)
            {
                //Remove
                Game1.currentMap.particleRemoveQueue.Add(this);
                return;
            }

            rigidBody.applyGravity();
            rigidBody.collisionRectangle = new Rectangle(rigidBody.collisionRectangle.X + (int)rigidBody.xMovementPossible(), rigidBody.collisionRectangle.Y + (int)rigidBody.yMovementPossible(), rigidBody.collisionRectangle.Width, rigidBody.collisionRectangle.Height);
            rigidBody.applyFriction();
        }

        public virtual void draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(texture, rigidBody.collisionRectangle, LightingManager.entityLightingColor(rigidBody.collisionRectangle));
        }
    }

    public class TileParticle : Particle
    {
        public Rectangle sourceRectangle;

        public TileParticle(int particleSize, Vector2 startLocation, Texture2D texture, int lifetime, Random random, Vector2 initialVelocity = new Vector2()) : base(particleSize, startLocation, texture, lifetime, initialVelocity)
        {
            sourceRectangle = new Rectangle(random.Next(0, texture.Width - particleSize), random.Next(0, texture.Height - particleSize), particleSize, particleSize);
        }

        public override void draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(texture, rigidBody.collisionRectangle, sourceRectangle, LightingManager.entityLightingColor(rigidBody.collisionRectangle));
        }
    }
}

