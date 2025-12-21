using System;
using System.Collections.Generic;
using UnityEngine;

public class HexGrid : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int gridWidth = 5;
    [SerializeField] private int gridHeight = 5;
    [Header("Hex Settings")]
    [SerializeField] private float hexSize = 1f;
    [SerializeField] private float hexHeight = 0.5f;
    [SerializeField] private bool isFlatTopped;
    [SerializeField] private Material hexMaterial;

    private List<GameObject> hexTiles = new List<GameObject>();
    
    private void OnEnable()
    {
        LayoutGrid();
    }

    public void OnValidate()
    {
        if (Application.isPlaying)
        {
            LayoutGrid();
        }
    }

    private void LayoutGrid()
    {
        for (int i = hexTiles.Count - 1; i >= 0; i--)
        {
            Destroy(hexTiles[i]);
        }
        hexTiles.Clear();
        
        for (int y = 0; y < gridWidth; y++)
        {
            for (int x = 0; x < gridHeight; x++)
            {
                GameObject tile = new GameObject($"Hex_{x}_{y}", typeof(HexRenderer));
                tile.transform.position = GetPositionForHexFromCoordinate(new Vector2Int(x, y));
                HexRenderer hexRenderer = tile.GetComponent<HexRenderer>();
                hexRenderer.innerSize = 0; // Approximation for inner size
                hexRenderer.outerSize = hexSize;
                hexRenderer.height = hexHeight;
                hexRenderer.isFlatTopped = isFlatTopped;
                hexRenderer.hexMaterial = hexMaterial;
                hexRenderer.DrawMesh();
                
                tile.transform.SetParent(transform, true);
                hexTiles.Add(tile);
            }
        }
    }

    private Vector3 GetPositionForHexFromCoordinate(Vector2Int coordinate)
    {
        int column = coordinate.x;
        int row = coordinate.y;
        float width;
        float height;
        float xPosition;
        float yPosition;
        bool shouldOffset;
        float horizontalDistance;
        float verticalDistance;
        float offset;
        float size = hexSize;

        if (isFlatTopped)
        {
            shouldOffset = (row % 2) == 0;
            width = Mathf.Sqrt(3) * size;
            height = 2f * size;
            horizontalDistance = width;
            verticalDistance = height * 0.75f;
            offset = shouldOffset ? horizontalDistance / 2f : 0f;
            xPosition = column * horizontalDistance + offset;
            yPosition = row * verticalDistance;
        }
        else
        {
            shouldOffset = (column % 2) == 0;
            width = 2f * size;
            height = Mathf.Sqrt(3) * size;
            horizontalDistance = width * 0.75f;
            verticalDistance = height;
            offset = shouldOffset ? verticalDistance / 2f : 0f;
            xPosition = column * horizontalDistance;
            yPosition = row * verticalDistance - offset;
        }

        return new Vector3(xPosition, 0, -yPosition);
    }
}
