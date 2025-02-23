using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FruitBoxHelper.Common
{
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public struct BOARD
    {
        public RECT rect;
        public float width;
        public float height;
        public float aspectRatio;
    }
}
