using UnityEngine;
using System.Collections.Generic;

public class GridWorldManager : MonoBehaviour
{
    public static GridWorldManager Instance { get; private set; }

    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject rewardPrefab;
    [SerializeField] private GridManager gridManager;

    private GameObject currentPlayer;
    private List<GameObject> currentRewards = new List<GameObject>();

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

        if (gridManager == null)
        {
            gridManager = FindObjectOfType<GridManager>();
            if (gridManager == null)
            {
                Debug.LogError("GridManager not found in the scene!");
            }
        }
    }

    /// <summary>
    /// Starts a new trial by spawning the player and reward at specified positions.
    /// </summary>
    /// <param name="playerPosition">The position to spawn the player.</param>
    /// <param name="rewardPosition">The position to spawn the reward.</param>
    /// <param name="rewardValue">The value of the reward.</param>
    public void StartTrial(Vector2 playerPosition, Vector2 rewardPosition, float rewardValue)
    {
        if (!gridManager.IsValidPosition(playerPosition) || !gridManager.IsValidPosition(rewardPosition))
        {
            Debug.LogError("Invalid player or reward position!");
            return;
        }

        EndTrial(); // Clean up any existing trial objects

        SpawnPlayer(playerPosition);
        SpawnReward(rewardPosition, rewardValue);

    }

    /// <summary>
    /// Ends the current trial and cleans up spawned objects.
    /// </summary>
    public void EndTrial()
    {
        if (currentPlayer != null)
        {
            Destroy(currentPlayer);
            currentPlayer = null;
        }

        foreach (GameObject reward in currentRewards)
        {
            if (reward != null)
            {
                Destroy(reward);
            }
        }
        currentRewards.Clear();

        // Additional cleanup logic if needed
    }

    private void SpawnPlayer(Vector2 position)
    {
        if (playerPrefab != null)
        {
            currentPlayer = Instantiate(playerPrefab, new Vector3(position.x, position.y, 0f), Quaternion.identity);
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
            currentRewards.Add(rewardObject);

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
/// <summary>
/// Ends the current trial and cleans up