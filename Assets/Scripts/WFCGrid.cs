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
    public int2 size = 10;
    public float tileSize = 1f;

    public TileType defaultTileType = null;
    public GameObject pieceSet;

    [Header("Debug")] 
    public bool draw = true;
    public Color mainGridColor = Color.white;
    public Color dualGridColor = Color.green;
    public bool debugTileColors = true;

    private TileType[,] tiles;
    private Dictionary<PieceKey, PieceData> pieces;

    private PieceType[,] spawned;

    private struct PieceKey
    {
        public TileType t0;
        public TileType t1;
        public TileType t2;
        public TileType t3;

        public PieceKey(TileType t0, TileType t1, TileType t2, TileType t3)
        {
            Assert.IsNotNull(t0);
            Assert.IsNotNull(t1);
            Assert.IsNotNull(t2);
            Assert.IsNotNull(t3);
            
            this.t0 = t0;
            this.t1 = t1;
            this.t2 = t2;
            this.t3 = t3;
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
                var key = new PieceKey(tiles[i, j], tiles[i + 1, j], tiles[i, j + 1], tiles[i + 1, j + 1]);
                
                if(!pieces.ContainsKey(key)) continue;
                
                var data = pieces[key];

                var piece = Instantiate(data.pieceType,
                    new Vector3(i, 0, j) * tileSize + data.offset + new Vector3(1,0,1), data.rotation);
                spawned[i, j] = piece;
            }
        }
    }

    private void InitializeMatrix()
    {
        tiles = new TileType[size.x, size.y];
        spawned = new PieceType[size.x-1, size.y-1];
        for (int i = 0; i < size.x; i++)
        {
            for (int j = 0; j < size.y; j++)
            {
                tiles[i, j] = defaultTileType;
            }
        }
    }

    private void LoadPieces()
    {
        pieces = new Dictionary<PieceKey, PieceData>();
        
        var loadedPieces = pieceSet.GetComponentsInChildren<PieceType>();

        foreach (var loadedPiece in loadedPieces)
        {
            var k0 = new PieceKey(loadedPiece.type0, loadedPiece.type1, loadedPiece.type2, loadedPiece.type3);

            if (pieces.ContainsKey(k0))
            {
                Debug.LogWarning("Trying to add a piece with an already defined key.");
                continue;
            }

            var k1 = new PieceKey(loadedPiece.type2, loadedPiece.type0, loadedPiece.type3, loadedPiece.type1);
            var k2 = new PieceKey(loadedPiece.type3, loadedPiece.type2, loadedPiece.type1, loadedPiece.type0);
            var k3 = new PieceKey(loadedPiece.type1, loadedPiece.type3, loadedPiece.type0, loadedPiece.type2);

            pieces[k0] = new PieceData()
            {
                pieceType = loadedPiece,
                offset = Vector3.zero * tileSize,
                rotation = Quaternion.identity
            };
            pieces[k1] = new PieceData()
            {
                pieceType = loadedPiece,
                //offset = Vector3.right * tileSize,
                rotation = Quaternion.Euler(0,-90,0)
            };
            pieces[k2] = new PieceData()
            {
                pieceType = loadedPiece,
                //offset = Vector3.forward * tileSize,
                rotation = Quaternion.Euler(0,-180,0)
            };
            pieces[k3] = new PieceData()
            {
                pieceType = loadedPiece,
                //offset = new Vector3(1,0,1) * tileSize,
                rotation = Quaternion.Euler(0,-270,0)
            };
        }
    }

    private void OnDrawGizmos()
    {
        if(!draw) return;

        DrawMainGrid();
        DrawDualGrid();
        
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
                Gizmos.color = tiles[i,j].color;
                var pos = new Vector3(i + 0.5f, 0, j + 0.5f) * tileSize;
                Gizmos.DrawSphere(pos, tileSize * 0.1f);
            }
        }
    }

    private void DrawMainGrid()
    {
        Gizmos.color = mainGridColor;

        var sizeZ = Vector3.forward * tileSize * size.y;
        for (int i = 0; i <= size.x; i++)
        {
            var p0 = Vector3.right * tileSize * i;
            Gizmos.DrawLine(p0, p0 + sizeZ);
        }

        var sizeX = Vector3.right * tileSize * size.x;
        for (int j = 0; j <= size.y; j++)
        {
            var p0 = Vector3.forward * tileSize * j;
            Gizmos.DrawLine(p0, p0 + sizeX);
        }
    }

    private void DrawDualGrid()
    {
        Gizmos.color = dualGridColor;

        var sizeZ = Vector3.forward * tileSize * (size.y - 1);
        for (int i = 0; i < size.x; i++)
        {
            var p0 = Vector3.right * tileSize * (i + 0.5f) + Vector3.forward * 0.5f;
            Gizmos.DrawLine(p0, p0 + sizeZ);
        }

        var sizeX = Vector3.right * tileSize * (size.x - 1);
        for (int j = 0; j < size.y; j++)
        {
            var p0 = Vector3.forward * tileSize * (j + 0.5f) + Vector3.right * 0.5f;
            Gizmos.DrawLine(p0, p0 + sizeX);
        }
    }

    public void SetTile(int2 coordinates, TileType tileType)
    {
        Assert.IsNotNull(tileType);
        
        tiles[coordinates.x, coordinates.y] = tileType;

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
                var pieceType = spawned[i, j];
                if(pieceType)Destroy(pieceType.gameObject);
                spawned[i, j] = null;
            }
        }
    }
}
