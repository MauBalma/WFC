using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New tile type", menuName = "WFC/New tile type")]
public class TileType : ScriptableObject
{
    public new string name = "Undefined name";
    public Color color = Color.white;
}
