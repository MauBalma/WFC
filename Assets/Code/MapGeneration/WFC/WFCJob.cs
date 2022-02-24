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
        void ApplyInitialConditions<T>(ref WFCJob<T>.Data data, ref NativeList<PropagateStackHelper> propagateStack) where T : IWFCRules;
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
        
        public struct Data
        {
            public WFCDomain staticDomain;

            public void Hint(int3 coordinates, TileKey tileKey, ref NativeList<PropagateStackHelper> propagateStack)
            {
                var possibles = staticDomain.possibleTiles[coordinates];
                
                if(possibles.Length == 0) return;
                if(!possibles.Contains(tileKey)) return;

                var collapsed = tileKey;
            
                possibles.Clear();
                possibles.Add(collapsed);
    
                staticDomain.possibleTiles[coordinates] = possibles;
                staticDomain.open.Push(coordinates, -1);//
    
                propagateStack.Add(new PropagateStackHelper(){coordinates = coordinates, omitIndex = -1});
                Propagate(ref this, ref propagateStack);
            }
        }

        public TWFCRules rules;
        public Data data;
        public NativeList<PropagateStackHelper> propagateStack;

        public WFCJob(TWFCRules rules, WFCDomain staticDomain, NativeList<PropagateStackHelper> propagateStack)
        {
            this.rules = rules;
            data.staticDomain = staticDomain;
            this.propagateStack = propagateStack;
        }

        public void Execute()
        {
            Initialize();
            rules.ApplyInitialConditions(ref data, ref propagateStack);

            while (data.staticDomain.open.Count > 0)
            {
                Observe(data.staticDomain.open.Pop(), ref data, ref propagateStack);
                if(data.staticDomain.contradiction.Value.x >= 0) return;//Abort
            }
        }

        private void Initialize()
        {
            data.staticDomain.contradiction.Value = -1;
                
            for (var i = 0; i < data.staticDomain.size.x; i++)
            for (var j = 0; j < data.staticDomain.size.y; j++)
            for (var k = 0; k < data.staticDomain.size.z; k++)
            {
                var coordinate = new int3(i, j, k);

                var tileList = data.staticDomain.possibleTiles[coordinate];
                tileList.Clear();

                for (var tileIndex = 0; tileIndex < data.staticDomain.tileCount; tileIndex++)
                {
                    tileList.Add(new TileKey(){index = tileIndex});
                }

                data.staticDomain.possibleTiles[coordinate] = tileList;//Reassign, is a struct not a managed object
                data.staticDomain.open.Push(coordinate, 1);
            }
        }
        
        
            
        private static void Observe(int3 coordinates, ref Data data, ref NativeList<PropagateStackHelper> propagateStack)
        {
            var possibles = data.staticDomain.possibleTiles[coordinates];
                
            if(possibles.Length <= 1) return;

            var collapsed = Collapse(possibles, ref data);
            
            possibles.Clear();
            possibles.Add(collapsed);
    
            data.staticDomain.possibleTiles[coordinates] = possibles;
            
            propagateStack.Add(new PropagateStackHelper(){coordinates = coordinates, omitIndex = -1});
            Propagate(ref data, ref propagateStack);
        }

        private static void Propagate(ref Data data, ref NativeList<PropagateStackHelper> propagateStack)
        {
            while (!propagateStack.IsEmpty)
            {
                var entry = propagateStack[propagateStack.Length - 1];
                propagateStack.RemoveAtSwapBack(propagateStack.Length - 1);
                Propagate(entry.coordinates, ref data, ref propagateStack, entry.omitIndex);
                if(data.staticDomain.contradiction.Value.x >= 0) return;//Abort
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

            var value = data.staticDomain.rng.NextFloat(totalWeight);
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

        private static void Propagate(int3 coordinates, ref Data data, ref NativeList<PropagateStackHelper> propagateStack, int omitIndex = -1)
        {
            for (int i = 0; i < NeighbourDirection.Length; i++)
            {
                if(i == omitIndex) continue;
                    
                var neighbourCoordinates = coordinates + NeighbourDirection[i];
    
                if (neighbourCoordinates.x < 0 || neighbourCoordinates.x >= data.staticDomain.size.x ||
                    neighbourCoordinates.y < 0 || neighbourCoordinates.y >= data.staticDomain.size.y ||
                    neighbourCoordinates.z < 0 || neighbourCoordinates.z >= data.staticDomain.size.z)
                    continue;
    
                FilterPossibles(i, coordinates, neighbourCoordinates, ref data, ref propagateStack);
                
                if(data.staticDomain.contradiction.Value.x >= 0) return;//Abort
            }
            
            void FilterPossibles(int directionIndex, int3 coordinates, int3 neighbourCoordinates, ref Data data, ref NativeList<PropagateStackHelper> propagateStack)
            {
                var possibles = data.staticDomain.possibleTiles[coordinates];
                
                if(possibles.Length == 0) return;
                
                var neighbourPossibles = data.staticDomain.possibleTiles[neighbourCoordinates];
        
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
                            data.staticDomain.contradiction.Value = neighbourCoordinates;//Abort
                            return;
                        }
                    }
                }
        
                if (changePerformed)
                {
                    data.staticDomain.possibleTiles[neighbourCoordinates] = neighbourPossibles;
    
                    var entropy = 0f;
                    for (int i = 0; i < neighbourPossibles.Length; i++)
                    {
                        //The greater the weight, lesser the entropy
                        entropy += 1f / data.staticDomain.tileWeight[neighbourPossibles[i].index];
                    }
                    
                    data.staticDomain.open.Push(neighbourCoordinates, entropy);
                    propagateStack.Add(new PropagateStackHelper(){coordinates = neighbourCoordinates, omitIndex = ReciprocalDirection[directionIndex]});
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