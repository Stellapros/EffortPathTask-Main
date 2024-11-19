using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
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
    // Add new field to track if we're persistent
    private bool isPersistent = false;
    private Transform poolContainer;

    public int GridWidth => gridWidth;
    public int GridHeight => gridHeight;

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
        // Check if we're marked as persistent
        isPersistent = transform.root.gameObject.scene.name == "DontDestroyOnLoad";

        // Check if we already have a pool container
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
            // Create a new container in the active scene
            GameObject container = new GameObject("GridPool_Container");

            // Ensure the container is created in the correct scene first
            if (isPersistent)
            {
                SceneManager.MoveGameObjectToScene(container, SceneManager.GetActiveScene());
            }

            // Set up hierarchy after scene management
            if (container != null)
            {
                poolContainer = container.transform;

                // Only set parent if we're not persistent and the current object is not a prefab asset
                if (!isPersistent && !PrefabUtility.IsPartOfPrefabAsset(gameObject))
                {
                    container.transform.SetParent(transform, false);
                }
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
occupiedPositions = new bool[gridWidth, gridHeight]; // Initialize the occupiedPositions array


        if (gridLayoutFile != null)
        {
            LoadGridFromFile();
        }
        else
        {
            CreateDefaultGrid();
        }

            // Reset all positions to unoccupied
    for (int x = 0; x < gridWidth; x++)
    {
        for (int y = 0; y < gridHeight; y++)
        {
            occupiedPositions[x, y] = false;
        }
    }
    }

    // Add these methods to manage occupied positions
public void SetPositionOccupied(Vector2 worldPosition, bool occupied)
{
    Vector2Int gridPos = WorldToGridPosition(worldPosition);
    if (IsValidGridPosition(gridPos))
    {
        occupiedPositions[gridPos.x, gridPos.y] = occupied;
    }
}

public bool IsPositionOccupied(Vector2 worldPosition)
{
    Vector2Int gridPos = WorldToGridPosition(worldPosition);
    return IsValidGridPosition(gridPos) && occupiedPositions[gridPos.x, gridPos.y];
}

private bool IsValidGridPosition(Vector2Int gridPos)
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

    // Add this method to check if grid is initialized
    public bool IsInitialized()
    {
        return grid != null && gridObjects != null;
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

    /// <summary>
    /// Sets the size of the grid and reinitializes it with the new dimensions.
    /// </summary>
    /// <param name="width">The new width of the grid</param>
    /// <param name="height">The new height of the grid</param>
    public void SetGridSize(int width, int height)
    {
        // Store new dimensions
        this.gridWidth = Mathf.Max(1, width);
        this.gridHeight = Mathf.Max(1, height);

        // Clean up existing grid if any
        HideGrid();

        // Reinitialize with new dimensions
        InitializeGrid();
        InitializeObjectPools();
        ShowGrid();
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

        // Create an instance in the scene
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

        if (instance == null)
        {
            Debug.LogError($"GridManager: Failed to instantiate prefab {prefab.name}");
            return null;
        }

        // Ensure the instance is in the correct scene
        if (isPersistent)
        {
            SceneManager.MoveGameObjectToScene(instance, SceneManager.GetActiveScene());
        }

        // Set initial state
        instance.SetActive(false);

        // Only set parent if the instance is not a prefab asset
        if (!PrefabUtility.IsPartOfPrefabAsset(instance))
        {
            instance.transform.SetParent(poolContainer, false);
        }

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

// Modified GetPositionAtDistance method with null check and better error handling
public Vector2 GetPositionAtDistance(Vector2 startPosition, float distance)
{
    if (occupiedPositions == null)
    {
        Debug.LogError("occupiedPositions array is not initialized!");
        return startPosition; // Return original position as fallback
    }

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

    // public Vector2 GetPositionAtDistance(Vector2 startPosition, float distance)
    // {
    //     Vector2Int startGridPos = WorldToGridPosition(startPosition);
    //     List<Vector2Int> candidatePositions = new List<Vector2Int>();

    //     for (int x = 0; x < gridWidth; x++)
    //     {
    //         for (int y = 0; y < gridHeight; y++)
    //         {
    //             if (grid[x, y] == CellType.Floor && !occupiedPositions[x, y])
    //             {
    //                 Vector2Int gridPos = new Vector2Int(x, y);
    //                 float gridDistance = Vector2Int.Distance(startGridPos, gridPos);
    //                 if (Mathf.Abs(gridDistance - distance) < 0.5f) // Allow some tolerance
    //                 {
    //                     candidatePositions.Add(gridPos);
    //                 }
    //             }
    //         }
    //     }
    //     if (candidatePositions.Count == 0)
    //     {
    //         Debug.LogWarning("No positions found at the exact distance. Returning closest available position.");
    //         return GetClosestAvailablePosition(startPosition, distance);
    //     }

    //     Vector2Int selectedGridPos = candidatePositions[Random.Range(0, candidatePositions.Count)];
    //     return GridToWorldPosition(selectedGridPos);
    // }
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


    // Modify IsValidFloorPosition to include null check
    public bool IsValidFloorPosition(Vector2Int gridPos)
    {
        EnsureInitialization();

        if (grid == null)
        {
            Debug.LogError("Grid is null! Ensure GridManager is properly initialized.");
            return false;
        }

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
                // Create a new instance by copying the prefab instance
                GameObject obj = Object.Instantiate(prefabInstance);

                if (obj != null && parent != null && !PrefabUtility.IsPartOfPrefabAsset(obj))
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
            // Clean up any null references
            pool.RemoveAll(item => item == null);

            // Try to find an inactive object
            GameObject obj = pool.Find(item => !item.activeInHierarchy);

            // If no inactive object is found, create a new one
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
                // Only modify transform if the object is not a prefab asset
                if (!PrefabUtility.IsPartOfPrefabAsset(obj))
                {
                    // Reset transform
                    obj.transform.localPosition = Vector3.zero;
                    obj.transform.localRotation = Quaternion.identity;

                    // Make sure it's in the right parent
                    if (parent != null && obj.transform.parent != parent)
                    {
                        obj.transform.SetParent(parent, false);
                    }
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
                if (obj != null && !PrefabUtility.IsPartOfPrefabAsset(obj))
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