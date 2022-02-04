using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace Balma.WFC
{
    [BurstCompile]
    public struct WFCJob : IJob
    {
        private static readonly int3[] NeighbourDirection = new int3[]
        {
            new int3(1, 0,  0),
            new int3(0, 0,  1),
            new int3(-1, 0, 0),
            new int3(0, 0, -1),
            new int3(0, 1, 0),
            new int3(0, -1, 0),
        };

        private static readonly int4[] OwnFaceTypes = new int4[]
        {
            new int4(3, 0, 7, 4),
            new int4(0, 1, 4, 5),
            new int4(1, 2, 5, 6),
            new int4(2, 3, 6, 7),
            new int4(4, 5, 6, 7),
            new int4(0, 1, 2, 3),
        };

        private static readonly int4[] NeighbourFaceTypes = new int4[]
        {
            new int4(2, 1, 6, 5),
            new int4(3, 2, 7, 6),
            new int4(0, 3, 4, 7),
            new int4(1, 0, 5, 4),
            new int4(0, 1, 2, 3),
            new int4(4, 5, 6, 7),
        };
        
        private static readonly int[] ReciprocalDirection = new int[]
        {
            2,
            3,
            0,
            1,
            5,
            4,
        };
            
        public WFCDomain domain;

        public void Execute()
        {
            InitializePossibleTiles();

            while (domain.open.Count > 0)
            {
                Observe(domain.open.Pop());
            }
        }
            
        private void InitializePossibleTiles()
        {
            domain.open.Clear();
                
            for (var i = 0; i < domain.size.x; i++)
            for (var j = 0; j < domain.size.y; j++)
            for (var k = 0; k < domain.size.z; k++)
            {
                var coordinate = new int3(i, j, k);

                if (!domain.possibleTiles.TryGetValue(coordinate, out var tileList))
                    tileList = new UnsafeList<TileKey>(domain.tileCount, Allocator.Temp);
                else
                    tileList.Clear();

                for (var tileIndex = 0; tileIndex < domain.tileCount; tileIndex++)
                {
                    tileList.Add(new TileKey(){index = tileIndex});
                }

                domain.possibleTiles[coordinate] = tileList;
                domain.open.Push(coordinate, domain.tileCount);
            }
        }
            
        private void Observe(int3 coordinates)
        {
            var possibles = domain.possibleTiles[coordinates];
                
            if(possibles.Length == 0) return;

            var collapsed = Collapse(possibles);
            
            possibles.Clear();
            possibles.Add(collapsed);
    
            domain.possibleTiles[coordinates] = possibles;
    
            Propagate(coordinates);
        }

        private TileKey Collapse(UnsafeList<TileKey> possibles)
        {
            var totalWeight = 0f;

            for (var index = 0; index < possibles.Length; index++)
            {
                var possible = possibles[index];
                totalWeight += domain.tileWeight[possible.index];
            }

            var value = domain.rng.NextFloat(totalWeight);
            var accWeight = 0f;

            for (var index = 0; index < possibles.Length; index++)
            {
                var possible = possibles[index];

                if (value >= accWeight && value < accWeight + domain.tileWeight[possible.index])
                {
                    return possible;
                }

                accWeight += domain.tileWeight[possible.index];
            }

            throw new Exception();
        }

        private void Propagate(int3 coordinates, int omitIndex = -1)
        {
            for (int i = 0; i < NeighbourDirection.Length; i++)
            {
                if(i == omitIndex) continue;
                    
                var neighbourCoordinates = coordinates + NeighbourDirection[i];
    
                if (neighbourCoordinates.x < 0 || neighbourCoordinates.x >= domain.size.x ||
                    neighbourCoordinates.y < 0 || neighbourCoordinates.y >= domain.size.y ||
                    neighbourCoordinates.z < 0 || neighbourCoordinates.z >= domain.size.z)
                    continue;
    
                FilterPossibles(i, coordinates, neighbourCoordinates);
            }
        }
    
        private void FilterPossibles(int directionIndex, int3 coordinates, int3 neighbourCoordinates)
        {
            var possibles = domain.possibleTiles[coordinates];
            var neighbourPossibles = domain.possibleTiles[neighbourCoordinates];
    
            var ownFaceIndices = OwnFaceTypes[directionIndex];
            var neighbourFaceIndices = NeighbourFaceTypes[directionIndex];
    
            var changePerformed = false;
    
            for (var np = neighbourPossibles.Length - 1; np >= 0; np--)
            {
                var neighbourPossible = neighbourPossibles[np];
                var neighbourFaceTypes = domain.tileDatas[neighbourPossible.index];
                    
                var neighbourFace = (neighbourFaceTypes[neighbourFaceIndices[0]],
                    neighbourFaceTypes[neighbourFaceIndices[1]],
                    neighbourFaceTypes[neighbourFaceIndices[2]],
                    neighbourFaceTypes[neighbourFaceIndices[3]]);
    
                var someMatch = false;
                    
                for (var cp = possibles.Length - 1; cp >= 0; cp--)
                {
                    var currentPossible = possibles[cp];
                    var currentFaceTypes = domain.tileDatas[currentPossible.index];
                        
                    var currentFace = (currentFaceTypes[ownFaceIndices[0]],
                        currentFaceTypes[ownFaceIndices[1]],
                        currentFaceTypes[ownFaceIndices[2]],
                        currentFaceTypes[ownFaceIndices[3]]);
    
                    if (currentFace == neighbourFace)
                    {
                        someMatch = true;
                        break;
                    }
                }
    
                if (!someMatch)
                {
                    neighbourPossibles.RemoveAtSwapBack(np);
                    changePerformed = true;
                }
            }
    
            if (changePerformed)
            {
                domain.possibleTiles[neighbourCoordinates] = neighbourPossibles;
                domain.open.Push(neighbourCoordinates, neighbourPossibles.Length);
                Propagate(neighbourCoordinates, ReciprocalDirection[directionIndex]);
            }
        }
    }
}