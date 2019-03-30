using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System.Threading.Tasks;

namespace CraftWar
{
    public class LightingManager
    {
        public const int skyRadianceLevel = 6;
        public const int maxLightDist = 8;
        public const float backgroundTileLightPercentage = .25F;

        public static float skyLightIntensity = dayLightLevel;
        public static Color entityLightingColor(Rectangle collisionRectangle)
        {
            List<Tile> tilesContainingPlayer = new List<Tile>();
            //Find columns containing player
            List<int> columnsContaining = new List<int>();
            columnsContaining.Add((int)Math.Floor((float)(collisionRectangle.Left - 1) / Game1.currentMap.tileSize));
            columnsContaining.Add((int)Math.Floor((float)(collisionRectangle.Right - 1) / Game1.currentMap.tileSize));
            //Fill in inside border columns
            for (int i = 0; i < columnsContaining.Count; i++)
            {
                if (i + 1 < columnsContaining.Count)
                {
                    //Still in bounds of array
                    for (int j = columnsContaining[i] + 1; j < columnsContaining[i + 1]; j++)
                    {
                        columnsContaining.Insert(i + 1, j);
                    }
                }
            }
            //Find rows containing player
            List<int> rowsContaining = new List<int>();
            rowsContaining.Add((int)Math.Floor((float)(collisionRectangle.Top - 1) / Game1.currentMap.tileSize));
            rowsContaining.Add((int)Math.Floor((float)(collisionRectangle.Bottom - 1) / Game1.currentMap.tileSize));
            //Fill in inside border rows
            for (int i = 0; i < rowsContaining.Count; i++)
            {
                if (i + 1 < rowsContaining.Count)
                {
                    //Still in bounds of array
                    for (int j = rowsContaining[i] + 1; j < rowsContaining[i + 1]; j++)
                    {
                        rowsContaining.Insert(i + 1, j);
                    }
                }
            }
            //Add tiles
            for (int i = 0; i < columnsContaining.Count; i++)
            {
                for (int j = 0; j < rowsContaining.Count; j++)
                {
                    tilesContainingPlayer.Add(Game1.currentMap.tileMap[columnsContaining[i], rowsContaining[j]]);
                }
            }

            List<BackgroundTile> backgroundTilesContainingPlayer = new List<BackgroundTile>();
            //Add background tiles
            for (int i = 0; i < columnsContaining.Count; i++)
            {
                for (int j = 0; j < rowsContaining.Count; j++)
                {
                    if (Game1.currentMap.tileMap[columnsContaining[i], rowsContaining[j]] == null || !Game1.currentMap.tileMap[columnsContaining[i], rowsContaining[j]].collidable)
                    {
                        if (Game1.currentMap.backgroundTileMap[columnsContaining[i], rowsContaining[j]] != null)
                        {
                            backgroundTilesContainingPlayer.Add(Game1.currentMap.backgroundTileMap[columnsContaining[i], rowsContaining[j]]);
                        }
                        else
                        {
                            //Sky tile. Create psuedo
                            BackgroundTile psuedoBackgroundTile = new BackgroundTile(TerrainTileType.borderTile.textures[0], new[] { columnsContaining[i], rowsContaining[j] });
                            psuedoBackgroundTile.lightingManager.checkForLighting();
                            backgroundTilesContainingPlayer.Add(psuedoBackgroundTile);
                        }
                    }
                }
            }

            //Processing
            float brightestLightPercentageContainedIn = 0;
            foreach (Tile b in tilesContainingPlayer)
            {
                if (b != null)
                {
                    if (b.lightingManager.currentLightingPercentage > brightestLightPercentageContainedIn)
                    {
                        brightestLightPercentageContainedIn = b.lightingManager.currentLightingPercentage;
                    }
                }
            }
            foreach (BackgroundTile b in backgroundTilesContainingPlayer)
            {
                if (b.lightingManager.currentLightingPercentage / backgroundTileLightPercentage > brightestLightPercentageContainedIn)
                {
                    brightestLightPercentageContainedIn = b.lightingManager.currentLightingPercentage / backgroundTileLightPercentage;
                }
            }

            return new Color(brightestLightPercentageContainedIn, brightestLightPercentageContainedIn, brightestLightPercentageContainedIn);
        }
        public static Color skyColor
        {
            get
            {
                //Cornflowerblue
                return new Color((int)(100 * (skyLightIntensity * 3)), (int)(125 * (skyLightIntensity * 3)), (int)(237 * (skyLightIntensity * 3)));
            }
        }
        public static void updateSurroundingTiles(int[] mapPosition)
        {
            for (int i = (int)GameMath.clamp(mapPosition[0] - maxLightDist, 0, Game1.currentMap.tileMap.GetLength(0) - 1); i <= (int)GameMath.clamp(mapPosition[0] + maxLightDist, 0, Game1.currentMap.tileMap.GetLength(0) - 1); i++)
            {
                int distFromCenter = Math.Abs(i - mapPosition[0]);
                for (int j = (int)GameMath.clamp(mapPosition[1] - (maxLightDist - distFromCenter), 0, Game1.currentMap.tileMap.GetLength(1) - 1); j <= (int)GameMath.clamp(mapPosition[1] + (maxLightDist - distFromCenter), 0, Game1.currentMap.tileMap.GetLength(1) - 1); j++)
                {
                    //Update lighting
                    updateTileLighting(i, j);
                }
            }
        }
        private static void updateTileLighting(int i, int j)
        {
            Task task = new Task(() =>
            {
                if (Game1.currentMap.tileMap[i, j] != null)
                {
                    Game1.currentMap.tileMap[i, j].lightingManager.checkForLighting();
                }
                if (Game1.currentMap.backgroundTileMap[i, j] != null)
                {
                    Game1.currentMap.backgroundTileMap[i, j].lightingManager.checkForLighting();
                }
            });

            task.Start();
        }

        //Day-night cycle
        public const float nightLightLevel = .08F;
        public const float dayLightLevel = 1F;
        public const int minutesOfDay = 10;
        public const int minutesOfNight = 10;
        public const int minutesOfTransition = 5;

        public static int timeOfCurrentCycle = 0;
        public static LightCycleState currentCycle = LightCycleState.Day;
        private static LightCycleState pastCycle = LightCycleState.Transition;
        public enum LightCycleState
        {
            Day,
            Night,
            Transition
        }
        public static float transitionLightMovementPerMillisecond = (dayLightLevel - nightLightLevel) / ((minutesOfTransition * 60) * 1000);
        public static void updateDayNightCycle(GameTime gameTime)
        {
            if (currentCycle == LightCycleState.Day)
            {
                if (gameTime.TotalGameTime.TotalMilliseconds - timeOfCurrentCycle >= minutesOfDay * 60 * 1000)
                {
                    //Day is completed
                    timeOfCurrentCycle = (int)gameTime.TotalGameTime.TotalMilliseconds;
                    pastCycle = LightCycleState.Day;
                    currentCycle = LightCycleState.Transition;
                }
            }
            else if (currentCycle == LightCycleState.Night)
            {
                if (gameTime.TotalGameTime.TotalMilliseconds - timeOfCurrentCycle >= minutesOfNight * 60 * 1000)
                {
                    //Night is completed
                    timeOfCurrentCycle = (int)gameTime.TotalGameTime.TotalMilliseconds;
                    pastCycle = LightCycleState.Night;
                    currentCycle = LightCycleState.Transition;
                }
            }
            else if (currentCycle == LightCycleState.Transition)
            {
                if (gameTime.TotalGameTime.TotalMilliseconds - timeOfCurrentCycle >= minutesOfTransition * 60 * 1000)
                {
                    //Transition is completed
                    if (pastCycle == LightCycleState.Day)
                    {
                        timeOfCurrentCycle = (int)gameTime.TotalGameTime.TotalMilliseconds;
                        skyLightIntensity = nightLightLevel;
                        currentCycle = LightCycleState.Night;
                    }
                    else
                    {
                        timeOfCurrentCycle = (int)gameTime.TotalGameTime.TotalMilliseconds;
                        skyLightIntensity = dayLightLevel;
                        currentCycle = LightCycleState.Day;
                    }

                    pastCycle = LightCycleState.Transition;
                }
                else
                {
                    //Transition is not completed
                    if (pastCycle == LightCycleState.Day)
                    {
                        //Subtract light
                        skyLightIntensity = GameMath.clamp((float)(skyLightIntensity - (transitionLightMovementPerMillisecond * gameTime.ElapsedGameTime.TotalMilliseconds)), nightLightLevel, dayLightLevel);
                    }
                    else
                    {
                        //Add light
                        skyLightIntensity = GameMath.clamp((float)(skyLightIntensity + (transitionLightMovementPerMillisecond * gameTime.ElapsedGameTime.TotalMilliseconds)), nightLightLevel, dayLightLevel);
                    }
                }
            }
        }

        //Object
        public bool lightSource;
        public int[] mapPosition;
        public bool lit = false;
        private float skyLightingPercentage;
        private float tileLightingPercentage;
        public float currentLightingPercentage
        {
            get
            {
                if (lit)
                {
                    if (skyLightingPercentage * skyLightIntensity > tileLightingPercentage)
                    {
                        if (backgroundTile)
                        {
                            return skyLightingPercentage * skyLightIntensity * backgroundTileLightPercentage;
                        }
                        else
                        {
                            return skyLightingPercentage * skyLightIntensity;
                        }
                    }
                    else
                    {
                        //Light from tile
                        if (backgroundTile)
                        {
                            return tileLightingPercentage * backgroundTileLightPercentage;
                        }
                        else
                        {
                            return tileLightingPercentage;
                        }
                    }
                }
                else
                {
                    return 0;
                }
            }
        }
        public Color tileColor
        {
            get
            {
                if (lit)
                {
                    if (skyLightingPercentage * skyLightIntensity > tileLightingPercentage)
                    {
                        if (backgroundTile)
                        {
                            return new Color(skyLightingPercentage * skyLightIntensity * backgroundTileLightPercentage, skyLightingPercentage * skyLightIntensity * backgroundTileLightPercentage, skyLightingPercentage * skyLightIntensity * backgroundTileLightPercentage);
                        }
                        else
                        {
                            return new Color(skyLightingPercentage * skyLightIntensity, skyLightingPercentage * skyLightIntensity, skyLightingPercentage * skyLightIntensity);
                        }
                    }
                    else
                    {
                        //Light from tile
                        if (backgroundTile)
                        {
                            return new Color(tileLightingPercentage * backgroundTileLightPercentage, tileLightingPercentage * backgroundTileLightPercentage, tileLightingPercentage * backgroundTileLightPercentage);
                        }
                        else
                        {
                            return new Color(tileLightingPercentage, tileLightingPercentage, tileLightingPercentage);
                        }
                    }
                }
                else
                {
                    return Color.Black;
                }
            }
        }
        private bool backgroundTile;
        public byte radianceLevel;

        public LightingManager(int[] mapPosition, bool lightSource = false, bool backgroundTile = false)
        {
            this.backgroundTile = backgroundTile;
            this.mapPosition = mapPosition;
            this.lightSource = lightSource;
            if (lightSource)
            {
                lit = true;
            }
        }

        public void checkForLighting()
        {
            int currentMaxLightDist = maxLightDist;

            skyLightingPercentage = 0;
            tileLightingPercentage = 0;
            lit = false;

            for (int i = (int)GameMath.clamp( mapPosition[0] - currentMaxLightDist, 0, Game1.currentMap.tileMap.GetLength(0) - 1); i <= (int)GameMath.clamp( mapPosition[0] + currentMaxLightDist, 0, Game1.currentMap.tileMap.GetLength(0) - 1); i++)
            {
                int horizontalOffset = Math.Abs(i -  mapPosition[0]);
                for (int j = (int)GameMath.clamp( mapPosition[1] - (currentMaxLightDist - horizontalOffset), 0, Game1.currentMap.tileMap.GetLength(1) - 1); j <= (int)GameMath.clamp( mapPosition[1] + (currentMaxLightDist - horizontalOffset), 0, Game1.currentMap.tileMap.GetLength(1) - 1); j++)
                {
                    //Check for possible lighting
                    int currentDistance = GameMath.distance(new Point(i, j), new Point( mapPosition[0],  mapPosition[1]));
                    if (Game1.currentMap.tileMap[i, j] != null)
                    {
                        //Check if tile is radiant and in range                    
                        if (Game1.currentMap.tileMap[i, j].collidable || Game1.currentMap.tileMap[i, j].lightingManager.lightSource)
                        {
                            if (Game1.currentMap.tileMap[i, j].lightingManager.lightSource && currentDistance <= Game1.currentMap.tileMap[i, j].lightingManager.radianceLevel)
                            {
                                //Tile is in range. Light tile
                                lit = true;
                                float newLightingPercentage = (float)((Game1.currentMap.tileMap[i, j].lightingManager.radianceLevel - currentDistance) + 1) / Game1.currentMap.tileMap[i, j].lightingManager.radianceLevel;
                                if (newLightingPercentage > tileLightingPercentage)
                                {
                                    tileLightingPercentage = newLightingPercentage;
                                    //currentMaxLightDist = currentDistance;
                                }
                            }
                        }
                        else
                        {
                            //Tile not there
                            if (Game1.currentMap.backgroundTileMap[i, j] == null)
                            {
                                //Sky
                                if (currentDistance <= skyRadianceLevel)
                                {
                                    lit = true;
                                    float newLightingPercentage = (float)((skyRadianceLevel - currentDistance) + 1) / skyRadianceLevel;
                                    if (newLightingPercentage > skyLightingPercentage)
                                    {
                                        skyLightingPercentage = newLightingPercentage;
                                        //currentMaxLightDist = currentDistance;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        //Tile not there
                        if (Game1.currentMap.backgroundTileMap[i, j] == null)
                        {
                            if (currentDistance <= skyRadianceLevel)
                            {
                                lit = true;
                                float newLightingPercentage = (float)((skyRadianceLevel - currentDistance) + 1) / skyRadianceLevel;
                                if (newLightingPercentage > skyLightingPercentage)
                                {
                                    skyLightingPercentage = newLightingPercentage;
                                    //currentMaxLightDist = currentDistance;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
