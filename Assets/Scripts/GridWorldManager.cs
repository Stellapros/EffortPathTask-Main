using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridWorldManager : MonoBehaviour
{
    public static GridWorldManager Instance { get; private set; }
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject rewardPrefab;

    private void Awake()
    {
        Instance = this;
    }

    public void StartTrial(Vector2 playerPosition, Vector2 rewardPosition, float rewardValue)
    {
        // Use the received data to spawn the player and reward in the "GridWorld" scene
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
            // Set the reward value on the spawned reward object
        }
        else
        {
            Debug.LogError("RewardPrefab is not assigned in the GridWorldManager!");
        }
    }
}