using System;
using System.Collections.Generic;
using System.Collections;
using Balma.ADT;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Balma.WFC
{
    public class WFCGrid : MonoBehaviour
    {
        private struct TileKey
        {
            public int index;
        }
        
        public int3 size = 5;
        public float tileSize = 1f;
        public uint seed = 69420;
        public Tile[] tileSet;
        
        private struct Domain
        {
            public int3 size;
            public Random rng;
            
            public int tileCount;
            public NativeList<int> prefabIndex;
            public NativeList<Quaternion> prefabRotation;
            public NativeList<WFCTileData> tileDatas;
            
            public DecreseableMinHeap<int3> open;
            public NativeHashMap<int3, UnsafeList<TileKey>> possibleTiles;
        }
        
        [BurstCompile]
        private struct WFCJob : IJob
        {
            public Domain domain;

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
                
                var collapsedIndex = domain.rng.NextInt(possibles.Length);
    
                var collapsed = possibles[collapsedIndex];
                possibles.Clear();
                possibles.Add(collapsed);
    
                domain.possibleTiles[coordinates] = possibles;
    
                Propagate(coordinates);
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

        private Domain domain;
        private List<GameObject> instanced = new List<GameObject>();
        
        public readonly static int3[] NeighbourDirection = new int3[]
        {
            new int3(1, 0,  0),
            new int3(0, 0,  1),
            new int3(-1, 0, 0),
            new int3(0, 0, -1),
            new int3(0, 1, 0),
            new int3(0, -1, 0),
        };

        public readonly static int4[] OwnFaceTypes = new int4[]
        {
            new int4(3, 0, 7, 4),
            new int4(0, 1, 4, 5),
            new int4(1, 2, 5, 6),
            new int4(2, 3, 6, 7),
            new int4(4, 5, 6, 7),
            new int4(0, 1, 2, 3),
        };

        public readonly static int4[] NeighbourFaceTypes = new int4[]
        {
            new int4(2, 1, 6, 5),
            new int4(3, 2, 7, 6),
            new int4(0, 3, 4, 7),
            new int4(1, 0, 5, 4),
            new int4(0, 1, 2, 3),
            new int4(4, 5, 6, 7),
        };
        
        public readonly static int[] ReciprocalDirection = new int[]
        {
            2,
            3,
            0,
            1,
            5,
            4,
        };

        private void Start()
        {
            domain = new Domain()
            {
                prefabIndex = new NativeList<int>(Allocator.Persistent),
                prefabRotation = new NativeList<Quaternion>(Allocator.Persistent),
                tileDatas = new NativeList<WFCTileData>(Allocator.Persistent),

                rng = new Random(seed),
                size = size,
                
                open = new DecreseableMinHeap<int3>(Allocator.Persistent),
                possibleTiles = new NativeHashMap<int3, UnsafeList<TileKey>>(1024, Allocator.Persistent),
            };
            
            GenerateData();
            
            Generate();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                Generate();
            }
        }

        private void Generate()
        {
            domain.rng = new Random(domain.rng.NextUInt());//Le hack
            
            new WFCJob()
            {
                domain = domain,
            }.Run();

            Print();
        }

        private void Print()
        {
            foreach (var go in instanced) Destroy(go);
            instanced.Clear();
            
            for (var i = 0; i < size.x; i++)
            for (var j = 0; j < size.y; j++)
            for (var k = 0; k < size.z; k++)
            {
                var coordinates = new int3(i, j, k);
                var possibles = domain.possibleTiles[coordinates];

                if (possibles.Length != 1) continue;
                
                var tileKey = possibles[0];
                var tile = Instantiate(tileSet[domain.prefabIndex[tileKey.index]], new Vector3(i, j, k) * tileSize, domain.prefabRotation[tileKey.index]);
                instanced.Add(tile.gameObject);
            }
        }

        private void GenerateData()
        {
            for (var originalTileIndex = 0; originalTileIndex < tileSet.Length; originalTileIndex++)
            {
                var tile = tileSet[originalTileIndex];

                for (int i = 0; i < (tile.rotable ? 4 : 1); i++)
                {
                    domain.prefabIndex.Add(originalTileIndex);
                    domain.prefabRotation.Add(Quaternion.Euler(0, i * 90, 0));
                    domain.tileDatas.Add(new WFCTileData()
                    {
                        [0] = tile.connections[i].key,     [1] = tile.connections[(1 + i) % 4].key,     [2] = tile.connections[(2 + i) % 4].key,     [3] = tile.connections[(3 + i) % 4].key,
                        [4] = tile.connections[i + 4].key, [5] = tile.connections[(1 + i) % 4 + 4].key, [6] = tile.connections[(2 + i) % 4 + 4].key, [7] = tile.connections[(3 + i) % 4 + 4].key,
                    });
                }
            }

            domain.tileCount = domain.prefabIndex.Length;
        }

        private void OnDestroy()
        {
            domain.prefabIndex.Dispose();
            domain.prefabRotation.Dispose();
            domain.tileDatas.Dispose();
            domain.possibleTiles.Dispose();
            domain.open.Dispose();
        }

        private void OnDrawGizmosSelected()
        {
            if(!domain.possibleTiles.IsCreated) return;
            
            for (var k = 0; k < size.z; k++)
            for (var j = 0; j < size.y; j++)
            for (var i = 0; i < size.x; i++)
            {
                var coordinate = new int3(i, j, k);
                var count = domain.possibleTiles[coordinate].Length;
                var entropy = (float)count / domain.tileCount;
                Gizmos.color = count == 0 ? Color.blue : count == 1 ? Color.green : Color.Lerp(Color.yellow, Color.red, entropy);
                Gizmos.DrawSphere((float3)coordinate * tileSize, 0.1f);
            }
        }
    }

    public struct WFCTileData
    {
        private TerrainType cd0;
        private TerrainType cd1;
        private TerrainType cd2;
        private TerrainType cd3;
        private TerrainType cd4;
        private TerrainType cd5;
        private TerrainType cd6;
        private TerrainType cd7;

        //TODO access memory directly avoiding branching (like float3)
        public TerrainType this[int index]
        {
            get => index switch
            {
                0 => cd0,
                1 => cd1,
                2 => cd2,
                3 => cd3,
                4 => cd4,
                5 => cd5,
                6 => cd6,
                7 => cd7,
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
                    case 6: cd6 = value; break;
                    case 7: cd7 = value; break;
                    default: throw new IndexOutOfRangeException();
                }
            }
        }
    }
}

