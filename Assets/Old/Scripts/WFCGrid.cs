using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Tilemaps;

public class WFCGrid : MonoBehaviour
{
    public int3 size = 10;
    public float tileSize = 1f;

    public TileType defaultTileType = null;
    public TileType bottomTileType = null;
    public TileType[] tileTypes = new TileType[0];
    public GameObject pieceSet;

    [Header("Debug")] 
    public bool draw = true;
    public Color mainGridColor = Color.white;
    public Color dualGridColor = Color.green;
    public bool debugTileColors = true;

    private TileType[,,] tiles;
    private Dictionary<PieceKey, PieceData> pieces;

    private PieceType[,,] spawned;

    private readonly struct PieceKey
    {
        private readonly TileType t0;
        private readonly TileType t1;
        private readonly TileType t2;
        private readonly TileType t3;
        private readonly TileType t4;
        private readonly TileType t5;
        private readonly TileType t6;
        private readonly TileType t7;

        public PieceKey(
            TileType t0, TileType t1, TileType t2, TileType t3,
            TileType t4, TileType t5, TileType t6, TileType t7
            )
        {
            this.t0 = t0;
            this.t1 = t1;
            this.t2 = t2;
            this.t3 = t3;
            this.t4 = t4;
            this.t5 = t5;
            this.t6 = t6;
            this.t7 = t7;
        }
        
        public bool Equals(PieceKey other)
        {
            return (t0 == null || other.t0 == null || Equals(t0, other.t0)) &&
                   (t1 == null || other.t1 == null || Equals(t1, other.t1)) &&
                   (t2 == null || other.t2 == null || Equals(t2, other.t2)) &&
                   (t3 == null || other.t3 == null || Equals(t3, other.t3)) &&
                   (t4 == null || other.t4 == null || Equals(t4, other.t4)) &&
                   (t5 == null || other.t5 == null || Equals(t5, other.t5)) &&
                   (t6 == null || other.t6 == null || Equals(t6, other.t6)) &&
                   (t7 == null || other.t7 == null || Equals(t7, other.t7));
        }

        public override bool Equals(object obj)
        {
            return obj is PieceKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return 0;//lol
        }
    }
    
    private struct PieceData
    {
        public PieceType pieceType;
        public Vector3 offset;
        public Quaternion rotation;
    }

    private void Awake()
    {
        LoadPieces();
        InitializeMatrix();
    }

    private void Start()
    {
        Generate();
    }

    private void Generate()
    {
        for (int i = 0; i < size.x - 1; i++)
        {
            for (int j = 0; j < size.y - 1; j++)
            {
                for (int k = 0; k < size.z - 1; k++)
                {
                    var key = new PieceKey(
                        tiles[i, j    , k], tiles[i + 1, j    , k], tiles[i, j    , k + 1], tiles[i + 1, j    , k + 1],
                        tiles[i, j + 1, k], tiles[i + 1, j + 1, k], tiles[i, j + 1, k + 1], tiles[i + 1, j + 1, k + 1]
                    );

                    if (!pieces.ContainsKey(key)) continue;

                    var data = pieces[key];

                    var piece = Instantiate(
                        original: data.pieceType,
                        position: new Vector3(i, j, k) * tileSize + data.offset + new Vector3(1, 1, 1),
                        rotation: data.rotation
                    );
                    spawned[i, j, k] = piece;
                }
            }
        }
    }

    private void InitializeMatrix()
    {
        tiles = new TileType[size.x, size.y,size.z];
        spawned = new PieceType[size.x-1, size.y-1,size.z-1];
        
        for (int i = 0; i < size.x; i++)
        for (int j = 0; j < size.y; j++)
        for (int k = 0; k < size.z; k++)
        {
            tiles[i, j, k] = j == 0 ? bottomTileType : defaultTileType;
        }
    }

    private void LoadPieces()
    {
        pieces = new Dictionary<PieceKey, PieceData>();
        
        var loadedPieces = pieceSet.GetComponentsInChildren<PieceType>();

        foreach (var loadedPiece in loadedPieces)
        {
            AddPiece(
                loadedPiece,
                loadedPiece.type0, loadedPiece.type1, loadedPiece.type2, loadedPiece.type3,
                loadedPiece.type4, loadedPiece.type5, loadedPiece.type6, loadedPiece.type7
                );
        }
    }

    private void AddPiece(PieceType loadedPiece, TileType t0, TileType t1, TileType t2, TileType t3, TileType t4, TileType t5, TileType t6, TileType t7)
    {
        var k0 = new PieceKey(t0, t1, t2, t3, t4, t5, t6, t7);

        if (pieces.ContainsKey(k0))
        {
            Debug.LogWarning("Trying to add a piece with an already defined key.");
            return;
        }

        var k1 = new PieceKey(t2, t0, t3, t1, t6, t4, t7, t5);
        var k2 = new PieceKey(t3, t2, t1, t0, t7, t6, t5, t4);
        var k3 = new PieceKey(t1, t3, t0, t2, t5, t7, t4, t6);

        pieces[k0] = new PieceData()
        {
            pieceType = loadedPiece,
            rotation = Quaternion.identity
        };
        pieces[k1] = new PieceData()
        {
            pieceType = loadedPiece,
            rotation = Quaternion.Euler(0, -90, 0)
        };
        pieces[k2] = new PieceData()
        {
            pieceType = loadedPiece,
            rotation = Quaternion.Euler(0, -180, 0)
        };
        pieces[k3] = new PieceData()
        {
            pieceType = loadedPiece,
            rotation = Quaternion.Euler(0, -270, 0)
        };
    }

    private void OnDrawGizmos()
    {
        if(!draw) return;

        //DrawMainGrid();
        //DrawDualGrid();
        
        if(debugTileColors) DrawTileDebugColors();
    }

    private void DrawTileDebugColors()
    {
        if(defaultTileType == null) return;
        if(tiles == null) return;
        
        for (int i = 0; i < size.x; i++)
        {
            for (int j = 0; j < size.y; j++)
            {
                for (int k = 0; k < size.z; k++)
                {
                    Gizmos.color = tiles[i,j,k].color;
                    var pos = new Vector3(i + 0.5f, j + 0.5f, k +0.5f) * tileSize;
                    Gizmos.DrawSphere(pos, tileSize * 0.1f);
                }
            }
        }
    }

    // private void DrawMainGrid()
    // {
    //     Gizmos.color = mainGridColor;
    //
    //     var sizeZ = Vector3.forward * tileSize * size.y;
    //     for (int i = 0; i <= size.x; i++)
    //     {
    //         var p0 = Vector3.right * tileSize * i;
    //         Gizmos.DrawLine(p0, p0 + sizeZ);
    //     }
    //
    //     var sizeX = Vector3.right * tileSize * size.x;
    //     for (int j = 0; j <= size.y; j++)
    //     {
    //         var p0 = Vector3.forward * tileSize * j;
    //         Gizmos.DrawLine(p0, p0 + sizeX);
    //     }
    // }
    //
    // private void DrawDualGrid()
    // {
    //     Gizmos.color = dualGridColor;
    //
    //     var sizeZ = Vector3.forward * tileSize * (size.y - 1);
    //     for (int i = 0; i < size.x; i++)
    //     {
    //         var p0 = Vector3.right * tileSize * (i + 0.5f) + Vector3.forward * 0.5f;
    //         Gizmos.DrawLine(p0, p0 + sizeZ);
    //     }
    //
    //     var sizeX = Vector3.right * tileSize * (size.x - 1);
    //     for (int j = 0; j < size.y; j++)
    //     {
    //         var p0 = Vector3.forward * tileSize * (j + 0.5f) + Vector3.right * 0.5f;
    //         Gizmos.DrawLine(p0, p0 + sizeX);
    //     }
    // }

    public void SetTile(int3 coordinates, TileType tileType)
    {
        Assert.IsNotNull(tileType);
        
        tiles[coordinates.x, coordinates.y, coordinates.z] = tileType;

        Regenerate();
    }

    private void Regenerate()
    {
        ClearPieces();
        Generate();
    }

    private void ClearPieces()
    {
        for (int i = 0; i < size.x - 1; i++)
        {
            for (int j = 0; j < size.y - 1; j++)
            {
                for (int k = 0; k < size.z - 1; k++)
                {
                    var pieceType = spawned[i, j, k];
                    if(pieceType)Destroy(pieceType.gameObject);
                    spawned[i, j, k] = null;
                }
            }
        }
    }
}
