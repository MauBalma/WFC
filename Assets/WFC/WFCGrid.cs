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
        public ConnectionData cd4;
        public ConnectionData cd5;
        public ConnectionData cd6;
        public ConnectionData cd7;

        public Quaternion prefabRotation;
        public GameObject prefab;

        //TODO access memory directly avoiding branching (like float3)
        public ConnectionData this[int index]
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
    
    public struct ConnectionData
    {
        public TerrainType key;
        
        public static bool operator ==(ConnectionData lhs, ConnectionData rhs)
        {
            return lhs.key == rhs.key;
        }

        public static bool operator !=(ConnectionData lhs, ConnectionData rhs)
        {
            return !(lhs == rhs);
        }
    }

    public class WFCGrid : MonoBehaviour
    {
        public int3 size = 5;
        public float tileSize = 1f;

        public Tile[] tileSet;

        private List<TileData> generatedTileSet = new List<TileData>();

        private List<GameObject> instanced = new List<GameObject>();

        public static int3[] NeighbourDirection = new int3[]
        {
            new int3(1, 0,  0),
            new int3(0, 0,  1),
            new int3(-1, 0, 0),
            new int3(0, 0, -1),
        };

        private void Start()
        {
            GenerateRotations();
            Print(Generate());
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

                if(!tile.rotable) continue;
                
                var tileB = Instantiate(tile);
                tileB.transform.position = new Vector3(x, 0, z++);
                tileB.connections = new[] 
                {
                    tile.connections[1], tile.connections[2], tile.connections[3], tile.connections[0],
                    tile.connections[4+1], tile.connections[4+2], tile.connections[4+3], tile.connections[4+0],
                };
                tileB.transform.rotation = Quaternion.Euler(0,90,0);
                generatedTileSet.Add(tileB.ToTileData());

                var tileC = Instantiate(tile);
                tileC.transform.position = new Vector3(x, 0, z++);
                tileC.connections = new[] 
                {
                    tile.connections[2], tile.connections[3], tile.connections[0], tile.connections[1],
                    tile.connections[4+2], tile.connections[4+3], tile.connections[4+0], tile.connections[4+1],
                };
                tileC.transform.rotation = Quaternion.Euler(0,180,0);
                generatedTileSet.Add(tileC.ToTileData());

                var tileD = Instantiate(tile);
                tileD.transform.position = new Vector3(x, 0, z++);
                tileD.connections = new[] 
                {
                    tile.connections[3], tile.connections[0], tile.connections[1], tile.connections[2],
                    tile.connections[4+3], tile.connections[4+0], tile.connections[4+1], tile.connections[4+2],
                };
                tileD.transform.rotation = Quaternion.Euler(0,270,0);
                generatedTileSet.Add(tileD.ToTileData());
            }
        }

        private void Print(State state)
        {
            if (state == null)
            {
                Debug.LogWarning("Failed state print.");
                return;
            }

            for (int i = 0; i < size.x; i++)
            for (int j = 0; j < size.y; j++)
            for (int k = 0; k < size.z; k++)
            {
                var tile = state.tiles[i, j, k];
                instanced.Add(Instantiate(tile.prefab, new Vector3(i, j, k) * tileSize, tile.prefabRotation).gameObject);
            }
        }

        public class State
        {
            public TileData[,,] tiles;
            public int emptyTiles;
            
            public bool IsComplete()
            {
                return emptyTiles == 0;
            }

            public bool CanHave(TileData tile, int3 coordinates)
            {
                var sizeX = tiles.GetLength(0);
                var sizeY = tiles.GetLength(1);
                var sizeZ = tiles.GetLength(2);
                
                for (int i = 0; i < 4; i++)
                {
                    var otherCoordinates = coordinates + NeighbourDirection[i];

                    if(otherCoordinates.x < 0 || otherCoordinates.x >= sizeX ||
                       otherCoordinates.z < 0 || otherCoordinates.z >= sizeZ)
                        continue;

                    var neighbour = tiles[otherCoordinates.x, otherCoordinates.y, otherCoordinates.z];
                    if(neighbour.prefab == default) continue;
                    
                    //TODO per member comparation to allow special case NONE to match any tile
                    //TODO remove i == x and use only two lines 1 + y
                    if(i == 0 && (tile[3], tile[0]) != (neighbour[2], neighbour[1])) return false;
                    if(i == 1 && (tile[0], tile[1]) != (neighbour[3], neighbour[2])) return false;
                    if(i == 2 && (tile[1], tile[2]) != (neighbour[0], neighbour[3])) return false;
                    if(i == 3 && (tile[2], tile[3]) != (neighbour[1], neighbour[0])) return false;
                    
                    if(i == 0 && (tile[3+4], tile[0+4]) != (neighbour[2+4], neighbour[1+4])) return false;
                    if(i == 1 && (tile[0+4], tile[1+4]) != (neighbour[3+4], neighbour[2+4])) return false;
                    if(i == 2 && (tile[1+4], tile[2+4]) != (neighbour[0+4], neighbour[3+4])) return false;
                    if(i == 3 && (tile[2+4], tile[3+4]) != (neighbour[1+4], neighbour[0+4])) return false;
                }

                bool CheckUpper()
                {
                    var upperCoordinates = coordinates + new int3(0, 1, 0);

                    if (upperCoordinates.y < 0 || upperCoordinates.y >= sizeY) return false;

                    var neighbour = tiles[upperCoordinates.x, upperCoordinates.y, upperCoordinates.z];
                    if (neighbour.prefab == default) return false;

                    if ((neighbour[0], neighbour[1], neighbour[2], neighbour[3]) !=
                        (tile[4 + 0], tile[4 + 1], tile[4 + 2], tile[4 + 3])) return true;

                    return false;
                }

                if (CheckUpper()) return false;
                
                bool CheckBottom()
                {
                    var bottom = coordinates + new int3(0,-1,0);

                    if (bottom.y < 0 || bottom.y >= sizeY) return false;
                    
                    var neighbour = tiles[bottom.x, bottom.y, bottom.z];
                    if (neighbour.prefab == default) return false;

                    if ((tile[0], tile[1], tile[2], tile[3]) !=
                        (neighbour[4 + 0], neighbour[4 + 1], neighbour[4 + 2], neighbour[4 + 3])) return true;

                    return false;
                }
                
                if (CheckBottom()) return false;

                if (!CheckTerrainRules(this, tile, coordinates)) return false;

                return true;
            }

            //TODO move to scriptable terrain settings?
            private bool CheckTerrainRules(State state, TileData tile, int3 coordinates)
            {
                if (coordinates.y == 0)
                    for (int i = 0; i < 4; i++)
                        if (tile[i].key == TerrainType.Air) return false;

                if (coordinates.y > 0 && coordinates.x == 0)
                    if (tile[2].key != TerrainType.Air || tile[3].key != TerrainType.Air)
                        return false;
                
                if (coordinates.y > 0 && coordinates.x == state.tiles.GetLength(0) - 1)
                    if (tile[0].key != TerrainType.Air || tile[1].key != TerrainType.Air)
                        return false;
                
                if (coordinates.y > 0 && coordinates.z == 0)
                    if (tile[0].key != TerrainType.Air || tile[3].key != TerrainType.Air)
                        return false;
                
                if (coordinates.y > 0 && coordinates.z == state.tiles.GetLength(2) - 1)
                    if (tile[1].key != TerrainType.Air || tile[2].key != TerrainType.Air)
                        return false;

                return true;
            }

            public State CopyWith(TileData tile, int3 coordinates)
            {
                var sizeX = tiles.GetLength(0);
                var sizeY = tiles.GetLength(1);
                var sizeZ = tiles.GetLength(2);
                
                var other = new State()
                {
                    tiles = new TileData[sizeX, sizeY, sizeZ],
                    emptyTiles = emptyTiles - 1,
                };

                for (int i = 0; i < sizeX; i++)
                for (int j = 0; j < sizeY; j++)
                for (int k = 0; k < sizeZ; k++)
                {
                    var tileData = tiles[i, j, k];
                    if (tileData.prefab == default) continue;
                    other.tiles[i, j, k] = tileData;
                }

                other.tiles[coordinates.x, coordinates.y, coordinates.z] = tile;

                return other;
            }
        }

        public State Generate()
        {
            var open = new DecreseableMinHeapManaged<State>();

            open.Push(new State()
            {
                tiles = new TileData[size.x, size.y, size.z],
                emptyTiles = size.x * size.y * size.z,
            }, size.x * size.y * size.z);

            while (open.Count > 0)
            {
                var current = open.Pop();
                
                if(current.IsComplete()) return current;

                //TODO Add last visited coordinate and remove these iterations
                var coordinates = new int3(-1);

                for (int k = 0; k < size.z && coordinates.z == -1; k++)
                for (int j = 0; j < size.y && coordinates.y == -1; j++)
                for (int i = 0; i < size.x && coordinates.x == -1; i++)
                    if (current.tiles[i, j, k].prefab == default)
                        coordinates = new int3(i, j, k);

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
            var offset = tileSize * 0.5f * new Vector3(-1, -1, -1);
            
            Gizmos.matrix = Matrix4x4.TRS(offset, Quaternion.identity, Vector3.one * tileSize);

            for (int i = 0; i <= size.x; i++)
            {
                Gizmos.DrawLine(new Vector3(i, 0, 0), new Vector3(i, 0, size.z));
            }
            
            for (int j = 0; j <= size.z; j++)
            {
                Gizmos.DrawLine(new Vector3(0, 0, j), new Vector3(size.x, 0, j));
            }
        }
    }
}