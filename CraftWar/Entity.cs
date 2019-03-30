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
    public abstract class Entity
    {
        public RigidBody rigidBody;
        public StatManager statManager = null;

        public abstract void update();

        public abstract void draw(SpriteBatch spriteBatch);
    }
}