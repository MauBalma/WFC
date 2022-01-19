using System;
using System.Collections;
using System.Collections.Generic;
using Balma.ADT;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

namespace Balma.WFC
{
    public struct TileData
    {
        public ConnectionData cd0;
        public ConnectionData cd1;
        public ConnectionData cd2;
        public ConnectionData cd3;

        public Quaternion prefabRotation;
        public GameObject prefab;

        public ConnectionData this[int index]
        {
            get => index switch
            {
                0 => cd0,
                1 => cd1,
                2 => cd2,
                3 => cd3,
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
                    default: throw new IndexOutOfRangeException();
                }
            }
        }
    }
    
    public struct ConnectionData
    {
        public enum Type
        {
            Normal, Forward, Reverse
        }
            
        public FixedString32Bytes key;
        public Type type;
    }

    public class WFCGrid : MonoBehaviour
    {
        public int2 size = 10;
        public float tileSize = 1f;

        public Tile[] tileSet;

        private List<TileData> generatedTileSet = new List<TileData>();

        private List<GameObject> instanced = new List<GameObject>();

        public static int2[] NeighbourDirection = new int2[]
        {
            new int2(1,  0),
            new int2(0,  1),
            new int2(-1, 0),
            new int2(0, -1),
        };

        private void Start()
        {
            GenerateRotations();
        }

        private void Clear()
        {
            foreach (var go in instanced)
            {
                Destroy(go);
            }
            
            instanced.Clear();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                Clear();
                Print(Generate());
            }
        }

        private void GenerateRotations()
        {
            var x = -3;
            
            foreach (var tile in tileSet)
            {
                var z = 0;
                tile.transform.position = new Vector3(--x, 0, z++);
                generatedTileSet.Add(tile.ToTileData());

                var tileB = Instantiate(tile);
                tileB.transform.position = new Vector3(x, 0, z++);
                tileB.connections = new[] {tile.connections[1], tile.connections[2], tile.connections[3], tile.connections[0]};
                tileB.transform.rotation = Quaternion.Euler(0,90,0);
                generatedTileSet.Add(tileB.ToTileData());

                var tileC = Instantiate(tile);
                tileC.transform.position = new Vector3(x, 0, z++);
                tileC.connections = new[] {tile.connections[2], tile.connections[3], tile.connections[0], tile.connections[1]};
                tileC.transform.rotation = Quaternion.Euler(0,180,0);
                generatedTileSet.Add(tileC.ToTileData());

                var tileD = Instantiate(tile);
                tileD.transform.position = new Vector3(x, 0, z++);
                tileD.connections = new[] {tile.connections[3], tile.connections[0], tile.connections[1], tile.connections[2]};
                tileD.transform.rotation = Quaternion.Euler(0,270,0);
                generatedTileSet.Add(tileD.ToTileData());
            }
        }

        private void Print(State state)
        {
            for (int i = 0; i < size.x; i++)
            {
                for (int j = 0; j < size.y; j++)
                {
                    var tile = state.tiles[i, j];
                    instanced.Add(Instantiate(tile.prefab, new Vector3(i, 0, j) * tileSize, tile.prefabRotation).gameObject);
                }
            }
        }

        public class State
        {
            public TileData[,] tiles;
            public int emptyTiles;
            
            public bool IsComplete()
            {
                return emptyTiles == 0;
            }

            public bool CanHave(TileData tile, int2 coordinates)
            {
                var sizeX = tiles.GetLength(0);
                var sizeY = tiles.GetLength(1);
                
                for (int i = 0; i < 4; i++)
                {
                    var otherCoordinates = coordinates + NeighbourDirection[i];

                    if(otherCoordinates.x < 0 || otherCoordinates.x >= sizeX ||
                       otherCoordinates.y < 0 || otherCoordinates.y >= sizeY)
                        continue;

                    var neighbour = tiles[otherCoordinates.x, otherCoordinates.y];
                    if(neighbour.prefab == default) continue;

                    var tileConnection = tile[i % 4];
                    var neighbourConnection = neighbour[(i + 2) % 4];
                    
                    if (tileConnection.key != neighbourConnection.key) 
                        return false;
                    
                    if (tileConnection.type != ConnectionData.Type.Normal 
                        && tileConnection.type == neighbourConnection.type) 
                        return false;
                }
                
                return true;
            }

            public State CopyWith(TileData tile, int2 coordinates)
            {
                var sizeX = tiles.GetLength(0);
                var sizeY = tiles.GetLength(1);
                
                var other = new State()
                {
                    tiles = new TileData[sizeX, sizeY],
                    emptyTiles = emptyTiles - 1,
                };

                for (int i = 0; i < sizeX; i++)
                for (int j = 0; j < sizeY; j++)
                {
                    var tileData = tiles[i, j];
                    if (tileData.prefab == default) continue;
                    other.tiles[i, j] = tileData;
                }

                other.tiles[coordinates.x, coordinates.y] = tile;

                return other;
            }
        }

        public State Generate()
        {
            var open = new DecreseableMinHeapManaged<State>();

            open.Push(new State()
            {
                tiles = new TileData[size.x, size.y],
                emptyTiles = size.x * size.y,
            }, size.x * size.y);

            while (open.Count > 0)
            {
                var current = open.Pop();
                
                if(current.IsComplete()) return current;

                var coordinates = new int2(-1);
                for (int i = 0; i < size.x && coordinates.x == -1; i++)
                for (int j = 0; j < size.y && coordinates.x == -1; j++)
                    if (current.tiles[i, j].prefab == default)
                        coordinates = new int2(i, j);

                Assert.IsTrue(math.all(coordinates != -1));

                Shuffle(generatedTileSet);
                
                foreach (var tile in generatedTileSet)
                {
                    if (current.CanHave(tile, coordinates))
                        open.Push(current.CopyWith(tile, coordinates), current.emptyTiles);
                }
            }

            return null;
        }
        
        public static void Shuffle<T> (List<T> array)
        {
            int n = array.Count;
            while (n > 1) 
            {
                int k = Random.Range(0, n--);
                T temp = array[n];
                array[n] = array[k];
                array[k] = temp;
            }
        }

        private void OnDrawGizmos()
        {
            var offset = tileSize * 0.5f * new Vector3(-1, 0, -1);
            
            Gizmos.matrix = Matrix4x4.TRS(offset, Quaternion.identity, Vector3.one * tileSize);

            for (int i = 0; i <= size.x; i++)
            {
                Gizmos.DrawLine(new Vector3(i, 0, 0), new Vector3(i, 0, size.y));
            }
            
            for (int j = 0; j <= size.y; j++)
            {
                Gizmos.DrawLine(new Vector3(0, 0, j), new Vector3(size.x, 0, j));
            }
        }
    }
}