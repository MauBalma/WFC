using System;
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

        private Rules rules;
        private WFCStaticDomain staticDomain;
        private Dictionary<Tile, TileKey> tileKeys = new Dictionary<Tile, TileKey>();
        private List<GameObject> instanced = new List<GameObject>();
        
        private NativeList<int> prefabIndex;
        private NativeList<Quaternion> prefabRotation;

        private void Start()
        {
            prefabIndex = new NativeList<int>(Allocator.Persistent);
            prefabRotation = new NativeList<Quaternion>(Allocator.Persistent);

            staticDomain = new WFCStaticDomain()
            {
                size = size,
                tileDatas = new NativeList<WFCTileData>(Allocator.Persistent),
                tileWeight = new NativeList<float>(Allocator.Persistent),
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

                    var tileData = new WFCTileData();

                    for (int j = 0; j < 4; j++)
                    {
                        var original = tile.connections[(j + i) % 4];
                        
                        Assert.AreEqual(original.rotation, Tile.ConnectionRotation.Indistinct);

                        tileData[j] = new Tile.ConnectionDataProxy()
                        {
                            type = GetConnectionTypeIndex(original.type),
                            direction = original.direction,
                            rotation = original.rotation,
                        };
                    }
                    
                    for (int j = 0; j < 2; j++)
                    {
                        var original = tile.connections[4 + j];
                        
                        tileData[4+j] = new Tile.ConnectionDataProxy()
                        {
                            type = GetConnectionTypeIndex(original.type),
                            direction = original.direction,
                            rotation = original.rotation == Tile.ConnectionRotation.Indistinct 
                                ? Tile.ConnectionRotation.Indistinct 
                                : (Tile.ConnectionRotation) (((int) (original.rotation) - 1 + i) % 4)
                            ,
                        };
                    }
                    
                    staticDomain.tileDatas.Add(tileData);
                    var tileWeight = tile.weight * (tile.generateRotations ? 0.25f : 1);
                    staticDomain.tileWeight.Add(tileWeight);//Compensate rotable tiles having 4 more instances
                }
            }

            staticDomain.tileCount = prefabIndex.Length;

            rules = new Rules()
            {
                airKey = tileKeys[pureAirTile],
                grassKey = tileKeys[grassTile],
            };
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
            switch (resolutionMode)
            {
                case ResolutionMode.SingleTry:
                    RunSingleTry();
                    break;
                case ResolutionMode.MultipleTries:
                    RunMultipleTries();
                    break;
            }
        }

        private void RunSingleTry()
        {
            var domain = GetCleanDomain();

            var job = new WFCJob<Rules>(rules, ref staticDomain, ref domain);
            job.Run();

            Print(domain.possibleTiles);

            domain.Dispose();

            seed = domain.rng.NextUInt();
        }
        
        private void RunMultipleTries()
        {
            var domain = GetCleanDomain();
            var tries = 0;

            do
            {
                if (tries++ > 1000)
                {
                    Debug.LogWarning("Cannot resolve in 1000 iterations.");
                    domain.Dispose();
                    return;
                }
                
                var job = new WFCJob<Rules>(rules, ref staticDomain, ref domain);
                job.Run();
                seed = domain.rng.NextUInt();
            }
            while (domain.contradiction.Value);

            Print(domain.possibleTiles);
            domain.Dispose();
        }

        private WFCDomain GetCleanDomain()
        {
            var domain = new WFCDomain()
            {
                rng = new Random(seed),
                possibleTiles = new NativeHashMap<int3, UnsafeList<TileKey>>(1024, Allocator.TempJob),
                open = new DecreseableMinHeap<int3>(Allocator.TempJob),
                contradiction = new NativeReference<bool>(false, Allocator.TempJob),
                propagateStack = new NativeList<WFCPropagateStackHelper>(Allocator.TempJob),
            };
            
            for (var i = 0; i < staticDomain.size.x; i++)
            for (var j = 0; j < staticDomain.size.y; j++)
            for (var k = 0; k < staticDomain.size.z; k++)
            {
                var coordinate = new int3(i, j, k);
                var tileList = new UnsafeList<TileKey>(staticDomain.tileCount, Allocator.TempJob);
                domain.possibleTiles[coordinate] = tileList;
            }
            
            return domain;
        }

        private void Print(NativeHashMap<int3, UnsafeList<TileKey>> result)
        {
            foreach (var go in instanced) Destroy(go);
            instanced.Clear();
            
            for (var i = 0; i < size.x; i++)
            for (var j = 0; j < size.y; j++)
            for (var k = 0; k < size.z; k++)
            {
                var coordinates = new int3(i, j, k);
                var possibles = result[coordinates];

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
            prefabIndex.Dispose();
            prefabRotation.Dispose();
            staticDomain.Dispose();
        }
    }
}