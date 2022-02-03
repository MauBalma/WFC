using System;
using System.Collections.Generic;
using System.Collections;
using Balma.ADT;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Balma.WFC
{
    public class WFCGrid : MonoBehaviour
    {
        public struct TileKey
        {
            public int index;
        }
        
        public int3 size = 5;
        public float tileSize = 1f;
        public uint seed = 69420;
        public Tile[] tileSet;

        private int tileCount;
        private NativeList<int> prefabIndex;
        private NativeList<Quaternion> prefabRotation;
        private NativeList<WFCTileData> tileDatas;
        
        private NativeHashMap<int3, UnsafeList<TileKey>> possibleTiles;
        private DecreseableMinHeap<int3> open;
        
        private Random rng;
        
        private List<GameObject> instanced = new List<GameObject>();
        
        public static int3[] NeighbourDirection = new int3[]
        {
            new int3(1, 0,  0),
            new int3(0, 0,  1),
            new int3(-1, 0, 0),
            new int3(0, 0, -1),
            new int3(0, 1, 0),
            new int3(0, -1, 0),
        };

        public static int4[] OwnFaceTypes = new int4[]
        {
            new int4(3, 0, 7, 4),
            new int4(0, 1, 4, 5),
            new int4(1, 2, 5, 6),
            new int4(2, 3, 6, 7),
            new int4(4, 5, 6, 7),
            new int4(0, 1, 2, 3),
        };

        public static int4[] NeighbourFaceTypes = new int4[]
        {
            new int4(2, 1, 6, 5),
            new int4(3, 2, 7, 6),
            new int4(0, 3, 4, 7),
            new int4(1, 0, 5, 4),
            new int4(0, 1, 2, 3),
            new int4(4, 5, 6, 7),
        };
        
        public static int[] ReciprocalDirection = new int[]
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
            prefabIndex = new NativeList<int>(Allocator.Persistent);
            prefabRotation = new NativeList<Quaternion>(Allocator.Persistent);
            tileDatas = new NativeList<WFCTileData>(Allocator.Persistent);
            possibleTiles = new NativeHashMap<int3, UnsafeList<TileKey>>(1024, Allocator.Persistent);
            open = new DecreseableMinHeap<int3>(Allocator.Persistent);

            rng = new Random(seed);
            
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
            Clear();
            InitializePossibleTiles();

            while (open.Count > 0)
            {
                Observe(open.Pop());
            }
            
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
                var possibles = possibleTiles[coordinates];

                if (possibles.Length != 1) continue;
                
                var tileKey = possibles[0];
                var tile = Instantiate(tileSet[prefabIndex[tileKey.index]], new Vector3(i, j, k) * tileSize, prefabRotation[tileKey.index]);
                instanced.Add(tile.gameObject);
            }
        }

        private void Observe(int3 coordinates)
        {
            var possibles = possibleTiles[coordinates];
            
            if(possibles.Length == 0) return;
            
            var collapsedIndex = rng.NextInt(possibles.Length);

            var collapsed = possibles[collapsedIndex];
            possibles.Clear();
            possibles.Add(collapsed);

            possibleTiles[coordinates] = possibles;

            Propagate(coordinates);
        }

        private void Propagate(int3 coordinates, int omitIndex = -1)
        {
            for (int i = 0; i < NeighbourDirection.Length; i++)
            {
                if(i == omitIndex) continue;
                
                var neighbourCoordinates = coordinates + NeighbourDirection[i];

                if (neighbourCoordinates.x < 0 || neighbourCoordinates.x >= size.x ||
                    neighbourCoordinates.y < 0 || neighbourCoordinates.y >= size.y ||
                    neighbourCoordinates.z < 0 || neighbourCoordinates.z >= size.z)
                    continue;

                FilterPossibles(i, coordinates, neighbourCoordinates);
            }
        }

        private void FilterPossibles(int directionIndex, int3 coordinates, int3 neighbourCoordinates)
        {
            var possibles = possibleTiles[coordinates];
            var neighbourPossibles = possibleTiles[neighbourCoordinates];

            var ownFaceIndices = OwnFaceTypes[directionIndex];
            var neighbourFaceIndices = NeighbourFaceTypes[directionIndex];

            var changePerformed = false;

            for (var np = neighbourPossibles.Length - 1; np >= 0; np--)
            {
                var neighbourPossible = neighbourPossibles[np];
                var neighbourFaceTypes = tileDatas[neighbourPossible.index];
                
                var neighbourFace = (neighbourFaceTypes[neighbourFaceIndices[0]],
                                     neighbourFaceTypes[neighbourFaceIndices[1]],
                                     neighbourFaceTypes[neighbourFaceIndices[2]],
                                     neighbourFaceTypes[neighbourFaceIndices[3]]);

                var someMatch = false;
                
                for (var cp = possibles.Length - 1; cp >= 0; cp--)
                {
                    var currentPossible = possibles[cp];
                    var currentFaceTypes = tileDatas[currentPossible.index];
                    
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
                possibleTiles[neighbourCoordinates] = neighbourPossibles;
                open.Push(neighbourCoordinates, neighbourPossibles.Length);
                Propagate(neighbourCoordinates, ReciprocalDirection[directionIndex]);
            }
        }

        private void GenerateData()
        {
            for (var originalTileIndex = 0; originalTileIndex < tileSet.Length; originalTileIndex++)
            {
                var tile = tileSet[originalTileIndex];

                for (int i = 0; i < (tile.rotable ? 4 : 1); i++)
                {
                    prefabIndex.Add(originalTileIndex);
                    prefabRotation.Add(Quaternion.Euler(0, i * 90, 0));
                    tileDatas.Add(new WFCTileData()
                    {
                        [0] = tile.connections[i].key,     [1] = tile.connections[(1 + i) % 4].key,     [2] = tile.connections[(2 + i) % 4].key,     [3] = tile.connections[(3 + i) % 4].key,
                        [4] = tile.connections[i + 4].key, [5] = tile.connections[(1 + i) % 4 + 4].key, [6] = tile.connections[(2 + i) % 4 + 4].key, [7] = tile.connections[(3 + i) % 4 + 4].key,
                    });
                }
            }

            tileCount = prefabIndex.Length;
        }

        private void Clear()
        {
            possibleTiles.Clear();
            open.Clear();
        }

        private void InitializePossibleTiles()
        {
            for (var i = 0; i < size.x; i++)
            for (var j = 0; j < size.y; j++)
            for (var k = 0; k < size.z; k++)
            {
                var tileList = new UnsafeList<TileKey>(tileCount, Allocator.Persistent);
                for (var tileIndex = 0; tileIndex < tileCount; tileIndex++)
                {
                    tileList.Add(new TileKey(){index = tileIndex});
                }
                
                var coordinate = new int3(i, j, k);
                possibleTiles.Add(coordinate, tileList);
                open.Push(coordinate, tileCount);
            }
        }

        private void OnDestroy()
        {
            prefabIndex.Dispose();
            prefabRotation.Dispose();
            tileDatas.Dispose();
            possibleTiles.Dispose();
            open.Dispose();
        }

        private void OnDrawGizmosSelected()
        {
            if(!possibleTiles.IsCreated) return;
            
            for (var k = 0; k < size.z; k++)
            for (var j = 0; j < size.y; j++)
            for (var i = 0; i < size.x; i++)
            {
                var coordinate = new int3(i, j, k);
                var count = possibleTiles[coordinate].Length;
                var entropy = (float)count / tileCount;
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

