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
    public class TileRaycast
    {
        public Vector2 startPos;
        public Vector2 endPos;

        public TileRaycast(Vector2 startPos, Vector2 endPos)
        {
            this.startPos = startPos;
            this.endPos = endPos;
            updateLineInfo();
        }
        
        public TileRaycast(Vector2 startPos, float distance, float angleDirection)
        {
            this.startPos = startPos;
            Vector2 endPosCreated = new Vector2(startPos.X, startPos.Y - distance);

            //Rotate endPosCreated
            float xWithout = endPosCreated.X - startPos.X;
            float yWithout = endPosCreated.Y - startPos.Y;
            endPosCreated.X = (float)(startPos.X + ((xWithout * Math.Cos(angleDirection)) - (yWithout * Math.Sin(angleDirection))));
            endPosCreated.Y = (float)(startPos.Y + ((yWithout * Math.Sin(angleDirection)) + (yWithout * Math.Cos(angleDirection))));

            updateLineInfo();
        }

        public void updateLineInfo()
        {
            lineSlope = (endPos.Y - startPos.Y) / (endPos.X - startPos.X);
            lineYIntercept = startPos.Y - (lineSlope * startPos.X);
        }

        private float lineYIntercept;
        private float lineSlope;
        public bool intersectsTile
        {
            get
            {
                //Get min and max of tile locations
                int minX, maxX, minY, maxY;
                if (startPos.X > endPos.X)
                {
                    minX = (int)Math.Floor(endPos.X / Game1.currentMap.tileSize);
                    maxX = (int)Math.Floor(startPos.X / Game1.currentMap.tileSize);
                }
                else
                {
                    maxX = (int)Math.Floor(endPos.X / Game1.currentMap.tileSize);
                    minX = (int)Math.Floor(startPos.X / Game1.currentMap.tileSize);
                }
                if (startPos.Y > endPos.Y)
                {
                    minY = (int)Math.Floor(endPos.Y / Game1.currentMap.tileSize);
                    maxY = (int)Math.Floor(startPos.Y / Game1.currentMap.tileSize);
                }
                else
                {
                    maxY = (int)Math.Floor(endPos.Y / Game1.currentMap.tileSize);
                    minY = (int)Math.Floor(startPos.Y / Game1.currentMap.tileSize);
                }

                //Horizontal
                for (int i = (int)GameMath.clamp(minX, 0, Game1.currentMap.tileMap.GetLength(0)); i < (int)GameMath.clamp(maxX, 0, Game1.currentMap.tileMap.GetLength(0)); i++)
                {
                    int currentRow = (int)Math.Floor(((lineSlope * (i * Game1.currentMap.tileSize)) + lineYIntercept) / Game1.currentMap.tileSize);
                    if (currentRow >= 0 && currentRow < Game1.currentMap.tileMap.GetLength(1))
                    {
                        if (Game1.currentMap.tileMap[i, currentRow] != null && Game1.currentMap.tileMap[i, currentRow].collidable && Game1.currentMap.tileMap[i, currentRow].tileType != TerrainTileType.borderTile)
                        {
                            //Will collide
                            return true;
                        }
                    }
                }

                //Vertical
                for (int j = (int)GameMath.clamp(minY, 0, Game1.currentMap.tileMap.GetLength(1)); j < (int)GameMath.clamp(maxY, 0, Game1.currentMap.tileMap.GetLength(1)); j++)
                {
                    int currentColumn = (int)Math.Floor((((j * Game1.currentMap.tileSize) - lineYIntercept) / lineSlope) / Game1.currentMap.tileSize);
                    if (currentColumn >= 0 && currentColumn < Game1.currentMap.tileMap.GetLength(1))
                    {
                        if (Game1.currentMap.tileMap[currentColumn, j] != null && Game1.currentMap.tileMap[currentColumn, j].collidable && Game1.currentMap.tileMap[currentColumn, j].tileType != TerrainTileType.borderTile)
                        {
                            //Will collide
                            return true;
                        }
                    }
                }

                return false;
            }
        }
    }
}
