using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.TerrainAPI;

[RequireComponent(typeof(WFCGrid))]
public class WFCGridController : MonoBehaviour
{
    public LayerMask raycastMask = ~0;

    public TileType[] palette = new TileType[0];
    
    private WFCGrid grid;
    private new Camera camera;
    private TileType currentTileType;

    public bool Hovering { get; private set; }
    public int2 HoveredTile { get; private set; }
    
    // Start is called before the first frame update
    void Start()
    {
        Assert.IsTrue(palette.Length > 0);
        
        grid = GetComponent<WFCGrid>();
        camera = Camera.main;

        currentTileType = palette[0];
    }

    // Update is called once per frame
    void Update()
    {
        CalculateCoordinates();
        Paint();
    }

    private void Paint()
    {
        for (int i = 1; i <= palette.Length; i++)
        {
            if (Input.GetKeyDown($"{i}"))
            {
                currentTileType = palette[i-1];
            }
        }

        if (Hovering && Input.GetMouseButton(0))
        {
            grid.SetTile(HoveredTile, currentTileType);
        }

    }

    private void CalculateCoordinates()
    {
        if (Physics.Raycast(camera.ScreenPointToRay(Input.mousePosition), out var hit, 1000, raycastMask))
        {
            var coord = (int3) math.floor(hit.point / grid.tileSize);

            Hovering = true;
            HoveredTile = coord.xz;
        }
        else
        {
            Hovering = false;
            HoveredTile = -1;
        }
    }
}
