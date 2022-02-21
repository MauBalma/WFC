using System;

namespace Balma.WFC
{
    public struct TileKey : IEquatable<TileKey>
    {
        public int index;

        public bool Equals(TileKey other)
        {
            return index == other.index;
        }

        public override bool Equals(object obj)
        {
            return obj is TileKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return index;
        }
    }
}