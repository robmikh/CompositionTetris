using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompositionTetris
{
    struct TilePosition
    {
        public int X;
        public int Y;

        public TilePosition(int x, int y) { X = x; Y = y; }

        public static TilePosition operator+(TilePosition left, TilePosition right)
        {
            return new TilePosition(left.X + right.X, left.Y + right.Y);
        }

        public static bool operator==(TilePosition left, TilePosition right)
        {
            return left.X == right.X && left.Y == right.Y;
        }

        public static bool operator!=(TilePosition left, TilePosition right)
        {
            return !(left == right);
        }

        public static float Distance(TilePosition position1, TilePosition position2)
        {
            return (float)Math.Sqrt(Math.Pow(position2.X - position1.X, 2) + Math.Pow(position2.Y - position1.Y, 2));
        }
    }
}
