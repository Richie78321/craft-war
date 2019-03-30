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
    public class Drop : Entity
    {
        public const float dropToTileRatio = .25F;
        public const float massesOfPlayer = .25F;
        public const int dropLifeTime = 300000;

        public static SoundEffect dropPickupSoundEffect;

        //Object
        public Item[] itemsInDrop;
        public int timeRemaining;

        public Drop(Item[] itemsInDrop, RigidBody rigidBody, Vector2 startLocation)
        {
            this.itemsInDrop = itemsInDrop;
            this.rigidBody = rigidBody;

            startLocation.X -= (Game1.currentMap.tileSize * dropToTileRatio) / 2;
            startLocation.Y -= (Game1.currentMap.tileSize * dropToTileRatio) / 2;

            rigidBody.collisionRectangle = new Rectangle((int)startLocation.X, (int)startLocation.Y, (int)(Game1.currentMap.tileSize * dropToTileRatio), (int)(Game1.currentMap.tileSize * dropToTileRatio));

            Random random = Game1.currentMap.syncedRandom;
            rigidBody.nonRelativeVelocity.Y += random.Next(-1, 2) * .025F;
            rigidBody.nonRelativeVelocity.X += random.Next(-1, 2) * .025F;

            timeRemaining = dropLifeTime;
        }

        public override void update()
        {
            //Update physics
            rigidBody.applyGravity();
            rigidBody.collisionRectangle.Location = new Point(rigidBody.collisionRectangle.Location.X + (int)rigidBody.xMovementPossible(), rigidBody.collisionRectangle.Location.Y + (int)rigidBody.yMovementPossible());
            rigidBody.applyFriction();

            //Check for player pickup
            checkForPickup();

            //Removes time
            timeRemaining -= Game1.gameTime.ElapsedGameTime.Milliseconds;
            if (timeRemaining <= 0)
            {
                //Remove drop
                Game1.currentMap.entityRemoveQueue.Add(this);
            }
        }

        private void checkForPickup()
        {
            foreach (OtherPlayer b in OtherPlayer.otherPlayers)
            {
                Rectangle equivalentCollisionRectangle = new Rectangle((int)b.location.X + ((Game1.mainPlayer.drawRectangle.Width - Game1.mainPlayer.rigidBody.collisionRectangle.Width) / 2), (int)b.location.Y + (Game1.mainPlayer.drawRectangle.Height - Game1.mainPlayer.rigidBody.collisionRectangle.Height), Game1.mainPlayer.rigidBody.collisionRectangle.Width, Game1.mainPlayer.rigidBody.collisionRectangle.Height);
                if (GameMath.distance(equivalentCollisionRectangle.Center, rigidBody.collisionRectangle.Center) <= Game1.mainPlayer.dropPickupRadius)
                {
                    //Other player has picked up
                    //TEMP ADD SOUND OCCLUSION
                    dropPickupSoundEffect.Play();
                    Game1.currentMap.entityRemoveQueue.Add(this);
                    return;
                }
            }

            if (GameMath.distance(Game1.mainPlayer.rigidBody.collisionRectangle.Center, rigidBody.getOffsetCollisionRectangle().Center) <= Game1.mainPlayer.dropPickupRadius)
            {
                //Player is in range of drop
                if (Game1.mainPlayer.playerGUI.playerInventory.addItems(itemsInDrop))
                {
                    //Player has picked up drop (remove now)
                    dropPickupSoundEffect.Play();
                    Game1.currentMap.entityRemoveQueue.Add(this);
                    return;
                }
            }
        }

        public override void draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(itemsInDrop[0].itemType.itemTexture, rigidBody.collisionRectangle, LightingManager.entityLightingColor(rigidBody.collisionRectangle));
        }
    }
}

