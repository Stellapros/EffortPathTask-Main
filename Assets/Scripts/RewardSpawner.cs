using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages the spawning and clearing of rewards in the GridWorld scene.
/// </summary>
public class RewardSpawner : MonoBehaviour
{
    // [SerializeField] private GameObject rewardPrefab;
    [SerializeField] private List<GameObject> rewardPrefabs; // List of 6 different reward prefabs
    [SerializeField] private GridManager gridManager;
    [SerializeField] private ExperimentManager experimentManager;

    private GameObject currentReward;

    private void Awake()
    {
        ValidateComponents();
    }

    public void SetGridManager(GridManager manager)
{
    gridManager = manager;
    Debug.Log("GridManager set in RewardSpawner");
}

    /// <summary>
    /// Validates that all required components are assigned or found in the scene.
    /// </summary>
    private void ValidateComponents()
    {
        if (gridManager == null)
        {
            gridManager = FindAnyObjectByType<GridManager>();
            if (gridManager == null)
            {
                Debug.LogError("GridManager not found in the scene. Please ensure it exists in the scene.");
            }
        }

        if (experimentManager == null)
        {
            experimentManager = FindAnyObjectByType<ExperimentManager>();
            if (experimentManager == null)
            {
                Debug.LogError("ExperimentManager not found in the scene. Please ensure it exists in the scene.");
            }
        }

        if (rewardPrefabs == null || rewardPrefabs.Count == 0)
        {
            Debug.LogError("Reward prefabs are not assigned in RewardSpawner. Please assign them in the inspector.");
        }
    }

    /// <summary>
    /// Spawns a reward at the specified position with the given parameters.
    /// </summary>
    /// <param name="rewardPosition">The position to spawn the reward.</param>
    /// <param name="blockIndex">The current block index.</param>
    /// <param name="trialIndex">The current trial index.</param>
    /// <param name="pressesRequired">The number of presses required to collect the reward.</param>
    /// <param name="scoreValue">The score value of the reward.</param>
    /// <returns>The spawned reward GameObject, or null if spawning failed.</returns>
    public GameObject SpawnReward(Vector2 playerPosition, int blockIndex, int trialIndex, int pressesRequired, int scoreValue)
    {
        if (currentReward != null)
        {
            Debug.LogWarning("Attempting to spawn a reward when one already exists. Clearing existing reward first.");
            ClearReward();
        }

        if (gridManager == null || rewardPrefabs == null || rewardPrefabs.Count == 0)
        {
            Debug.LogError("Cannot spawn reward: Required components are missing.");
            return null;
        }

        float distance = experimentManager.GetCurrentBlockDistance();
        Vector2 rewardPosition = GetSpawnPositionAtDistance(playerPosition, distance);

        GameObject selectedRewardPrefab = GetRandomRewardPrefab();
        if (selectedRewardPrefab == null)
        {
            Debug.LogError("Failed to select a random reward prefab.");
            return null;
        }

        Vector3 spawnPosition = new Vector3(rewardPosition.x, rewardPosition.y, 0f);
        currentReward = Instantiate(selectedRewardPrefab, spawnPosition, Quaternion.identity);

        if (currentReward == null)
        {
            Debug.LogError("Failed to instantiate reward prefab.");
            return null;
        }

        Reward rewardComponent = currentReward.GetComponent<Reward>();
        if (rewardComponent != null)
        {
            rewardComponent.SetRewardParameters(blockIndex, trialIndex, pressesRequired, scoreValue);
            Debug.Log($"Reward spawned at {currentReward.transform.position}, Block: {blockIndex}, Trial: {trialIndex}, Distance: {distance}");
            return currentReward;
        }
        else
        {
            Debug.LogError("Reward component not found on spawned reward!");
            Destroy(currentReward);
            currentReward = null;
            return null;
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
    /// <summary>
    /// Gets a random spawn position from the grid manager.
    /// </summary>
    /// <returns>A random available position on the grid.</returns>
    // public Vector2 GetRandomSpawnPosition()
    // {
    //     if (gridManager == null)
    //     {
    //         Debug.LogError("Cannot get random spawn position: GridManager is not assigned.");
    //         return Vector2.zero;
    //     }
    //     return gridManager.GetRandomAvailablePosition();
    // }
    // public Vector2 GetSpawnPositionAtDistance(Vector2 playerPosition, float distance)
    // {
    //     if (gridManager == null)
    //     {
    //         Debug.LogError("Cannot get spawn position: GridManager is not assigned.");
    //         return Vector2.zero;
    //     }
    //     return gridManager.GetPositionAtDistance(playerPosition, distance);
    // }
    
    // Update GetSpawnPositionAtDistance to use the gridManager reference
public Vector2 GetSpawnPositionAtDistance(Vector2 playerPosition, float distance)
{
    if (gridManager == null)
    {
        Debug.LogError("GridManager not set in RewardSpawner!");
        return playerPosition + Vector2.right * distance; // Fallback
    }
    
    return gridManager.GetPositionAtDistance(playerPosition, distance);
}

    /// <summary>
    /// Clears the current reward from the scene.
    /// </summary>
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