using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class RewardSpawner : MonoBehaviour
{
    [SerializeField] private GameObject rewardPrefab;
    [SerializeField] private GridManager gridManager;

    private List<GameObject> currentRewards = new List<GameObject>();


    private void Awake()
    {
        // Ensure GridManager is assigned
        if (gridManager == null)
        {
            gridManager = FindObjectOfType<GridManager>();
            if (gridManager == null)
            {
                Debug.LogError("GridManager not found in the scene. Please assign it in the inspector or ensure it exists in the scene.");
            }
        }

        // Ensure rewardPrefab is assigned
        if (rewardPrefab == null)
        {
            Debug.LogError("Reward prefab is not assigned in the RewardSpawner. Please assign it in the inspector.");
        }
    }

    /// <summary>
    /// Spawns a reward with the given parameters.
    /// </summary>
    /// <returns>The spawned reward GameObject, or null if spawning failed.</returns>
    public GameObject SpawnReward(int blockIndex, int trialIndex, int pressesRequired)
    {
        Debug.Log("SpawnReward called");

        if (currentRewards.Count > 0)
        {
            Debug.LogWarning("Attempting to spawn a reward when one already exists. Clearing existing rewards first.");
            ClearReward();
        }


        if (gridManager == null)
        {
            Debug.LogError("GridManager is null. Cannot spawn reward.");
            return null;
        }

        if (rewardPrefab == null)
        {
            Debug.LogError("Reward prefab is null. Cannot spawn reward.");
            return null;
        }

        ClearReward(); // Clear any existing rewards before spawning a new one

        Vector2 intendedRewardPosition = gridManager.GetRandomAvailablePosition();
        GameObject newReward = Instantiate(rewardPrefab, intendedRewardPosition, Quaternion.identity);

        if (newReward == null)
        {
            Debug.LogError("Failed to instantiate reward prefab.");
            return null;
        }

        Reward rewardComponent = newReward.GetComponent<Reward>();
        if (rewardComponent != null)
        {
            rewardComponent.SetRewardParameters(blockIndex, trialIndex, pressesRequired);
            currentRewards.Add(newReward);
            Vector2 actualRewardPosition = newReward.transform.position;

            Debug.Log($"Attempting to spawn reward at: {intendedRewardPosition}");
            Debug.Log($"Reward actually spawned at position: {actualRewardPosition}");

            // Log both intended and actual positions using LogManager
            if (LogManager.instance != null)
            {
                LogManager.instance.WriteTimeStampedEntry($"Reward spawn - Intended: {intendedRewardPosition}, Actual: {actualRewardPosition}");
            }
            else
            {
                Debug.LogWarning("LogManager instance is null. Cannot log reward spawn position.");
            }

            return newReward;
        }
        else
        {
            Debug.LogError("Reward component not found on spawned reward!");
            Destroy(newReward);
            return null;
        }
    }


    // Get a random spawn position from the grid manager - adopted from PlayerSpawner
    public Vector2 GetRandomSpawnPosition()
    {
        return gridManager.GetRandomAvailablePosition();
    }



    /// <summary>
    /// Clears all current rewards from the scene.
    /// </summary>
    public void ClearReward()
    {
        Debug.Log("ClearReward called");
        foreach (GameObject reward in currentRewards)
        {
            if (currentRewards != null)
            {
                if (gridManager != null)
                {
                    gridManager.ReleasePosition(reward.transform.position);
                }
                Destroy(reward);
            }
        }
        currentRewards.Clear();

        // Double-check for any remaining rewards
        StartCoroutine(FinalRewardCheck());
    }


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

        // Log the final reward count
        if (LogManager.instance != null)
        {
            LogManager.instance.WriteTimeStampedEntry($"Final reward count after clearing: {remainingRewards.Length}");
        }
        else
        {
            Debug.LogWarning("LogManager instance is null. Cannot log final reward count.");
        }
    }
}