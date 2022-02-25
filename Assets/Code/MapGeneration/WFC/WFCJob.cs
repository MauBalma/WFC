using System;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

// ReSharper disable Unity.BurstLoadingManagedType
// ReSharper disable StaticMemberInGenericType

namespace Balma.WFC
{
    [BurstCompile]
    public struct WFCJob<TWFCRules> : IJob where TWFCRules : IWFCRules
    {
        private static readonly int3[] NeighbourDirection = new[]
        {
            new int3(1, 0,  0),
            new int3(0, 0,  1),
            new int3(-1, 0, 0),
            new int3(0, 0, -1),
            new int3(0, 1, 0),
            new int3(0, -1, 0),
        };
        
        private static readonly int[] ReciprocalDirection = new[]
        {
            2, 3, 0, 1, 5, 4,
        };
        
        public struct Data
        {
            public WFCDomain domain;
            public WFCStaticDomain staticDomain;

            public void Hint(int3 coordinates, TileKey tileKey, ref Data data)
            {
                var possibles = domain.possibleTiles[coordinates];
                
                if(possibles.Length == 0) return;
                if(!possibles.Contains(tileKey)) return;

                var collapsed = tileKey;
            
                possibles.Clear();
                possibles.Add(collapsed);
    
                domain.possibleTiles[coordinates] = possibles;
                domain.open.Push(coordinates, -1);//
    
                data.domain.propagateStack.Add(new WFCPropagateStackHelper(){coordinates = coordinates, omitIndex = -1});
                Propagate(ref this);
            }
        }

        private TWFCRules rules;
        private Data data;

        public WFCJob(TWFCRules rules, ref WFCStaticDomain staticDomain, ref WFCDomain domain)
        {
            this.rules = rules;
            data.domain = domain;
            data.staticDomain = staticDomain;
        }

        public void Execute()
        {
            Initialize();
            rules.ApplyInitialConditions(ref data);

            while (data.domain.open.Count > 0)
            {
                Observe(data.domain.open.Pop(), ref data);
                if(data.domain.contradiction.Value) return;//Abort
            }
        }

        private void Initialize()
        {
            data.domain.contradiction.Value = false;
            data.domain.open.Clear();
            data.domain.propagateStack.Clear();
            
            for (var i = 0; i < data.staticDomain.size.x; i++)
            for (var j = 0; j < data.staticDomain.size.y; j++)
            for (var k = 0; k < data.staticDomain.size.z; k++)
            {
                var coordinate = new int3(i, j, k);

                var possible = data.domain.possibleTiles[coordinate];
                possible.Clear();

                for (var tileIndex = 0; tileIndex < data.staticDomain.tileCount; tileIndex++)
                {
                    possible.Add(new TileKey(){index = tileIndex});
                }

                data.domain.possibleTiles[coordinate] = possible;//Reassign, is a struct not a reference
                data.domain.open.Push(coordinate, 1);
            }
        }

        private static void Observe(int3 coordinates, ref Data data)
        {
            var possibles = data.domain.possibleTiles[coordinates];
                
            if(possibles.Length <= 1) return;

            var collapsed = Collapse(possibles, ref data);
            
            possibles.Clear();
            possibles.Add(collapsed);
    
            data.domain.possibleTiles[coordinates] = possibles;
            
            data.domain.propagateStack.Add(new WFCPropagateStackHelper(){coordinates = coordinates, omitIndex = -1});
            Propagate(ref data);
        }

        private static void Propagate(ref Data data)
        {
            while (!data.domain.propagateStack.IsEmpty)
            {
                var entry = data.domain.propagateStack[data.domain.propagateStack.Length - 1];
                data.domain.propagateStack.RemoveAtSwapBack(data.domain.propagateStack.Length - 1);
                Propagate(entry.coordinates, ref data, entry.omitIndex);
                if(data.domain.contradiction.Value) return;//Abort
            }
        }

        private static TileKey Collapse(UnsafeList<TileKey> possibles, ref Data data)
        {
            var totalWeight = 0f;

            for (var index = 0; index < possibles.Length; index++)
            {
                var possible = possibles[index];
                totalWeight += data.staticDomain.tileWeight[possible.index];
            }

            var value = data.domain.rng.NextFloat(totalWeight);
            var accWeight = 0f;

            for (var index = 0; index < possibles.Length; index++)
            {
                var possible = possibles[index];

                if (value >= accWeight && value < accWeight + data.staticDomain.tileWeight[possible.index])
                {
                    return possible;
                }

                accWeight += data.staticDomain.tileWeight[possible.index];
            }

            throw new Exception();
        }

        private static void Propagate(int3 coordinates, ref Data data, int omitIndex = -1)
        {
            for (int i = 0; i < NeighbourDirection.Length; i++)
            {
                if(i == omitIndex) continue;
                    
                var neighbourCoordinates = coordinates + NeighbourDirection[i];
    
                if (neighbourCoordinates.x < 0 || neighbourCoordinates.x >= data.staticDomain.size.x ||
                    neighbourCoordinates.y < 0 || neighbourCoordinates.y >= data.staticDomain.size.y ||
                    neighbourCoordinates.z < 0 || neighbourCoordinates.z >= data.staticDomain.size.z)
                    continue;
    
                FilterPossibles(i, coordinates, neighbourCoordinates, ref data);
                
                if(data.domain.contradiction.Value) return;//Abort
            }
            
            void FilterPossibles(int directionIndex, int3 coordinates, int3 neighbourCoordinates, ref Data data)
            {
                var possibles = data.domain.possibleTiles[coordinates];
                
                if(possibles.Length == 0) return;
                
                var neighbourPossibles = data.domain.possibleTiles[neighbourCoordinates];
        
                var changePerformed = false;
        
                for (var np = neighbourPossibles.Length - 1; np >= 0; np--)
                {
                    var neighbourPossible = neighbourPossibles[np];
                    var neighbourTileData = data.staticDomain.tileDatas[neighbourPossible.index];
                    var neighbourConnectionData = neighbourTileData[ReciprocalDirection[directionIndex]];
        
                    var someMatch = false;
                        
                    for (var cp = possibles.Length - 1; cp >= 0; cp--)
                    {
                        var currentPossible = possibles[cp];
                        var currentTileData = data.staticDomain.tileDatas[currentPossible.index];
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

                        if (neighbourPossibles.IsEmpty)
                        {
                            data.domain.contradiction.Value = true;//Abort
                            return;
                        }
                    }
                }
        
                if (changePerformed)
                {
                    data.domain.possibleTiles[neighbourCoordinates] = neighbourPossibles;
    
                    var entropy = 0f;
                    for (int i = 0; i < neighbourPossibles.Length; i++)
                    {
                        //The greater the weight, lesser the entropy
                        entropy += 1f / data.staticDomain.tileWeight[neighbourPossibles[i].index];
                    }
                    
                    data.domain.open.Push(neighbourCoordinates, entropy);
                    data.domain.propagateStack.Add(new WFCPropagateStackHelper(){coordinates = neighbourCoordinates, omitIndex = ReciprocalDirection[directionIndex]});
                }
            }
        }

        private static bool CheckConnectionsMatch(Tile.ConnectionDataProxy current, Tile.ConnectionDataProxy neighbour)
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