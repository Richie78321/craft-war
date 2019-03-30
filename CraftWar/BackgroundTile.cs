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
    public class BackgroundTile
    {
        public Texture2D texture;
        public LightingManager lightingManager;
        public int[] mapPosition;

        public BackgroundTile(Texture2D texture, int[] mapPosition) 
        {
            this.mapPosition = mapPosition;
            this.texture = texture;
            lightingManager = new LightingManager(mapPosition, backgroundTile: true);
        }
    }
}
