using System;
using UnityEngine;

public class PieceType : MonoBehaviour
{
    public TileType type0;
    public TileType type1;
    public TileType type2;
    public TileType type3;
    public TileType type4;
    public TileType type5;
    public TileType type6;
    public TileType type7;

    private void OnDrawGizmosSelected()
    {
        DrawTileType(type0, Vector3.zero);
        DrawTileType(type1, Vector3.right);
        DrawTileType(type2, Vector3.forward);
        DrawTileType(type3, Vector3.right + Vector3.forward);
        DrawTileType(type4, Vector3.up + Vector3.zero);
        DrawTileType(type5, Vector3.up + Vector3.right);
        DrawTileType(type6, Vector3.up + Vector3.forward);
        DrawTileType(type7, Vector3.up + Vector3.right + Vector3.forward);
    }

    private void DrawTileType(TileType tileType, Vector3 offset)
    {
        if (tileType != null)
        {
            Gizmos.color = tileType.color;
            Gizmos.DrawWireSphere(transform.position + offset - new Vector3(0.5f,0,0.5f), 0.2f);
        }
    }
}
