using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System.Threading;

namespace CraftWar
{
    public class SurfaceTile
    {
        public enum SupportDependence
        {
            Up,
            Down,
            Left,
            Right
        }

        //Object
        public Texture2D texture;
        public SupportDependence supportDependence;
        public int[] parentMapPosition;
        public Color drawColor
        {
            get
            {
                try
                {
                    return Game1.currentMap.tileMap[parentMapPosition[0], parentMapPosition[1]].lightingManager.tileColor;
                }
                catch
                {
                    return Color.White;
                }
            }
        }

        public SurfaceTile(Texture2D texture, SupportDependence supportDependence, int[] mapPosition)
        {
            this.texture = texture;
            this.supportDependence = supportDependence;
            this.parentMapPosition = mapPosition;
        }
    }
}
