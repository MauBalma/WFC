using System;
using Balma.WFC;
using Unity.Mathematics;
using UnityEngine;

public class MultiTile : MonoBehaviour
{
    [Serializable]
    public struct TileEntry
    {
        public int3 coordinates;
        public Tile2 tile;
    }
    
    public int3 size = 1;
    public TileEntry[] tiles;

    private void OnDrawGizmos()
    {
        Gizmos.matrix = Matrix4x4.TRS(transform.position, Quaternion.identity, Vector3.one);
        Gizmos.DrawWireCube((float3)(size-1) * 0.5f, (float3)size*.99f);
    }
}
