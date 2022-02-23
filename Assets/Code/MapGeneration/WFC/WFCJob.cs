using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace Balma.WFC
{
    public interface IWFCRules
    {
        void ApplyInitialConditions(ref WFCDomain domain);
    }
    
    public struct PropagateStackHelper
    {
        public int3 coordinates;
        public int omitIndex;
    }

    [BurstCompile]
    public struct WFCJob<TWFCRules> : IJob where TWFCRules : IWFCRules
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
        
        private static readonly int[] ReciprocalDirection = new int[]
        {
            2,
            3,
            0,
            1,
            5,
            4,
        };

        public TWFCRules rules;
        public WFCDomain domain;
        //[DeallocateOnJobCompletion] public UnsafeList<WFCDomain> domainStack;

        public void Execute()
        {
            InitializePossibleTiles();
            rules.ApplyInitialConditions(ref domain);

            while (domain.open.Count > 0)
            {
                /*domainStack.Add*///domain.Copy().Dispose();
                Observe(domain.open.Pop(), ref domain);
            }

            // for (int i = 0; i < domainStack.Length; i++)
            // {
            //     domainStack[i].Dispose();
            // }
        }

        private void InitializePossibleTiles()
        {
            domain.open.Clear();
                
            for (var i = 0; i < domain.size.x; i++)
            for (var j = 0; j < domain.size.y; j++)
            for (var k = 0; k < domain.size.z; k++)
            {
                var coordinate = new int3(i, j, k);

                var tileList = domain.possibleTiles[coordinate];
                tileList.Clear();

                for (var tileIndex = 0; tileIndex < domain.tileCount; tileIndex++)
                {
                    tileList.Add(new TileKey(){index = tileIndex});
                }

                domain.possibleTiles[coordinate] = tileList;//Reassign, is a struct not a managed object
                domain.open.Push(coordinate, 1);
            }
        }
        
        public static void Hint(int3 coordinates, TileKey tileKey, ref WFCDomain domain)
        {
            var possibles = domain.possibleTiles[coordinates];
                
            if(possibles.Length == 0) return;
            if(!possibles.Contains(tileKey)) return;

            var collapsed = tileKey;
            
            possibles.Clear();
            possibles.Add(collapsed);
    
            domain.possibleTiles[coordinates] = possibles;
            domain.open.Push(coordinates, -1);//
    
            domain.propagateStack.Add(new PropagateStackHelper(){coordinates = coordinates, omitIndex = -1});
            Propagate(ref domain);
        }
            
        private static void Observe(int3 coordinates, ref WFCDomain domain)
        {
            var possibles = domain.possibleTiles[coordinates];
                
            if(possibles.Length <= 1) return;

            var collapsed = Collapse(possibles, ref domain);
            
            possibles.Clear();
            possibles.Add(collapsed);
    
            domain.possibleTiles[coordinates] = possibles;
            
            domain.propagateStack.Add(new PropagateStackHelper(){coordinates = coordinates, omitIndex = -1});
            Propagate(ref domain);
        }

        private static void Propagate(ref WFCDomain domain)
        {
            while (!domain.propagateStack.IsEmpty)
            {
                var entry = domain.propagateStack[domain.propagateStack.Length - 1];
                domain.propagateStack.RemoveAtSwapBack(domain.propagateStack.Length - 1);
                Propagate(entry.coordinates, ref domain, entry.omitIndex);
            }
        }

        private static TileKey Collapse(UnsafeList<TileKey> possibles, ref WFCDomain domain)
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

        private static void Propagate(int3 coordinates, ref WFCDomain domain, int omitIndex = -1)
        {
            for (int i = 0; i < NeighbourDirection.Length; i++)
            {
                if(i == omitIndex) continue;
                    
                var neighbourCoordinates = coordinates + NeighbourDirection[i];
    
                if (neighbourCoordinates.x < 0 || neighbourCoordinates.x >= domain.size.x ||
                    neighbourCoordinates.y < 0 || neighbourCoordinates.y >= domain.size.y ||
                    neighbourCoordinates.z < 0 || neighbourCoordinates.z >= domain.size.z)
                    continue;
    
                FilterPossibles(i, coordinates, neighbourCoordinates, ref domain);
            }
            
            void FilterPossibles(int directionIndex, int3 coordinates, int3 neighbourCoordinates, ref WFCDomain domain)
            {
                var possibles = domain.possibleTiles[coordinates];
                
                if(possibles.Length == 0) return;
                
                var neighbourPossibles = domain.possibleTiles[neighbourCoordinates];
        
                var changePerformed = false;
        
                for (var np = neighbourPossibles.Length - 1; np >= 0; np--)
                {
                    var neighbourPossible = neighbourPossibles[np];
                    var neighbourTileData = domain.tileDatas[neighbourPossible.index];
                    var neighbourConnectionData = neighbourTileData[ReciprocalDirection[directionIndex]];
        
                    var someMatch = false;
                        
                    for (var cp = possibles.Length - 1; cp >= 0; cp--)
                    {
                        var currentPossible = possibles[cp];
                        var currentTileData = domain.tileDatas[currentPossible.index];
                        var currentConnectionData = currentTileData[directionIndex];
        
                        if (CheckConnectionsMatch(currentConnectionData,neighbourConnectionData))
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
    
                    var entropy = 0f;
                    for (int i = 0; i < neighbourPossibles.Length; i++)
                    {
                        //The greater the weight, lesser the entropy
                        entropy += 1f / domain.tileWeight[neighbourPossibles[i].index];
                    }
                    
                    domain.open.Push(neighbourCoordinates, entropy);
                    domain.propagateStack.Add(new PropagateStackHelper(){coordinates = neighbourCoordinates, omitIndex = ReciprocalDirection[directionIndex]});
                }
            }
        }

        private static bool CheckConnectionsMatch(Tile.ConnectionDataProxy current, Tile.ConnectionDataProxy neighbour)
        {
            if (current.type != neighbour.type) return false;
            if (current.rotation != neighbour.rotation) return false;
            switch (current.direction)
            {
                case Tile.ConnectionDirection.Symmetrical:
                    return neighbour.direction == Tile.ConnectionDirection.Symmetrical;
                case Tile.ConnectionDirection.Forward:
                    return neighbour.direction == Tile.ConnectionDirection.Backwards;
                case Tile.ConnectionDirection.Backwards:
                    return neighbour.direction == Tile.ConnectionDirection.Forward;
            }

            throw new Exception();
        }
    }

    
}