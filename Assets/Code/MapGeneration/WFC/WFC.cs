using System;
using Unity.Mathematics;

namespace Balma.WFC
{
    public static class WFC
    {
        public static readonly int3[] NeighbourDirection = new[]
        {
            new int3(1, 0,  0),
            new int3(0, 0,  1),
            new int3(-1, 0, 0),
            new int3(0, 0, -1),
            new int3(0, 1, 0),
            new int3(0, -1, 0),
        };
        
        public static readonly int[] ReciprocalDirection = new[]
        {
            2, 3, 0, 1, 5, 4,
        };
        
        public static bool CheckConnectionsMatch(Tile.ConnectionDataProxy current, Tile.ConnectionDataProxy neighbour)
        {
            if (current.type != neighbour.type) return false;
            if (current.rotation != neighbour.rotation) return false;

            return current.direction switch
            {
                Tile.ConnectionDirection.Symmetrical => neighbour.direction == Tile.ConnectionDirection.Symmetrical,
                Tile.ConnectionDirection.Forward => neighbour.direction == Tile.ConnectionDirection.Backwards,
                Tile.ConnectionDirection.Backwards => neighbour.direction == Tile.ConnectionDirection.Forward,
                _ => throw new Exception()
            };
        }
    }
}