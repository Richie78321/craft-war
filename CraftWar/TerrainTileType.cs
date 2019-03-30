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
    class TerrainTileType
    {
        public static TileType grassTile;
        public static TileType dirtTile;
        public static TileType stoneTile;
        public static TileType borderTile;
        public static TileType logTile;
        public static TileType woodTile;
        public static TileType leavesTile;
        public static TileType grassSurfaceTile;
        public static TileType surfaceDirtSurfaceTile;
        public static TileType surfaceDirtTile;
        public static TileType branchTile;
    }

    class OreTileType
    {
        public static TileType coalOre;
        public static TileType copperOre;
        public static TileType ironOre;
        public static TileType goldOre;
        public static TileType diamondOre;
    }

    class LightingTileType
    {
        public static TileType torchTile;
        public static TileType playerGlowTile;
    }

    class FunctioningTileType
    {
        public static TileType doorTile;
    }

    class SurfaceTiles
    {
        public static Texture2D dirtRight;
        public static Texture2D dirtLeft;
        public static Texture2D dirtBottom;

        public static Texture2D stoneTop;
        public static Texture2D stoneRight;
        public static Texture2D stoneLeft;
        public static Texture2D stoneBottom;

        public static Texture2D logLeft;
        public static Texture2D logRight;

        public static Texture2D rootLeft;
        public static Texture2D rootRight;

        public static Texture2D leavesTop;
        public static Texture2D leavesBottom;
        public static Texture2D leavesRight;
        public static Texture2D leavesLeft;
    }
}
