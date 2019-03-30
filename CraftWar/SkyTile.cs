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
    public class Background
    {
        public SkyTile[] skyTiles;
        public string name;

        public Background(string name, SkyTile[] skyTiles)
        {
            this.skyTiles = skyTiles;
            this.name = name;

            foreach (SkyTile b in skyTiles)
            {
                if (b.depthLevel > maxDepthLevel) maxDepthLevel = b.depthLevel;
            }
        }

        public void updateSkyTiles(int screenWidth)
        {
            foreach (SkyTile b in skyTiles)
            {
                b.currentNonRelativeOffsetAddition += b.nonRelativeOffsetChangePerTick;
            }
        }

        private int maxDepthLevel = 0;
        public void drawSkyTiles(SpriteBatch spriteBatch, int screenHeight, int screenWidth)
        {
            for (int i = maxDepthLevel; i >= 0; i--)
            {
                foreach (SkyTile b in skyTiles)
                {
                    if (b.depthLevel == i)
                    {
                        b.draw(spriteBatch, screenHeight, screenWidth);
                    }
                }
            }
        }
    }

    public class SkyTile
    {
        public Texture2D texture;
        public float offset(int screenWidth)
        {
            return ((((Game1.mainPlayer.relativeOffset.X * (1F / depthLevel)) + (currentNonRelativeOffsetAddition * Game1.currentMap.tileSize))) % screenWidth);
        }
        private float widthToHeightRatio;
        public Rectangle drawRectangle(float offset, int screenHeight)
        {
            return new Rectangle((int)offset, 0, (int)(widthToHeightRatio * screenHeight), screenHeight);
        }
        public int depthLevel;
        public float nonRelativeOffsetChangePerTick = 0;
        public float currentNonRelativeOffsetAddition = 0;

        public SkyTile(Texture2D texture, int depthLevel)
        {
            this.texture = texture;
            this.depthLevel = depthLevel;
            widthToHeightRatio = (float)texture.Width / texture.Height;
        }

        public SkyTile(Texture2D texture, int depthLevel, float nonRelativeOffsetChangePerTick)
        {
            this.texture = texture;
            this.depthLevel = depthLevel;
            widthToHeightRatio = (float)texture.Width / texture.Height;
            this.nonRelativeOffsetChangePerTick = nonRelativeOffsetChangePerTick;
        }

        public void draw(SpriteBatch spriteBatch, int screenHeight, int screenWidth)
        {
            Rectangle rectangleToDraw = drawRectangle(offset(screenWidth), screenHeight);
            spriteBatch.Draw(texture, rectangleToDraw, LightingManager.skyColor);

            Rectangle leftDrawRec = rectangleToDraw;
            while (leftDrawRec.X > 0)
            {
                leftDrawRec.X -= leftDrawRec.Width;
                spriteBatch.Draw(texture, leftDrawRec, LightingManager.skyColor);
            }

            Rectangle rightDrawRec = rectangleToDraw;
            while (rightDrawRec.Right < screenWidth)
            {
                rightDrawRec.X += rightDrawRec.Width;
                spriteBatch.Draw(texture, rightDrawRec, LightingManager.skyColor);
            }
        }
    }
}
