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
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private bool centerCells = true;
    [SerializeField] private TextAsset gridLayoutFile;
    [SerializeField] private GameObject floorTilePrefab;
    [SerializeField] private GameObject wallTilePrefab;

    private CellType[,] grid;
    private List<Vector2Int> availableFloorPositions;
    private GameObject[,] gridObjects;
    private ObjectPool floorPool;
    private ObjectPool wallPool;

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

    private bool IsValidFloorPosition(Vector2Int gridPos)
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