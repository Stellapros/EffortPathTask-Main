using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    [SerializeField] private int gridWidth = 18;
    [SerializeField] private int gridHeight = 10;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private bool centerCells = true;

    private List<Vector2Int> availablePositions;

    private void Awake()
    {
        InitializeGrid();
    }

    private void InitializeGrid()
    {
        availablePositions = new List<Vector2Int>();
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                availablePositions.Add(new Vector2Int(x, y));
            }
        }
    }

    public Vector2 GetRandomAvailablePosition()
    {
        if (availablePositions.Count == 0)
        {
            Debug.LogWarning("No available positions left!");
            return Vector2.zero;
        }

        int index = Random.Range(0, availablePositions.Count);
        Vector2Int gridPos = availablePositions[index];
        availablePositions.RemoveAt(index);

        return GridToWorldPosition(gridPos);
    }

    public void ReleasePosition(Vector2 worldPosition)
    {
        Vector2Int gridPos = WorldToGridPosition(worldPosition);
        if (!availablePositions.Contains(gridPos))
        {
            availablePositions.Add(gridPos);
        }
    }

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

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Vector2 gridCenter = new Vector2(gridWidth * 0.5f, gridHeight * 0.5f) * cellSize;
        Vector2 cellOffset = centerCells ? new Vector2(cellSize * 0.5f, cellSize * 0.5f) : Vector2.zero;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector2 cellPosition = new Vector2(x * cellSize, y * cellSize) - gridCenter + cellOffset;
                Gizmos.DrawWireCube(cellPosition, Vector2.one * cellSize);
            }
        }
    }
}