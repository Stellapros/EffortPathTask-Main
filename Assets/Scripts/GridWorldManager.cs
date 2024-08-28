using UnityEngine;

public class GridWorldManager : MonoBehaviour
{
    public static GridWorldManager Instance { get; private set; }
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject rewardPrefab;
    [SerializeField] private GridManager gridManager;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartTrial(Vector2 playerPosition, Vector2 rewardPosition, float rewardValue)
    {
        SpawnPlayer(playerPosition);
        SpawnReward(rewardPosition, rewardValue);
        // Other trial setup logic
    }

    private void SpawnPlayer(Vector2 position)
    {
        if (playerPrefab != null)
        {
            Instantiate(playerPrefab, new Vector3(position.x, position.y, 0f), Quaternion.identity);
        }
        else
        {
            Debug.LogError("PlayerPrefab is not assigned in the GridWorldManager!");
        }
    }

    private void SpawnReward(Vector2 position, float value)
    {
        if (rewardPrefab != null)
        {
            GameObject rewardObject = Instantiate(rewardPrefab, new Vector3(position.x, position.y, 0f), Quaternion.identity);
            Reward rewardComponent = rewardObject.GetComponent<Reward>();
            if (rewardComponent != null)
            {
                rewardComponent.SetValue(value);
                // You may want to set these parameters based on your game logic
                rewardComponent.SetRewardParameters(0, 0, 1);
            }
            else
            {
                Debug.LogError("Reward component not found on rewardPrefab!");
            }
        }
        else
        {
            Debug.LogError("RewardPrefab is not assigned in the GridWorldManager!");
        }
    }
}