using UnityEngine;
using System.Collections;

/// <summary>
/// Manages the spawning and clearing of rewards in the GridWorld scene.
/// </summary>
public class RewardSpawner : MonoBehaviour
{
    [SerializeField] private GameObject rewardPrefab;
    [SerializeField] private GridManager gridManager;

    private GameObject currentReward;

    private void Awake()
    {
        ValidateComponents();
    }

    /// <summary>
    /// Validates that all required components are assigned or found in the scene.
    /// </summary>
    private void ValidateComponents()
    {
        if (gridManager == null)
        {
            gridManager = FindObjectOfType<GridManager>();
            if (gridManager == null)
            {
                Debug.LogError("GridManager not found in the scene. Please ensure it exists in the scene.");
            }
        }

        if (rewardPrefab == null)
        {
            Debug.LogError("Reward prefab is not assigned in RewardSpawner. Please assign it in the inspector.");
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
    public GameObject SpawnReward(Vector2 rewardPosition, int blockIndex, int trialIndex, int pressesRequired, int scoreValue)
    {
        Debug.Log($"Attempting to spawn reward at position: {rewardPosition}");

        if (currentReward != null)
        {
            Debug.LogWarning("Attempting to spawn a reward when one already exists. Clearing existing reward first.");
            ClearReward();
        }

        if (gridManager == null || rewardPrefab == null)
        {
            Debug.LogError("Cannot spawn reward: GridManager or reward prefab is missing.");
            return null;
        }

        Vector3 spawnPosition = new Vector3(rewardPosition.x, rewardPosition.y, 0f);
        currentReward = Instantiate(rewardPrefab, spawnPosition, Quaternion.identity);
        Debug.Log($"Reward instantiated: {currentReward != null}");

        if (currentReward == null)
        {
            Debug.LogError("Failed to instantiate reward prefab.");
            return null;
        }

        Reward rewardComponent = currentReward.GetComponent<Reward>();
        if (rewardComponent != null)
        {
            rewardComponent.SetRewardParameters(blockIndex, trialIndex, pressesRequired, scoreValue);
            Vector2 actualRewardPosition = currentReward.transform.position;

            Debug.Log($"Reward spawned - Intended: {rewardPosition}, Actual: {actualRewardPosition}, " +
                      $"Block: {blockIndex}, Trial: {trialIndex}, Presses required: {pressesRequired}, Score: {scoreValue}");
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

    /// <summary>
    /// Gets a random spawn position from the grid manager.
    /// </summary>
    /// <returns>A random available position on the grid.</returns>
    public Vector2 GetRandomSpawnPosition()
    {
        if (gridManager == null)
        {
            Debug.LogError("Cannot get random spawn position: GridManager is not assigned.");
            return Vector2.zero;
        }
        return gridManager.GetRandomAvailablePosition();
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