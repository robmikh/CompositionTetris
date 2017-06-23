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
    }
}
