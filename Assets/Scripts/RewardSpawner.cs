using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages the spawning and clearing of rewards in the GridWorld scene.
/// </summary>
public class RewardSpawner : MonoBehaviour
{
    [SerializeField] private List<GameObject> rewardPrefabs;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private ExperimentManager experimentManager;
    [SerializeField] private PlayerSpawner playerSpawner;

    private GameObject currentReward;

    private void Awake()
    {
        ValidateComponents();
    }

    private void ValidateComponents()
    {
        if (gridManager == null)
            gridManager = FindAnyObjectByType<GridManager>();
        if (experimentManager == null)
            experimentManager = FindAnyObjectByType<ExperimentManager>();
        if (playerSpawner == null)
            playerSpawner = FindAnyObjectByType<PlayerSpawner>();

        if (gridManager == null || experimentManager == null || playerSpawner == null)
            Debug.LogError("Required components are missing in RewardSpawner.");
        if (rewardPrefabs == null || rewardPrefabs.Count == 0)
            Debug.LogError("Reward prefabs are not assigned in RewardSpawner.");
    }

    public GameObject SpawnReward(int blockIndex, int trialIndex, int pressesRequired, int scoreValue)
    {
        Debug.Log($"SpawnReward called: Block {blockIndex}, Trial {trialIndex}");

        if (currentReward != null)
        {
            Debug.LogWarning("Clearing existing reward before spawning new one.");
            ClearReward();
        }

        Vector2 playerPosition = GetPlayerPosition();
        Debug.Log($"Player position: {playerPosition}");

        float distance = experimentManager.GetCurrentBlockDistance();
        Debug.Log($"Desired distance: {distance}");

        // Get a valid spawn position within the grid
        Vector2 spawnPosition = GetValidSpawnPosition(playerPosition, distance);
        Debug.Log($"Calculated spawn position: {spawnPosition}");

        if (spawnPosition == Vector2.negativeInfinity)
        {
            Debug.LogError("Failed to find a valid spawn position within the grid. Aborting spawn.");
            return null;
        }

        GameObject selectedRewardPrefab = GetRandomRewardPrefab();
        if (selectedRewardPrefab == null)
        {
            Debug.LogError("Failed to select a random reward prefab. Aborting spawn.");
            return null;
        }

        currentReward = Instantiate(selectedRewardPrefab, spawnPosition, Quaternion.identity);
        Debug.Log($"Reward instantiated: {currentReward != null}");

        if (currentReward != null)
        {
            Reward rewardComponent = currentReward.GetComponent<Reward>();
            if (rewardComponent != null)
            {
                rewardComponent.SetRewardParameters(blockIndex, trialIndex, pressesRequired, scoreValue);
                Debug.Log("Reward parameters set successfully.");
            }
            else
            {
                Debug.LogError("Reward component not found on spawned reward!");
            }
        }
        else
        {
            Debug.LogError("Failed to instantiate reward prefab.");
        }

        return currentReward;
    }

    private Vector2 GetValidSpawnPosition(Vector2 playerPosition, float targetDistance)
    {
        int maxAttempts = 100;
        for (int i = 0; i < maxAttempts; i++)
        {
            // Generates a random direction
            Vector2 randomDirection = Random.insideUnitCircle.normalized;

            // Calculates a target position by adding the random direction multiplied by the target distance to the player's position
            Vector2 targetPosition = playerPosition + randomDirection * targetDistance;

            // Converts the world position to a grid position
            Vector2Int gridPosition = gridManager.WorldToGridPosition(targetPosition);

            // Checks if the grid position is valid; If valid, returns the center world position of that grid cell
            if (gridManager.IsValidFloorPosition(gridPosition))
            {
                return gridManager.GetCellCenterWorldPosition(gridPosition);
            }
        }

        Debug.LogWarning($"Failed to find valid spawn position after {maxAttempts} attempts.");
        return Vector2.negativeInfinity;
    }

    private Vector2 GetPlayerPosition()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            return player.transform.position;
        }
        else
        {
            Debug.LogError("Player not found in scene.");
            return Vector2.negativeInfinity;
        }
    }

    private GameObject GetRandomRewardPrefab()
    {
        if (rewardPrefabs == null || rewardPrefabs.Count == 0)
        {
            Debug.LogError("No reward prefabs available.");
            return null;
        }
        int randomIndex = Random.Range(0, rewardPrefabs.Count);
        return rewardPrefabs[randomIndex];
    }

    public Vector2 GetSpawnPositionAtDistance(Vector2 playerPosition, float distance)
    {
        Debug.Log($"GetSpawnPositionAtDistance called with playerPosition: {playerPosition}, distance: {distance}");

        if (gridManager == null)
        {
            Debug.LogError("Cannot get spawn position: GridManager is not assigned.");
            return Vector2.zero;
        }

        if (float.IsNaN(distance) || float.IsInfinity(distance))
        {
            Debug.LogError($"Invalid distance value: {distance}");
            return Vector2.zero;
        }

        Vector2Int playerGridPos = gridManager.WorldToGridPosition(playerPosition);
        int gridDistance = Mathf.RoundToInt(distance / gridManager.cellSize);
        Vector2Int rewardGridPos = gridManager.GetGridPositionAtDistance(playerGridPos, gridDistance);
        Vector2 spawnPosition = gridManager.GetCellCenterWorldPosition(rewardGridPos);

        float actualDistance = Vector2.Distance(playerPosition, spawnPosition);
        Debug.Log($"Spawn position found: {spawnPosition}. Actual distance from player: {actualDistance}");

        return spawnPosition;
    }

    private Vector2 FindNearestValidPosition(Vector2 initialPosition)
    {
        // Define a search radius and increment
        float searchRadius = 1f;
        float increment = 0.5f;
        int maxIterations = 20; // Prevent infinite loop

        for (int i = 0; i < maxIterations; i++)
        {
            // Check positions in a circle around the initial position
            for (float angle = 0; angle < 360; angle += 45)
            {
                float radian = angle * Mathf.Deg2Rad;
                Vector2 checkPosition = initialPosition + new Vector2(Mathf.Cos(radian), Mathf.Sin(radian)) * searchRadius;

                if (gridManager.IsValidPosition(checkPosition))
                {
                    Debug.Log($"Found nearest valid position: {checkPosition}");
                    return checkPosition;
                }
            }

            // Increase search radius
            searchRadius += increment;
        }

        Debug.LogError("Could not find a valid position after maximum iterations.");
        return Vector2.zero;
    }

    public void ClearReward()
    {
        if (currentReward != null)
        {
            Debug.Log("Clearing current reward");
            if (gridManager != null)
            {
                gridManager.ReleasePosition(currentReward.transform.position);
            }
            Destroy(currentReward);
            currentReward = null;
        }

        StartCoroutine(FinalRewardCheck());
    }

    /// <summary>
    /// Performs a final check for any remaining rewards and logs the result.
    /// </summary>
    private IEnumerator FinalRewardCheck()
    {
        yield return new WaitForEndOfFrame();
        GameObject[] remainingRewards = GameObject.FindGameObjectsWithTag("Reward");
        if (remainingRewards.Length > 0)
        {
            Debug.LogWarning($"There are still {remainingRewards.Length} rewards in the scene after clearing. Removing them.");
            foreach (var reward in remainingRewards)
            {
                Destroy(reward);
            }
        }
        Debug.Log($"Final reward count after clearing: {remainingRewards.Length}");
    }
}