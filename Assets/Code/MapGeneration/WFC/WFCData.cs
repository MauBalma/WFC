using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Balma.WFC
{
    public struct WFCData
    {
        public WFCDomain domain;
        public WFCStaticDomain staticDomain;

        private TileKey Collapse(UnsafeList<TileKey> possibles)
        {
            var totalWeight = 0f;

            for (var index = 0; index < possibles.Length; index++)
            {
                var possible = possibles[index];
                totalWeight += staticDomain.tileWeight[possible.index];
            }

            var value = domain.rng.NextFloat(totalWeight);
            var accWeight = 0f;

            for (var index = 0; index < possibles.Length; index++)
            {
                var possible = possibles[index];

                if (value >= accWeight && value < accWeight + staticDomain.tileWeight[possible.index])
                {
                    return possible;
                }

                accWeight += staticDomain.tileWeight[possible.index];
            }

            throw new Exception();
        }
            
        public void Observe(int3 coordinates)
        {
            var possibles = domain.possibleTiles[coordinates];
                
            if(possibles.Length <= 1) return;

            var collapsed = Collapse(possibles);
            
            possibles.Clear();
            possibles.Add(collapsed);
    
            domain.possibleTiles[coordinates] = possibles;
            
            domain.propagateStack.Add(new WFCPropagateStackHelper(){coordinates = coordinates, omitIndex = -1});
            Propagate();
        }

        public void Hint(int3 coordinates, TileKey tileKey)
        {
            var possibles = domain.possibleTiles[coordinates];
                
            if(possibles.Length == 0) return;
            if(!possibles.Contains(tileKey)) return;

            var collapsed = tileKey;
            
            possibles.Clear();
            possibles.Add(collapsed);
    
            domain.possibleTiles[coordinates] = possibles;
            domain.open.Push(coordinates, -1);//
    
            domain.propagateStack.Add(new WFCPropagateStackHelper(){coordinates = coordinates, omitIndex = -1});
            Propagate();
        }
            
        private void Propagate()
        {
            while (!domain.propagateStack.IsEmpty)
            {
                var entry = domain.propagateStack[domain.propagateStack.Length - 1];
                domain.propagateStack.RemoveAtSwapBack(domain.propagateStack.Length - 1);
                Propagate(entry.coordinates, entry.omitIndex);
                if(domain.contradiction.Value) return;//Abort
            }
        }
            
        private void Propagate(int3 coordinates, int omitIndex = -1)
        {
            for (int i = 0; i < WFC.NeighbourDirection.Length; i++)
            {
                if(i == omitIndex) continue;
                        
                var neighbourCoordinates = coordinates + WFC.NeighbourDirection[i];
        
                if (neighbourCoordinates.x < 0 || neighbourCoordinates.x >= staticDomain.size.x ||
                    neighbourCoordinates.y < 0 || neighbourCoordinates.y >= staticDomain.size.y ||
                    neighbourCoordinates.z < 0 || neighbourCoordinates.z >= staticDomain.size.z)
                    continue;
        
                FilterPossibles(i, coordinates, neighbourCoordinates, ref this);
                    
                if(domain.contradiction.Value) return;//Abort
            }
                
            void FilterPossibles(int directionIndex, int3 coordinates, int3 neighbourCoordinates, ref WFCData data)
            {
                var possibles = data.domain.possibleTiles[coordinates];
                    
                if(possibles.Length == 0) return;
                    
                var neighbourPossibles = data.domain.possibleTiles[neighbourCoordinates];
            
                var changePerformed = false;
            
                for (var np = neighbourPossibles.Length - 1; np >= 0; np--)
                {
                    var neighbourPossible = neighbourPossibles[np];
                    var neighbourTileData = data.staticDomain.tileDatas[neighbourPossible.index];
                    var neighbourConnectionData = neighbourTileData[WFC.ReciprocalDirection[directionIndex]];
            
                    var someMatch = false;
                            
                    for (var cp = possibles.Length - 1; cp >= 0; cp--)
                    {
                        var currentPossible = possibles[cp];
                        var currentTileData = data.staticDomain.tileDatas[currentPossible.index];
                        var currentConnectionData = currentTileData[directionIndex];
            
                        if (WFC.CheckConnectionsMatch(currentConnectionData,neighbourConnectionData))
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
                    data.domain.propagateStack.Add(new WFCPropagateStackHelper(){coordinates = neighbourCoordinates, omitIndex = WFC.ReciprocalDirection[directionIndex]});
                }
            }
        }
    }
}