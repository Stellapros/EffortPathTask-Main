using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages the grid layout for the game, handling floor and wall positions.
/// </summary>
public class GridManager : MonoBehaviour
{
    [SerializeField] private int gridWidth = 16;
    [SerializeField] private int gridHeight = 8;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private bool centerCells = true;
    [SerializeField] private TextAsset gridLayoutFile; // Grid layout file

    private bool[,] floorGrid; // True for floor, false for wall
    private List<Vector2Int> availableFloorPositions;

    private void Awake()
    {
        InitializeGrid();
    }

    /// <summary>
    /// Initializes the grid based on the provided layout file or creates a default all-floor layout.
    /// </summary>
    private void InitializeGrid()
    {
        floorGrid = new bool[gridWidth, gridHeight];
        availableFloorPositions = new List<Vector2Int>();

        if (gridLayoutFile != null)
        {
            string[] rows = gridLayoutFile.text.Split('\n');
            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    if (x < rows[y].Length && rows[y][x] == '.')
                    {
                        floorGrid[x, y] = true;
                        availableFloorPositions.Add(new Vector2Int(x, y));
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("Grid layout file not assigned. All positions will be considered as floor.");
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    floorGrid[x, y] = true;
                    availableFloorPositions.Add(new Vector2Int(x, y));
                }
            }
        }
    }

    /// <summary>
    /// Returns a random available floor position and removes it from the list of available positions.
    /// </summary>
    /// <returns>A Vector2 representing the world position of the selected floor tile.</returns>
    public Vector2 GetRandomAvailablePosition()
    {
        if (availableFloorPositions.Count == 0)
        {
            Debug.LogWarning("No available floor positions left!");
            return Vector2.zero;
        }

        int index = Random.Range(0, availableFloorPositions.Count);
        Vector2Int gridPos = availableFloorPositions[index];
        availableFloorPositions.RemoveAt(index);

        return GridToWorldPosition(gridPos);
    }

    /// <summary>
    /// Releases a position back into the pool of available positions if it's a valid floor tile.
    /// </summary>
    /// <param name="worldPosition">The world position to release.</param>
    public void ReleasePosition(Vector2 worldPosition)
    {
        Vector2Int gridPos = WorldToGridPosition(worldPosition);
        if (IsValidFloorPosition(gridPos) && !availableFloorPositions.Contains(gridPos))
        {
            availableFloorPositions.Add(gridPos);
        }
    }

    /// <summary>
    /// Checks if a given grid position is a valid floor tile.
    /// </summary>
    /// <param name="gridPos">The grid position to check.</param>
    /// <returns>True if the position is a valid floor tile, false otherwise.</returns>
    private bool IsValidFloorPosition(Vector2Int gridPos)
    {
        return gridPos.x >= 0 && gridPos.x < gridWidth && 
               gridPos.y >= 0 && gridPos.y < gridHeight && 
               floorGrid[gridPos.x, gridPos.y];
    }

    /// <summary>
    /// Converts a grid position to a world position.
    /// </summary>
    /// <param name="gridPos">The grid position to convert.</param>
    /// <returns>The corresponding world position.</returns>
    private Vector2 GridToWorldPosition(Vector2Int gridPos)
    {
        Vector2 worldPos = new Vector2(gridPos.x * cellSize, gridPos.y * cellSize);
        if (centerCells)
        {
            worldPos += new Vector2(cellSize * 0.5f, cellSize * 0.5f);
        }
        Vector2 gridCenter = new Vector2(gridWidth * 0.5f, gridHeight * 0.5f) * cellSize;
        return worldPos - gridCenter;
    }

    /// <summary>
    /// Converts a world position to a grid position.
    /// </summary>
    /// <param name="worldPosition">The world position to convert.</param>
    /// <returns>The corresponding grid position.</returns>
    private Vector2Int WorldToGridPosition(Vector2 worldPosition)
    {
        Vector2 gridCenter = new Vector2(gridWidth * 0.5f, gridHeight * 0.5f) * cellSize;
        Vector2 offsetPosition = worldPosition + gridCenter;
        if (centerCells)
        {
            offsetPosition -= new Vector2(cellSize * 0.5f, cellSize * 0.5f);
        }
        return new Vector2Int(
            Mathf.FloorToInt(offsetPosition.x / cellSize),
            Mathf.FloorToInt(offsetPosition.y / cellSize)
        );
    }

    /// <summary>
    /// Draws gizmos in the Unity editor to visualize the grid.
    /// Green represents floor tiles, red represents wall tiles.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (floorGrid == null) return;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector2 cellPosition = GridToWorldPosition(new Vector2Int(x, y));
                Gizmos.color = floorGrid[x, y] ? Color.green : Color.red;
                Gizmos.DrawWireCube(cellPosition, Vector2.one * cellSize);
            }
        }
    }
}