using System;
using System.Collections.Generic;
using System.Collections;
using Balma.ADT;
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
        public int3 size = 5;
        public float tileSize = 1f;
        public uint seed = 69420;
        public Tile[] tileSet;

        private WFCDomain domain;
        private List<GameObject> instanced = new List<GameObject>();

        private void Start()
        {
            domain = new WFCDomain()
            {
                prefabIndex = new NativeList<int>(Allocator.Persistent),
                prefabRotation = new NativeList<Quaternion>(Allocator.Persistent),
                tileDatas = new NativeList<WFCTileData>(Allocator.Persistent),
                tileWeight = new NativeList<float>(Allocator.Persistent),

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
            //if (Input.GetKeyDown(KeyCode.R))
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
                    domain.tileWeight.Add(tile.weight * (tile.rotable ? 0.25f : 1));//Compensate rotable tiles having 4 more instances
                }
            }

            domain.tileCount = domain.prefabIndex.Length;
        }

        private void OnDestroy()
        {
            domain.prefabIndex.Dispose();
            domain.prefabRotation.Dispose();
            domain.tileDatas.Dispose();
            domain.tileWeight.Dispose();
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

