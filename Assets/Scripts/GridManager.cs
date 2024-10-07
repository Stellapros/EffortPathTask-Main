using UnityEngine;
using System.Collections.Generic;

public enum CellType { Empty, Floor, Wall }

/// <summary>
/// Manages the grid layout for the game, handling floor and wall positions.
/// </summary>
public class GridManager : MonoBehaviour
{
    [SerializeField] private int gridWidth = 18;
    [SerializeField] private int gridHeight = 10;
    [SerializeField] public float cellSize = 1f;
    [SerializeField] private bool centerCells = true;
    [SerializeField] private TextAsset gridLayoutFile;
    [SerializeField] private GameObject floorTilePrefab;
    [SerializeField] private GameObject wallTilePrefab;

    private CellType[,] grid;
    private List<Vector2Int> availableFloorPositions;
    private GameObject[,] gridObjects;
    private ObjectPool floorPool;
    private ObjectPool wallPool;
    private bool[,] occupiedPositions;

    public int GridWidth => gridWidth;
    public int GridHeight => gridHeight;

    private void Awake()
    {
        InitializeGrid();
        InitializeObjectPools();
    }

    private void InitializeGrid()
    {
        grid = new CellType[gridWidth, gridHeight];
        availableFloorPositions = new List<Vector2Int>();
        gridObjects = new GameObject[gridWidth, gridHeight];

        if (gridLayoutFile != null)
        {
            LoadGridFromFile();
        }
        else
        {
            CreateDefaultGrid();
        }
    }

    private void LoadGridFromFile()
    {
        string[] rows = gridLayoutFile.text.Split('\n');
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                if (x < rows[y].Length)
                {
                    grid[x, y] = rows[y][x] == '.' ? CellType.Floor : CellType.Wall;
                    if (grid[x, y] == CellType.Floor)
                    {
                        availableFloorPositions.Add(new Vector2Int(x, y));
                    }
                }
                else
                {
                    grid[x, y] = CellType.Empty;
                }
            }
        }
    }

    private void CreateDefaultGrid()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                grid[x, y] = CellType.Floor;
                availableFloorPositions.Add(new Vector2Int(x, y));
            }
        }
    }

    private void InitializeObjectPools()
    {
        floorPool = new ObjectPool(floorTilePrefab, 100, transform);
        wallPool = new ObjectPool(wallTilePrefab, 100, transform);
    }

    public void ShowGrid()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector3 position = GridToWorldPosition(new Vector2Int(x, y));
                GameObject tile = grid[x, y] == CellType.Floor ? floorPool.GetObject() : wallPool.GetObject();

                if (tile != null)
                {
                    tile.transform.position = position;
                    tile.SetActive(true);
                    gridObjects[x, y] = tile;
                }
            }
        }
    }

    public void HideGrid()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (gridObjects[x, y] != null)
                {
                    gridObjects[x, y].SetActive(false);
                    if (grid[x, y] == CellType.Floor)
                    {
                        floorPool.ReturnObject(gridObjects[x, y]);
                    }
                    else
                    {
                        wallPool.ReturnObject(gridObjects[x, y]);
                    }
                    gridObjects[x, y] = null;
                }
            }
        }
    }

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
    public Vector2Int GetGridPositionAtDistance(Vector2Int startGridPos, int distance)
    {
        Debug.Log($"GetGridPositionAtDistance called: Start {startGridPos}, Distance {distance}");
        List<Vector2Int> candidatePositions = new List<Vector2Int>();

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (grid[x, y] == CellType.Floor)
                {
                    Vector2Int gridPos = new Vector2Int(x, y);
                    int gridDistance = Mathf.Abs(gridPos.x - startGridPos.x) + Mathf.Abs(gridPos.y - startGridPos.y);
                    if (gridDistance == distance)
                    {
                        candidatePositions.Add(gridPos);
                    }
                }
            }
        }

        Debug.Log($"Candidate positions found: {candidatePositions.Count}");

        if (candidatePositions.Count == 0)
        {
            Debug.LogWarning("No positions found at the exact distance. Returning closest available position.");
            return GetClosestAvailableGridPosition(startGridPos, distance);
        }

        Vector2Int selectedPosition = candidatePositions[Random.Range(0, candidatePositions.Count)];
        Debug.Log($"Selected position: {selectedPosition}");
        return selectedPosition;
    }

    public Vector2 GetPositionAtDistance(Vector2 startPosition, float distance)
    {
        Vector2Int startGridPos = WorldToGridPosition(startPosition);
        List<Vector2Int> candidatePositions = new List<Vector2Int>();

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (grid[x, y] == CellType.Floor && !occupiedPositions[x, y])
                {
                    Vector2Int gridPos = new Vector2Int(x, y);
                    float gridDistance = Vector2Int.Distance(startGridPos, gridPos);
                    if (Mathf.Abs(gridDistance - distance) < 0.5f) // Allow some tolerance
                    {
                        candidatePositions.Add(gridPos);
                    }
                }
            }
        }
        if (candidatePositions.Count == 0)
        {
            Debug.LogWarning("No positions found at the exact distance. Returning closest available position.");
            return GetClosestAvailablePosition(startPosition, distance);
        }

        Vector2Int selectedGridPos = candidatePositions[Random.Range(0, candidatePositions.Count)];
        return GridToWorldPosition(selectedGridPos);
    }
    public Vector2 GetCellCenterWorldPosition(Vector2Int gridPos)
    {
        Vector2 worldPos = new Vector2(gridPos.x * cellSize, gridPos.y * cellSize);
        Vector2 cellCenter = worldPos + new Vector2(cellSize * 0.5f, cellSize * 0.5f);
        Vector2 gridCenter = new Vector2(gridWidth * 0.5f, gridHeight * 0.5f) * cellSize;
        return cellCenter - gridCenter;
    }

    private Vector2 GetClosestAvailablePosition(Vector2 startPosition, float targetDistance)
    {
        Vector2Int startGridPos = WorldToGridPosition(startPosition);
        Vector2Int closestPos = startGridPos;
        float closestDistanceDiff = float.MaxValue;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (grid[x, y] == CellType.Floor)
                {
                    Vector2Int gridPos = new Vector2Int(x, y);
                    float distance = Vector2.Distance(GridToWorldPosition(startGridPos), GridToWorldPosition(gridPos));
                    float distanceDiff = Mathf.Abs(distance - targetDistance);
                    if (distanceDiff < closestDistanceDiff)
                    {
                        closestDistanceDiff = distanceDiff;
                        closestPos = gridPos;
                    }
                }
            }
        }

        return GridToWorldPosition(closestPos);
    }

    public Vector2Int GetClosestAvailableGridPosition(Vector2Int startGridPos, int targetDistance)
    {
        Debug.Log($"GetClosestAvailableGridPosition called: Start {startGridPos}, Target Distance {targetDistance}");
        Vector2Int closestPos = startGridPos;
        int closestDistanceDiff = int.MaxValue;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (grid[x, y] == CellType.Floor)
                {
                    Vector2Int gridPos = new Vector2Int(x, y);
                    int distance = Mathf.Abs(gridPos.x - startGridPos.x) + Mathf.Abs(gridPos.y - startGridPos.y);
                    int distanceDiff = Mathf.Abs(distance - targetDistance);
                    if (distanceDiff < closestDistanceDiff)
                    {
                        closestDistanceDiff = distanceDiff;
                        closestPos = gridPos;
                    }
                }
            }
        }

        Debug.Log($"Closest available position: {closestPos}, Distance difference: {closestDistanceDiff}");
        return closestPos;
    }

    public void ReleasePosition(Vector2 worldPosition)
    {
        Vector2Int gridPos = WorldToGridPosition(worldPosition);
        if (IsValidFloorPosition(gridPos) && !availableFloorPositions.Contains(gridPos))
        {
            availableFloorPositions.Add(gridPos);
        }
    }

    public bool IsValidPosition(Vector2 worldPosition)
    {
        Vector2Int gridPos = WorldToGridPosition(worldPosition);
        return IsValidFloorPosition(gridPos);
    }

    public bool IsValidFloorPosition(Vector2Int gridPos)
    {
        return gridPos.x >= 0 && gridPos.x < gridWidth &&
               gridPos.y >= 0 && gridPos.y < gridHeight &&
               grid[gridPos.x, gridPos.y] == CellType.Floor;
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

    public Vector2Int WorldToGridPosition(Vector2 worldPosition)
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
        if (grid == null) return;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector2 cellPosition = GridToWorldPosition(new Vector2Int(x, y));
                Gizmos.color = grid[x, y] == CellType.Floor ? Color.green : Color.red;
                Gizmos.DrawWireCube(cellPosition, Vector2.one * cellSize);
            }
        }
    }

    public void ResetGrid()
    {
        HideGrid();
        InitializeGrid();
        ShowGrid();
    }
}

// Simple Object Pool implementation
public class ObjectPool
{
    private GameObject prefab;
    private List<GameObject> pool;
    private Transform parent;

    public ObjectPool(GameObject prefab, int initialSize, Transform parent)
    {
        this.prefab = prefab;
        this.parent = parent;
        pool = new List<GameObject>();

        for (int i = 0; i < initialSize; i++)
        {
            CreateObject();
        }
    }

    private GameObject CreateObject()
    {
        GameObject obj = GameObject.Instantiate(prefab, parent);
        obj.SetActive(false);
        pool.Add(obj);
        return obj;
    }

    public GameObject GetObject()
    {
        foreach (GameObject obj in pool)
        {
            if (!obj.activeInHierarchy)
            {
                return obj;
            }
        }

        return CreateObject();
    }

    public void ReturnObject(GameObject obj)
    {
        obj.SetActive(false);
    }
}