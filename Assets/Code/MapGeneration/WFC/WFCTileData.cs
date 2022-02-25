using System;

namespace Balma.WFC
{
    public struct WFCTileData
    {
        private Tile.ConnectionDataProxy cd0;
        private Tile.ConnectionDataProxy cd1;
        private Tile.ConnectionDataProxy cd2;
        private Tile.ConnectionDataProxy cd3;
        private Tile.ConnectionDataProxy cd4;
        private Tile.ConnectionDataProxy cd5;

        //TODO access memory directly avoiding branching (like float3)
        public Tile.ConnectionDataProxy this[int index]
        {
            get => index switch
            {
                0 => cd0,
                1 => cd1,
                2 => cd2,
                3 => cd3,
                4 => cd4,
                5 => cd5,
                _ => throw new IndexOutOfRangeException()
            };
            set
            {
                switch (index)
                {
                    case 0: cd0 = value; break;
                    case 1: cd1 = value; break;
                    case 2: cd2 = value; break;
                    case 3: cd3 = value; break;
                    case 4: cd4 = value; break;
                    case 5: cd5 = value; break;
                    default: throw new IndexOutOfRangeException();
                }
            }
        }
    }
}