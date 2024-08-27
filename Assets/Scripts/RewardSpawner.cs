using UnityEngine;

public class RewardSpawner : MonoBehaviour
{
    [SerializeField] private GameObject rewardPrefab;
    [SerializeField] private GridManager gridManager;

    // Spawn a reward with the given parameters
    public GameObject SpawnReward(int blockIndex, int trialIndex, int pressesRequired)
    {
        Vector2 spawnPosition = gridManager.GetRandomAvailablePosition();
        GameObject spawnedReward = Instantiate(rewardPrefab, spawnPosition, Quaternion.identity);

        Reward rewardComponent = spawnedReward.GetComponent<Reward>();
        if (rewardComponent != null)
        {
            rewardComponent.SetRewardParameters(blockIndex, trialIndex, pressesRequired);
        }
        else
        {
            Debug.LogError("Reward component not found on spawned reward!");
        }

        Debug.Log($"Spawned reward at position: {spawnPosition}");
        return spawnedReward;
    }

    // Clear all rewards from the scene
    public void ClearReward()
    {
        foreach (GameObject reward in GameObject.FindGameObjectsWithTag("Reward"))
        {
            gridManager.ReleasePosition(reward.transform.position);
            Destroy(reward);
        }
    }
}