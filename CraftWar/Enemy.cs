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
    public class Enemy : Entity
    {
        public delegate void Attack(object target);

        //Object
        public MovementAI movementAI;
        public int attackTime;
        public Attack attackAction;
        public SpritesheetManager spritesheetManager;

        public Enemy(Texture2D standTexture, Texture2D[] walkTexture, Texture2D jumpTexture, int[] drawDimensions, float walkFrameDist, RigidBody rigidBody, MovementAI movementAI, StatManager statManager, int attackTime, Attack attackAction)
        {
            this.attackAction = attackAction;
            this.attackTime = attackTime;
            movementAI.OnDestinationReached += attack;
            this.movementAI = movementAI;
            this.rigidBody = rigidBody;
            movementAI.rigidBody = rigidBody;
            this.statManager = statManager;
            this.drawDimensions = drawDimensions;

            Texture2D[,] spritesheet = new Texture2D[3, walkTexture.Length];
            spritesheet[0, 0] = standTexture;
            for (int i = 0; i < walkTexture.Length; i++) spritesheet[1, i] = walkTexture[i];
            spritesheet[2, 0] = jumpTexture;
            spritesheetManager = new SpritesheetManager(this, walkFrameDist, spritesheet);
        }

        private long timeOfLastAttack = 0;
        private void attack(object sender, EventArgs e)
        {   
            if (Game1.gameTime.TotalGameTime.TotalMilliseconds - timeOfLastAttack >= attackTime)
            {
                //Can attack
                timeOfLastAttack = (int)Game1.gameTime.TotalGameTime.TotalMilliseconds;

                //Attack
                attackAction.Invoke(((MovementAI)sender).targetObject);
            }
        }

        public int[] drawDimensions;
        public override void draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(spritesheetManager.currentTexture, new Rectangle(rigidBody.collisionRectangle.X - ((drawDimensions[0] - rigidBody.collisionRectangle.Width) / 2), rigidBody.collisionRectangle.Y - ((drawDimensions[1] - rigidBody.collisionRectangle.Height) / 2), drawDimensions[0], drawDimensions[1]), null, LightingManager.entityLightingColor(rigidBody.collisionRectangle), 0, Vector2.Zero, spritesheetManager.currentEffect, 0);
        }

        public override void update()
        {
            movementAI.updateRigidbody(spritesheetManager);
            movementAI.updateAI();
        }
    }
}
