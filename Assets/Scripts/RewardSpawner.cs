using UnityEngine;

public class RewardSpawner : MonoBehaviour
{
    [SerializeField] private GameObject rewardPrefab;
    [SerializeField] private GridManager gridManager;

    public GameObject SpawnReward(int blockIndex, int trialIndex, int pressesRequired)
    {
        Vector2 spawnPosition = gridManager.GetRandomAvailablePosition();
        GameObject spawnedReward = Instantiate(rewardPrefab, spawnPosition, Quaternion.identity);

        Reward rewardComponent = spawnedReward.GetComponent<Reward>();
        if (rewardComponent != null)
        {
            rewardComponent.SetRewardParameters(blockIndex, trialIndex, pressesRequired);
        }

        Debug.Log($"Spawned reward at position: {spawnPosition}");
        return spawnedReward;
    }

    public void ClearReward()
    {
        foreach (GameObject reward in GameObject.FindGameObjectsWithTag("Reward"))
        {
            gridManager.ReleasePosition(reward.transform.position);
            Destroy(reward);
        }
    }
}