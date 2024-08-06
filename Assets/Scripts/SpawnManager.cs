using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Numerics;

public class SpawnManager : MonoBehaviour
{

    public GameObject rewardPrefab;
    public int rewardCount;
    private float spawnRange;
    private string conditionName;
    public TextMeshProUGUI logText;
    private List<UnityEngine.Vector3> spawnPositions = new List<UnityEngine.Vector3>();
    


    // Start is called before the first frame update
    void Start()
    {

        // Randomly select one of the four conditions
        int condition = Random.Range(0, 4); // Generates a random number between 0 and 3

        // Apply the selected condition
        switch (condition)
        {
            case 0:
                SetCondition(5, 3.0f, "LowProfit LowEffort");
                break;
            case 1:
                SetCondition(5, 7.0f, "LowProfit HighEffort");
                break;
            case 2:
                SetCondition(10, 3.0f, "HighProfit LowEffort");
                break;
            case 3:
                SetCondition(10, 7.0f, "HighProfit HighEffort");
                break;
        }

        // Update the UI log
        if (logText != null)
        {
            logText.text = "Condition: " + conditionName + "\n";
        }

        // Log the selected condition
        Debug.Log("Selected Condition: " + conditionName);

        // Spawn rewards
        SpawnRewards();
    }



    void SetCondition(int count, float range, string name)
    {
        rewardCount = count;
        spawnRange = range;
        conditionName = name;

        Debug.Log("Condition set: " + conditionName + " - Reward Count = " + rewardCount + ", Spawn Range = " + spawnRange);

        // Update the UI log
        if (logText != null)
        {
            logText.text = "Condition: " + conditionName + "\n";
        }
    }


    void SpawnRewards()
    {
        // Clear previous spawn positions
        spawnPositions.Clear();

        // Spawn rewards based on rewardCount and spawnRange
        for (int i = 0; i < rewardCount; i++)
        {
            UnityEngine.Vector3 spawnPosition = GenerateSpawnPosition();
            Instantiate(rewardPrefab, spawnPosition, rewardPrefab.transform.rotation);
        }
    }

    private UnityEngine.Vector3 GenerateSpawnPosition()
    {
        UnityEngine.Vector3 randomPos;
        bool positionIsValid;

        do
        {
            // Generate a random position
            float spawnPosX = Random.Range(-spawnRange, spawnRange);
            float spawnPosZ = Random.Range(-spawnRange, spawnRange);

            randomPos = new UnityEngine.Vector3(spawnPosX, 0, spawnPosZ);

            // Check if the position is valid (not the center and not overlapping)
            positionIsValid = !IsPositionInvalid(randomPos);

        } while (!positionIsValid);

        // Add the valid position to the list
        spawnPositions.Add(randomPos);
        return randomPos;
    }

    private bool IsPositionInvalid(UnityEngine.Vector3 position)
    {
        // Check if the position is too close to the center (0, 0, 0)
        if (UnityEngine.Vector3.Distance(position, UnityEngine.Vector3.zero) < 1.0f)
        {
            return true;
        }

        // Check if the position overlaps with any existing spawn positions
        foreach (var pos in spawnPositions)
        {
            if (UnityEngine.Vector3.Distance(position, pos) < 1.0f)
            {
                return true;
            }
        }

        return false;
    }
}