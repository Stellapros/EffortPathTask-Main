using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

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
        gridSize = new Vector2Int(gridWidth, gridHeight);
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
        Debug.Log($"Available positions count: {availableFloorPositions.Count}");
        Debug.Log($"Grid size: {gridWidth}x{gridHeight}");

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

    public Vector2 GridToWorldPosition(Vector2Int gridPos)
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