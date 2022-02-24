﻿using System;
using System.Collections.Generic;
using Balma.ADT;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using Random = Unity.Mathematics.Random;

namespace Balma.WFC
{
    public class WFCGrid : MonoBehaviour
    {
        public enum ResolutionMode
        {
            SingleTry,
            MultipleTries,
            //Backtracking,//Coming soon
            //BackJumping,//Coming soon
        }

        public Transform contradictionPointer;
        public ResolutionMode resolutionMode = ResolutionMode.MultipleTries;
        
        public int3 size = 5;
        public float tileSize = 1f;
        public uint seed = 69420;
        public bool forceSeed;
        public List<Tile> tileSet;
        public MultiTile[] multiTileSet;

        public Tile pureAirTile;
        public Tile grassTile;

        private WFCDomain staticDomain;
        private Dictionary<Tile, TileKey> tileKeys = new Dictionary<Tile, TileKey>();
        private List<GameObject> instanced = new List<GameObject>();
        
        private NativeList<int> prefabIndex;
        private NativeList<Quaternion> prefabRotation;

        private void Start()
        {
            prefabIndex = new NativeList<int>(Allocator.Persistent);
            prefabRotation = new NativeList<Quaternion>(Allocator.Persistent);

            staticDomain = new WFCDomain()
            {
                size = size,
                tileDatas = new NativeList<WFCTileData2>(Allocator.Persistent),
                tileWeight = new NativeList<float>(Allocator.Persistent),

                rng = new Random(seed),
                possibleTiles = new NativeHashMap<int3, UnsafeList<TileKey>>(1024, Allocator.Persistent),
                open = new DecreseableMinHeap<int3>(Allocator.Persistent),
                contradiction = new NativeReference<int3>(-1, Allocator.Persistent)
            };
            
            GenerateData();
            
            Generate();
        }
        
        private void GenerateData()
        {
            UnfoldMultiTiles();
            
            var connectionTypeIndices = new Dictionary<ConnectionType, int>();

            int GetConnectionTypeIndex(ConnectionType ct)
            {
                if (connectionTypeIndices.TryGetValue(ct, out var index))
                    return index;
                connectionTypeIndices.Add(ct, connectionTypeIndices.Count);
                return connectionTypeIndices.Count - 1;
            }

            for (var originalTileIndex = 0; originalTileIndex < tileSet.Count; originalTileIndex++)
            {
                var tile = tileSet[originalTileIndex];

                for (int i = 0; i < (tile.generateRotations ? 4 : 1); i++)
                {
                    if(i == 0) tileKeys.Add(tile, new TileKey(){index = prefabIndex.Length});
                    
                    prefabIndex.Add(originalTileIndex);
                    prefabRotation.Add(Quaternion.Euler(0, i * 90, 0));

                    var tileData2 = new WFCTileData2();

                    for (int j = 0; j < 4; j++)
                    {
                        var original = tile.connections[(j + i) % 4];
                        
                        Assert.AreEqual(original.rotation, Tile.ConnectionRotation.Indistinct);

                        tileData2[j] = new Tile.ConnectionDataProxy()
                        {
                            type = GetConnectionTypeIndex(original.type),
                            direction = original.direction,
                            rotation = original.rotation,
                        };
                    }
                    
                    for (int j = 0; j < 2; j++)
                    {
                        var original = tile.connections[4 + j];
                        
                        tileData2[4+j] = new Tile.ConnectionDataProxy()
                        {
                            type = GetConnectionTypeIndex(original.type),
                            direction = original.direction,
                            rotation = original.rotation == Tile.ConnectionRotation.Indistinct 
                                ? Tile.ConnectionRotation.Indistinct 
                                : (Tile.ConnectionRotation) (((int) (original.rotation) - 1 + i) % 4)
                            ,
                        };
                    }
                    
                    staticDomain.tileDatas.Add(tileData2);
                    var tileWeight = tile.weight * (tile.generateRotations ? 0.25f : 1);
                    staticDomain.tileWeight.Add(tileWeight);//Compensate rotable tiles having 4 more instances
                }
            }

            staticDomain.tileCount = prefabIndex.Length;
            
            for (var i = 0; i < staticDomain.size.x; i++)
            for (var j = 0; j < staticDomain.size.y; j++)
            for (var k = 0; k < staticDomain.size.z; k++)
            {
                var coordinate = new int3(i, j, k);
                var tileList = new UnsafeList<TileKey>(staticDomain.tileCount, Allocator.Persistent);
                staticDomain.possibleTiles[coordinate] = tileList;
            }
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
            void RandomizeAndRun()
            {
                var seed = staticDomain.rng.NextUInt();
                Debug.Log(seed);
                staticDomain.rng = new Random(forceSeed ? this.seed : seed); //Le hack

                var job = new WFCJob<Rules>(new Rules()
                {
                    airKey = tileKeys[pureAirTile],
                    grassKey = tileKeys[grassTile],
                }, staticDomain, new NativeList<PropagateStackHelper>(Allocator.TempJob));

                job.Run();
                job.propagateStack.Dispose();
            }

            switch (resolutionMode)
            {
                case ResolutionMode.SingleTry:
                    RandomizeAndRun();
                    break;
                case ResolutionMode.MultipleTries:
                    var tries = 0;
                    do
                    {
                        RandomizeAndRun();
                        if (tries++ > 1000) throw new Exception("To many tries.");
                    } while (staticDomain.contradiction.Value.x >= 0);
                    Debug.Log($"Tries: {tries}");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (staticDomain.contradiction.Value.x >= 0)
            {
                contradictionPointer.gameObject.SetActive(true);
                contradictionPointer.position = (float3)staticDomain.contradiction.Value * tileSize;
                
                Debug.LogWarning($"Contradiction at {staticDomain.contradiction.Value}");
            }
            else
            {
                contradictionPointer.gameObject.SetActive(false);
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
                var possibles = staticDomain.possibleTiles[coordinates];

                if (possibles.Length != 1)
                {
                    continue;
                }
                
                var tileKey = possibles[0];
                var tile = Instantiate(tileSet[prefabIndex[tileKey.index]], new Vector3(i, j, k) * tileSize, prefabRotation[tileKey.index]);
                instanced.Add(tile.gameObject);
            }
            
        }

        private void UnfoldMultiTiles()
        {
            foreach (var multiTile in multiTileSet)
            {
                var auxMatrix = new Tile[multiTile.size.x, multiTile.size.y, multiTile.size.z];

                bool TryGetNeighbour(int x, int y, int z, out Tile neighbour)
                {
                    neighbour = default;
                    if (x >= multiTile.size.x || y >= multiTile.size.y || z >= multiTile.size.z) return false;
                    neighbour = auxMatrix[x, y, z];
                    return neighbour != null;
                }

                foreach (var entry in multiTile.tiles)
                {
                    auxMatrix[entry.coordinates.x, entry.coordinates.y, entry.coordinates.z] = entry.tile;
                }
                
                for (var k = 0; k < multiTile.size.z; k++)
                for (var j = 0; j < multiTile.size.y; j++)
                for (var i = 0; i < multiTile.size.x; i++)
                {
                    var current = auxMatrix[i, j, k];
                    if(current == null) continue;

                    if (TryGetNeighbour(i+1, j, k, out var right))
                    {
                        var connectionType = ScriptableObject.CreateInstance<ConnectionType>();
                        current.connections[0] = new Tile.ConnectionData()
                        {
                            type = connectionType, 
                            direction = Tile.ConnectionDirection.Forward,
                            rotation = Tile.ConnectionRotation.Indistinct
                        };
                        right.connections[2] = new Tile.ConnectionData()
                        {
                            type = connectionType,
                            direction = Tile.ConnectionDirection.Backwards,
                            rotation = Tile.ConnectionRotation.Indistinct
                        };
                    }
                    
                    if (TryGetNeighbour(i, j, k+1, out var forward))
                    {
                        var connectionType = ScriptableObject.CreateInstance<ConnectionType>();
                        current.connections[1] = new Tile.ConnectionData()
                        {
                            type = connectionType, 
                            direction = Tile.ConnectionDirection.Forward,
                            rotation = Tile.ConnectionRotation.Indistinct
                        };
                        forward.connections[3] = new Tile.ConnectionData()
                        {
                            type = connectionType,
                            direction = Tile.ConnectionDirection.Backwards,
                            rotation = Tile.ConnectionRotation.Indistinct
                        };
                    }
                    
                    if (TryGetNeighbour(i, j+1, k, out var top))
                    {
                        var connectionType = ScriptableObject.CreateInstance<ConnectionType>();
                        current.connections[4] = new Tile.ConnectionData()
                        {
                            type = connectionType, 
                            direction = Tile.ConnectionDirection.Forward,
                            rotation = Tile.ConnectionRotation.R0
                        };
                        top.connections[5] = new Tile.ConnectionData()
                        {
                            type = connectionType,
                            direction = Tile.ConnectionDirection.Backwards,
                            rotation = Tile.ConnectionRotation.R0
                        };
                    }
                    
                    tileSet.Add(current);
                }
            }
        }

        private void OnDestroy()
        {
            using var possiblesList = staticDomain.possibleTiles.GetEnumerator();

            while (possiblesList.MoveNext())
            {
                possiblesList.Current.Value.Dispose();
            }
            
            prefabIndex.Dispose();
            prefabRotation.Dispose();
            staticDomain.tileDatas.Dispose();
            staticDomain.tileWeight.Dispose();
            staticDomain.possibleTiles.Dispose();
            staticDomain.open.Dispose();
            staticDomain.contradiction.Dispose();
        }

        private void OnDrawGizmosSelected()
        {
            if(!staticDomain.possibleTiles.IsCreated) return;
            
            for (var k = 0; k < size.z; k++)
            for (var j = 0; j < size.y; j++)
            for (var i = 0; i < size.x; i++)
            {
                var coordinate = new int3(i, j, k);
                var count = staticDomain.possibleTiles[coordinate].Length;
                var entropy = (float)count / staticDomain.tileCount;
                Gizmos.color = count == 0 ? Color.blue : count == 1 ? Color.green : Color.Lerp(Color.yellow, Color.red, entropy);
                Gizmos.DrawSphere((float3)coordinate * tileSize, 0.1f);
            }
        }
    }
    
    public struct WFCTileData2
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