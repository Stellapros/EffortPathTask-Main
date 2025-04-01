using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum CellType { Empty, Floor, Wall }

/// <summary>
/// Manages the grid layout for the game, handling floor and wall positions.
/// </summary>
public class GridManager : MonoBehaviour
{

    [SerializeField] private int gridWidth = 18;
    [SerializeField] private int gridHeight = 10;
    [SerializeField] public float cellSize = 1f;
    // [SerializeField] private bool centerCells = true;
    [SerializeField] private TextAsset gridLayoutFile;

    [SerializeField] private GameObject floorTilePrefab;

    [SerializeField] private GameObject wallTilePrefab;


    private CellType[,] grid;
    private List<Vector2Int> availableFloorPositions;
    private GameObject[,] gridObjects;
    private ObjectPool floorPool;
    private ObjectPool wallPool;
    private bool[,] occupiedPositions;
    // Add new field to track if we're persistent
    private bool isPersistent = false;
    private Transform poolContainer;

    public int GridWidth => gridWidth;
    public int GridHeight => gridHeight;
    private Vector2Int gridSize;

    private bool isInitialized = false;

    public void Initialize()
    {
        if (isInitialized)
        {
            Debug.Log("GridManager already initialized");
            return;
        }

        try
        {
            // Set default values if not already set in inspector
            if (gridWidth <= 0) gridWidth = 18;
            if (gridHeight <= 0) gridHeight = 10;
            if (cellSize <= 0) cellSize = 1f;

            // Create grid representation if needed

            isInitialized = true;
            Debug.Log($"GridManager initialized with dimensions: {gridWidth}x{gridHeight}, cell size: {cellSize}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to initialize GridManager: {e.Message}\n{e.StackTrace}");
        }
    }

    private void Awake()
    {
        Debug.Log("GridManager Awake - Initializing grid");
        if (!ValidateAndLoadPrefabs())
        {
            Debug.LogError("GridManager: Critical prefabs missing. Component will be disabled.");
            enabled = false;
            return;
        }
        CreatePoolContainer();
        InitializeGrid();
        InitializeObjectPools();
    }

    private void CreatePoolContainer()
    {
        isPersistent = transform.root.gameObject.scene.name == "DontDestroyOnLoad";

        if (poolContainer != null)
        {
            if (isPersistent)
            {
                SceneManager.MoveGameObjectToScene(poolContainer.gameObject, SceneManager.GetActiveScene());
            }
            return;
        }

        try
        {
            GameObject container = new GameObject("GridPool_Container");

            if (isPersistent)
            {
                SceneManager.MoveGameObjectToScene(container, SceneManager.GetActiveScene());
            }

            if (container != null)
            {
                poolContainer = container.transform;

#if UNITY_EDITOR
                if (!isPersistent && !PrefabUtility.IsPartOfPrefabAsset(gameObject))
                {
                    container.transform.SetParent(transform, false);
                }
#else
                if (!isPersistent)
                {
                    container.transform.SetParent(transform, false);
                }
#endif
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"GridManager: Error creating pool container: {e.Message}");
            enabled = false;
        }
    }

    private void InitializeGrid()
    {
        grid = new CellType[gridWidth, gridHeight];
        availableFloorPositions = new List<Vector2Int>();
        gridObjects = new GameObject[gridWidth, gridHeight];
        gridSize = new Vector2Int(gridWidth, gridHeight);
        occupiedPositions = new bool[gridWidth, gridHeight];

        if (gridLayoutFile != null)
        {
            LoadGridFromFile();
        }
        else
        {
            CreateDefaultGrid();
        }

        // Debug log to check available positions
        Debug.Log($"Grid initialized with {availableFloorPositions.Count} available floor positions.");
    }

    public bool IsValidGridPosition(Vector2Int gridPos)
    {
        return gridPos.x >= 0 && gridPos.x < gridWidth &&
               gridPos.y >= 0 && gridPos.y < gridHeight;
    }

    // Method to validate and load prefabs
    private bool ValidateAndLoadPrefabs()
    {
        // Try to load prefabs from Resources if they're not assigned
        if (floorTilePrefab == null)
        {
            floorTilePrefab = Resources.Load<GameObject>("Prefabs/FloorTile");
            if (floorTilePrefab == null)
            {
                Debug.LogError("GridManager: Floor tile prefab is not assigned and couldn't be loaded from Resources/Prefabs/FloorTile!");
                return false;
            }
        }

        if (wallTilePrefab == null)
        {
            wallTilePrefab = Resources.Load<GameObject>("Prefabs/WallTile");
            if (wallTilePrefab == null)
            {
                Debug.LogError("GridManager: Wall tile prefab is not assigned and couldn't be loaded from Resources/Prefabs/WallTile!");
                return false;
            }
        }

        return true;
    }

    private void LoadGridFromFile()
    {
        if (gridLayoutFile == null)
        {
            Debug.LogError("Grid layout file is not assigned!");
            return;
        }

        string[] rows = gridLayoutFile.text.Split('\n');
        Debug.Log($"Loaded grid layout with {rows.Length} rows");

        for (int y = 0; y < gridHeight; y++)
        {
            if (y >= rows.Length)
            {
                Debug.LogWarning($"Row {y} is missing in the layout file. Filling with walls.");
                for (int x = 0; x < gridWidth; x++)
                {
                    grid[x, y] = CellType.Wall;
                }
                continue;
            }

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
                    grid[x, y] = CellType.Wall; // Fill missing columns with walls
                }
            }
        }
        Debug.Log($"Available floor positions after loading layout: {availableFloorPositions.Count}");
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
        if (!ValidateAndLoadPrefabs())
        {
            Debug.LogError("GridManager: Failed to initialize object pools due to missing prefabs.");
            enabled = false;
            return;
        }

        try
        {
            // Create a container if needed
            if (poolContainer == null)
            {
                CreatePoolContainer();
            }

            if (poolContainer == null)
            {
                Debug.LogError("GridManager: Failed to create pool container!");
                return;
            }

            // Initialize pools with instantiated prefabs
            floorPool = new ObjectPool(InstantiatePrefab(floorTilePrefab), 100, poolContainer);
            wallPool = new ObjectPool(InstantiatePrefab(wallTilePrefab), 100, poolContainer);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"GridManager: Error initializing object pools: {e.Message}");
            enabled = false;
        }
    }

    private GameObject InstantiatePrefab(GameObject prefab)
    {
        if (prefab == null) return null;

        GameObject instance;
#if UNITY_EDITOR
        instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
#else
        instance = Instantiate(prefab);
#endif

        if (instance == null)
        {
            Debug.LogError($"GridManager: Failed to instantiate prefab {prefab.name}");
            return null;
        }

        if (isPersistent)
        {
            SceneManager.MoveGameObjectToScene(instance, SceneManager.GetActiveScene());
        }

        instance.SetActive(false);

#if UNITY_EDITOR
        if (!PrefabUtility.IsPartOfPrefabAsset(instance))
        {
            instance.transform.SetParent(poolContainer, false);
        }
#else
        instance.transform.SetParent(poolContainer, false);
#endif

        return instance;
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

    public void ResetAvailablePositions()
    {
        availableFloorPositions.Clear();
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (grid[x, y] == CellType.Floor)
                {
                    availableFloorPositions.Add(new Vector2Int(x, y));
                }
            }
        }
        Debug.Log($"Reset available positions. New count: {availableFloorPositions.Count}");
    }


    public Vector2 GetRandomAvailablePosition()
    {
        if (availableFloorPositions.Count == 0)
        {
            Debug.LogWarning("No available floor positions left! Resetting grid.");
            ResetAvailablePositions();
        }

        if (availableFloorPositions.Count == 0)
        {
            Debug.LogError("No positions available even after resetting the grid!");
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
            Debug.Log($"Released position: {gridPos}");
        }
    }

    /// <summary>
    /// find a position exactly 5 cells away
    /// </summary>
    public Vector2 GetPositionAtDistance(Vector2 startPosition, int exactDistance)
    {
        try
        {
            Vector2Int startGridPos = WorldToGridPosition(startPosition);
            List<Vector2Int> candidatePositions = new List<Vector2Int>();

            // Systematically search all positions exactly 'exactDistance' away
            for (int x = -exactDistance; x <= exactDistance; x++)
            {
                for (int y = -exactDistance; y <= exactDistance; y++)
                {
                    // Ensure position is exactly 'exactDistance' away using Manhattan distance
                    if (Mathf.Abs(x) + Mathf.Abs(y) != exactDistance)
                        continue;

                    Vector2Int testPos = startGridPos + new Vector2Int(x, y);

                    // Verify position is within grid, a valid floor tile, and not occupied
                    if (IsValidPosition(testPos))
                    {
                        candidatePositions.Add(testPos);
                    }
                }
            }

            // If no positions found exactly at the specified distance
            if (candidatePositions.Count == 0)
            {
                Debug.LogWarning($"No positions found exactly {exactDistance} cells away. Trying alternative approaches.");

                // Try expanding search with more flexible criteria
                return FindAlternativePosition(startPosition, exactDistance);
            }

            // Randomly select from valid positions
            Vector2Int selectedGridPos = candidatePositions[Random.Range(0, candidatePositions.Count)];
            Vector2 selectedWorldPos = GridToWorldPosition(selectedGridPos);

            // Double-check the distance
            float actualDistance = Vector2.Distance(startPosition, selectedWorldPos);
            Debug.Log($"Selected Position - Grid: {selectedGridPos}, World: {selectedWorldPos}, Actual Distance: {actualDistance}");

            return selectedWorldPos;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error finding position at distance {exactDistance}: {e.Message}");
            return FindAlternativePosition(startPosition, exactDistance);
        }
    }

    private Vector2 FindAlternativePosition(Vector2 startPosition, int exactDistance)
    {
        // More flexible search strategy
        for (int offset = 1; offset <= exactDistance * 2; offset++)
        {
            List<Vector2Int> expandedCandidates = new List<Vector2Int>();
            Vector2Int startGridPos = WorldToGridPosition(startPosition);

            for (int x = -exactDistance - offset; x <= exactDistance + offset; x++)
            {
                for (int y = -exactDistance - offset; y <= exactDistance + offset; y++)
                {
                    Vector2Int testPos = startGridPos + new Vector2Int(x, y);

                    // More relaxed distance check
                    float distance = Vector2.Distance(startGridPos, testPos);
                    if (Mathf.Abs(distance - exactDistance) <= 1.0f && IsValidPosition(testPos))
                    {
                        expandedCandidates.Add(testPos);
                    }
                }
            }

            if (expandedCandidates.Count > 0)
            {
                Vector2Int selectedGridPos = expandedCandidates[Random.Range(0, expandedCandidates.Count)];
                Vector2 alternativePos = GridToWorldPosition(selectedGridPos);

                Debug.LogWarning($"Alternative position found: {alternativePos}, Distance from start: {Vector2.Distance(startPosition, alternativePos)}");
                return alternativePos;
            }
        }

        // Absolute fallback
        Debug.LogError("Could not find any valid position near the start!");
        return startPosition;
    }


    public bool IsValidPosition(Vector2 worldPosition)
    {
        Vector2Int gridPos = WorldToGridPosition(worldPosition);
        return IsValidFloorPosition(gridPos);
    }

    public void EnsureInitialization()
    {
        if (grid == null || !enabled)
        {
            Debug.Log("GridManager - Late initialization triggered");
            try
            {
                if (!ValidateAndLoadPrefabs())
                {
                    Debug.LogError("GridManager: Cannot initialize - missing prefabs");
                    enabled = false;
                    return;
                }
                InitializeGrid();
                InitializeObjectPools();
                ShowGrid();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"GridManager: Error during initialization: {e.Message}");
                enabled = false;
            }
        }
    }

    private bool IsValidPosition(Vector2Int testPos)
    {
        // Implement your specific grid validation logic
        return testPos.x >= 0 &&
               testPos.x < gridWidth &&
               testPos.y >= 0 &&
               testPos.y < gridHeight &&
               grid[testPos.x, testPos.y] == CellType.Floor &&
               !occupiedPositions[testPos.x, testPos.y];
    }


    // // Modify IsValidFloorPosition to include null check
    // public bool IsValidFloorPosition(Vector2Int gridPos)
    // {
    //     EnsureInitialization();

    //     if (grid == null)
    //     {
    //         Debug.LogError("Grid is null! Ensure GridManager is properly initialized.");
    //         return false;
    //     }

    // // Ensure position is within walkable area (1-16 x 1-8 for 18x10 grid)
    // return gridPos.x >= 1 && gridPos.x < gridWidth - 1 &&
    //        gridPos.y >= 1 && gridPos.y < gridHeight - 1 &&
    //        grid[gridPos.x, gridPos.y] == CellType.Floor;
    // }


    public bool IsValidFloorPosition(Vector2Int gridPos)
    {
        // Walls are at columns 0,17 and rows 0,9 → Walkable area is (1-16, 1-8)
        bool isInsideWalkableArea =
            gridPos.x > 0 && gridPos.x < gridWidth - 1 &&  // Columns 1-16
            gridPos.y > 0 && gridPos.y < gridHeight - 1;   // Rows 1-8

        // Also ensure the cell is a floor (not a wall)
        bool isFloor = grid[gridPos.x, gridPos.y] == CellType.Floor;

        return isInsideWalkableArea && isFloor;
    }

    // public Vector2 GridToWorldPosition(Vector2Int gridPos)
    // {
    //     Vector2 worldPos = new Vector2(gridPos.x * cellSize, gridPos.y * cellSize);
    //     if (centerCells)
    //     {
    //         worldPos += new Vector2(cellSize * 0.5f, cellSize * 0.5f);
    //     }
    //     Vector2 gridCenter = new Vector2(gridWidth * 0.5f, gridHeight * 0.5f) * cellSize;
    //     return worldPos - gridCenter;
    // }

    // public Vector2Int WorldToGridPosition(Vector2 worldPosition)
    // {
    //     Vector2 gridCenter = new Vector2(gridWidth * 0.5f, gridHeight * 0.5f) * cellSize;
    //     Vector2 offsetPosition = worldPosition + gridCenter;
    //     if (centerCells)
    //     {
    //         offsetPosition -= new Vector2(cellSize * 0.5f, cellSize * 0.5f);
    //     }
    //     return new Vector2Int(
    //         Mathf.FloorToInt(offsetPosition.x / cellSize),
    //         Mathf.FloorToInt(offsetPosition.y / cellSize)
    //     );
    // }

    public Vector2 GridToWorldPosition(Vector2Int gridPos)
    {
        // Calculate the exact center of the grid
        float gridCenterX = (gridWidth - 1) * cellSize * 0.5f;
        float gridCenterY = (gridHeight - 1) * cellSize * 0.5f;

        // Calculate world position, accounting for grid center
        Vector2 worldPos = new Vector2(
            gridPos.x * cellSize - gridCenterX,
            gridPos.y * cellSize - gridCenterY
        );

        return worldPos;
    }

    // public Vector2Int WorldToGridPosition(Vector2 worldPosition)
    // {
    //     // Calculate the exact center of the grid
    //     float gridCenterX = (gridWidth - 1) * cellSize * 0.5f;
    //     float gridCenterY = (gridHeight - 1) * cellSize * 0.5f;

    //     // Offset the world position by the grid center
    //     Vector2 offsetPosition = worldPosition + new Vector2(gridCenterX, gridCenterY);

    //     return new Vector2Int(
    //         Mathf.FloorToInt(offsetPosition.x / cellSize),
    //         Mathf.FloorToInt(offsetPosition.y / cellSize)
    //     );
    // }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Vector2 walkableMin = GridToWorldPosition(new Vector2Int(1, 1));
        Vector2 walkableMax = GridToWorldPosition(new Vector2Int(gridWidth - 2, gridHeight - 2));
        Gizmos.DrawWireCube((walkableMin + walkableMax) * 0.5f, walkableMax - walkableMin);
    }


    public Vector2Int WorldToGridPosition(Vector2 worldPosition)
    {
        // Calculate grid center (accounting for cellSize)
        float gridCenterX = (gridWidth * cellSize) / 2f;
        float gridCenterY = (gridHeight * cellSize) / 2f;

        // Convert world position to grid space (with proper offset)
        float gridX = (worldPosition.x + gridCenterX) / cellSize;
        float gridY = (worldPosition.y + gridCenterY) / cellSize;

        // Clamp to ensure it stays within grid bounds
        int x = Mathf.Clamp(Mathf.FloorToInt(gridX), 0, gridWidth - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt(gridY), 0, gridHeight - 1);

        return new Vector2Int(x, y);
    }

    public class ObjectPool
    {
        private GameObject prefabInstance;
        private List<GameObject> pool;
        private Transform parent;
        private int initialSize;

        public ObjectPool(GameObject prefabInstance, int initialSize, Transform parent)
        {
            if (prefabInstance == null)
            {
                Debug.LogError("ObjectPool: Cannot initialize with null prefab instance!");
                return;
            }

            this.prefabInstance = prefabInstance;
            this.initialSize = initialSize;
            this.parent = parent;
            pool = new List<GameObject>();

            CreateInitialObjects();
        }

        private void CreateInitialObjects()
        {
            for (int i = 0; i < initialSize; i++)
            {
                CreateObject();
            }
        }

        private GameObject CreateObject()
        {
            if (prefabInstance == null)
            {
                Debug.LogError("ObjectPool: Prefab instance is null!");
                return null;
            }

            try
            {
                GameObject obj = Object.Instantiate(prefabInstance);

                if (obj != null && parent != null)
                {
                    obj.transform.SetParent(parent, false);
                    obj.SetActive(false);
                    pool.Add(obj);
                }

                return obj;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"ObjectPool: Error creating object: {e.Message}");
                return null;
            }
        }

        public GameObject GetObject()
        {
            pool.RemoveAll(item => item == null);
            GameObject obj = pool.Find(item => !item.activeInHierarchy);

            if (obj == null)
            {
                obj = CreateObject();
                if (obj == null)
                {
                    Debug.LogError("ObjectPool: Failed to create new object!");
                    return null;
                }
            }

            return obj;
        }

        public void ReturnObject(GameObject obj)
        {
            if (obj == null) return;

            try
            {
                obj.transform.localPosition = Vector3.zero;
                obj.transform.localRotation = Quaternion.identity;

                if (parent != null && obj.transform.parent != parent)
                {
                    obj.transform.SetParent(parent, false);
                }

                obj.SetActive(false);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"ObjectPool: Error returning object: {e.Message}");
            }
        }

        public void UpdateParent(Transform newParent)
        {
            if (newParent == null)
            {
                Debug.LogWarning("ObjectPool: Attempting to set null parent!");
                return;
            }

            this.parent = newParent;

            foreach (GameObject obj in pool)
            {
                if (obj != null)
                {
                    try
                    {
                        obj.transform.SetParent(newParent, false);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"ObjectPool: Error updating parent: {e.Message}");
                    }
                }
            }
        }

        public void CleanupPool()
        {
            foreach (GameObject obj in pool)
            {
                if (obj != null)
                {
                    Object.Destroy(obj);
                }
            }
            pool.Clear();
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (isPersistent)
        {
            // Recreate our pool container in the new scene
            CreatePoolContainer();
            // Reinitialize pools if needed
            if (floorPool != null)
            {
                floorPool.UpdateParent(poolContainer);
            }
            if (wallPool != null)
            {
                wallPool.UpdateParent(poolContainer);
            }
            // Refresh the grid visualization
            HideGrid();
            ShowGrid();
        }
    }

    public void CleanupGrid()
    {
        HideGrid();
        if (poolContainer != null)
        {
            Destroy(poolContainer.gameObject);
        }
    }
}