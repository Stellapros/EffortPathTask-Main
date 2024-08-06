using UnityEngine;
using System.Collections.Generic;

public class SpawnManager : MonoBehaviour
{
    [SerializeField] private GameObject rewardPrefab;
    [SerializeField] private float gridSize = 0.1f;
    [SerializeField] private float ySpawnPosition = 0.5f;

    [SerializeField] private float minX = -1.0f;
    [SerializeField] private float maxX = 1.0f;
    [SerializeField] private float minZ = -1.0f;
    [SerializeField] private float maxZ = 1.0f;

    private int gridWidth;
    private int gridLength;
    private Vector3 gridOrigin;
    private List<Vector2Int> availableGridPositions;

    private void Awake()
    {
        CalculateGridDimensions();
        InitializeGrid();
    }


    private void CalculateGridDimensions()
    {
        gridWidth = Mathf.CeilToInt((maxX - minX) / gridSize);
        gridLength = Mathf.CeilToInt((maxZ - minZ) / gridSize);
        gridOrigin = new Vector3(minX, 0f, minZ);

        Debug.Log($"Grid Dimensions - Width: {gridWidth}, Length: {gridLength}");
        Debug.Log($"Grid Origin: {gridOrigin}");
    }

    private void InitializeGrid()
    {
        availableGridPositions = new List<Vector2Int>();
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridLength; z++)
            {
                availableGridPositions.Add(new Vector2Int(x, z));
            }
        }
        Debug.Log($"Total grid positions: {availableGridPositions.Count}");
    }

    public void SpawnReward(int blockIndex, int trialIndex, int pressesRequired)
    {
        if (availableGridPositions.Count == 0)
        {
            Debug.LogWarning("No available grid positions for spawning reward!");
            return;
        }

        int randomIndex = Random.Range(0, availableGridPositions.Count);
        Vector2Int gridPosition = availableGridPositions[randomIndex];
        availableGridPositions.RemoveAt(randomIndex);

        Vector3 worldPosition = GridToWorldPosition(gridPosition);
        GameObject spawnedReward = Instantiate(rewardPrefab, worldPosition, Quaternion.identity);

        Reward rewardComponent = spawnedReward.GetComponent<Reward>();
        if (rewardComponent != null)
        {
            rewardComponent.SetRewardParameters(blockIndex, trialIndex, pressesRequired);
        }

        Debug.Log($"Spawned reward at grid position: {gridPosition}, world position: {worldPosition}");
    }


    private Vector3 GridToWorldPosition(Vector2Int gridPosition)
    {
        float worldX = minX + gridPosition.x * gridSize;
        float worldZ = minZ + gridPosition.y * gridSize;
        Vector3 worldPosition = new Vector3(worldX, ySpawnPosition, worldZ);

        // Clamp the position to ensure it's within bounds
        worldPosition.x = Mathf.Clamp(worldPosition.x, minX, maxX);
        worldPosition.z = Mathf.Clamp(worldPosition.z, minZ, maxZ);

        return worldPosition;
    }


    public void ClearReward()
    {
        foreach (GameObject reward in GameObject.FindGameObjectsWithTag("Reward"))
        {
            Vector2Int gridPosition = WorldToGridPosition(reward.transform.position);
            if (!availableGridPositions.Contains(gridPosition))
            {
                availableGridPositions.Add(gridPosition);
            }
            Destroy(reward);
        }
    }


    private Vector2Int WorldToGridPosition(Vector3 worldPosition)
    {
        int gridX = Mathf.FloorToInt((worldPosition.x - minX) / gridSize);
        int gridZ = Mathf.FloorToInt((worldPosition.z - minZ) / gridSize);
        return new Vector2Int(gridX, gridZ);
    }



    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Vector3 gridSize3D = new Vector3(gridSize, 0.1f, gridSize);

            for (int x = 0; x < gridWidth; x++)
            {
                for (int z = 0; z < gridLength; z++)
                {
                    Vector3 worldPos = GridToWorldPosition(new Vector2Int(x, z));
                    Gizmos.DrawWireCube(worldPos, gridSize3D);
                }
            }

            // Draw border
            Gizmos.color = Color.red;
            Gizmos.DrawLine(new Vector3(minX, 0, minZ), new Vector3(minX, 0, maxZ));
            Gizmos.DrawLine(new Vector3(maxX, 0, minZ), new Vector3(maxX, 0, maxZ));
            Gizmos.DrawLine(new Vector3(minX, 0, minZ), new Vector3(maxX, 0, minZ));
            Gizmos.DrawLine(new Vector3(minX, 0, maxZ), new Vector3(maxX, 0, maxZ));
        }
    }
}