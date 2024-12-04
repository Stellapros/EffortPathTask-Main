using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages the spawning and clearing of rewards in the GridWorld scene.
/// </summary>
public class RewardSpawner : MonoBehaviour
{
    // [SerializeField] private GameObject rewardPrefab;
    // [SerializeField] private List<GameObject> rewardPrefabs; // List of 6 different reward prefabs
    [SerializeField] private List<GameObject> formalRewardPrefabs; // Formal experiment reward prefabs
    [SerializeField] private List<GameObject> practiceRewardPrefabs; // Practice trial reward prefabs
    private Sprite currentRewardSprite;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private ExperimentManager experimentManager;
    [SerializeField] private PracticeManager practiceManager;
    [SerializeField] private PlayerSpawner playerSpawner;
    private GameObject currentReward;

    // Always use exactly 5 cells distance
    // Player (Start)  1st Cell  2nd Cell  3rd Cell  4th Cell  5th Cell  Reward (End)
    // 0             1         2         3         4         5         6
    private const int FIXED_DISTANCE = 6; // creating a path that crosses 5 intermediate cells

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

        if (practiceManager == null)
        {
            practiceManager = FindAnyObjectByType<PracticeManager>();
            if (practiceManager == null)
            {
                Debug.LogWarning("PracticeManager not found in the scene.");
            }
        }

        ValidateRewardPrefabs();
    }

    private void ValidateRewardPrefabs()
    {
        if (formalRewardPrefabs == null || formalRewardPrefabs.Count == 0)
        {
            Debug.LogError("Formal reward prefabs are not assigned in RewardSpawner. Please assign them in the inspector.");
        }

        if (practiceRewardPrefabs == null || practiceRewardPrefabs.Count == 0)
        {
            Debug.LogError("Practice reward prefabs are not assigned in RewardSpawner. Please assign them in the inspector.");
        }
    }

    public void SetRewardSprite(Sprite sprite)
    {
        if (sprite == null)
        {
            // Try to retrieve sprite from PlayerPrefs if null
            string spriteName = PlayerPrefs.GetString("CurrentRewardSpriteName", "");
            if (!string.IsNullOrEmpty(spriteName))
            {
                // Load sprite by name from resources or find in scene
                sprite = Resources.Load<Sprite>(spriteName);
            }
        }

        if (sprite == null)
        {
            Debug.LogError("Attempting to set null sprite in RewardSpawner");
            return;
        }

        currentRewardSprite = sprite;
        Debug.Log($"Setting reward sprite: {sprite.name}");

        // If there's already a reward, update its sprite
        if (currentReward != null)
        {
            SpriteRenderer spriteRenderer = currentReward.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = currentRewardSprite;
                spriteRenderer.sortingOrder = 5;  // Ensure it's visible above grid
                spriteRenderer.enabled = true;
                Debug.Log($"Setting reward sprite: {currentRewardSprite.name}");
                Debug.Log($"Sprite details - Width: {currentRewardSprite.rect.width}, Height: {currentRewardSprite.rect.height}");
                Debug.Log($"Updated existing reward sprite to: {currentRewardSprite.name}");
            }
        }
    }

    /// <summary>
    /// Spawns a reward at the specified position with the given parameters.
    /// </summary>
    public GameObject SpawnReward(Vector2 playerPosition, int blockIndex, int trialIndex, int pressesRequired, int scoreValue)
    {
        // Add more detailed logging
        Debug.Log($"SpawnReward - Input Player Position: {playerPosition}");

        // Retrieve sprite from PlayerPrefs if not already set
        if (currentRewardSprite == null)
        {
            string spriteName = PlayerPrefs.GetString("CurrentRewardSpriteName", "");
            if (!string.IsNullOrEmpty(spriteName))
            {
                // Load sprite by name from resources or find in scene
                currentRewardSprite = Resources.Load<Sprite>(spriteName);
            }
        }
        if (currentReward != null)
        {
            Debug.LogWarning("Attempting to spawn a reward when one already exists. Clearing existing reward first.");
            ClearReward();
        }

        if (gridManager == null)
        {
            Debug.LogError("Cannot spawn reward: GridManager is missing.");
            return null;
        }

        // Determine which reward prefabs to use based on trial type
        List<GameObject> currentRewardPrefabs = practiceManager.IsPracticeTrial()
            ? practiceRewardPrefabs
            : formalRewardPrefabs;

        if (currentRewardPrefabs == null || currentRewardPrefabs.Count == 0)
        {
            Debug.LogError("No reward prefabs available for current trial type.");
            return null;
        }

        // Special handling for practice trials to use specific sprites
        if (practiceManager.IsPracticeTrial())
        {
            Sprite practiceTrialSprite = practiceManager.GetCurrentPracticeTrialSprite();
            if (practiceTrialSprite != null)
            {
                currentRewardSprite = practiceTrialSprite;
                Debug.Log($"Using practice trial sprite: {practiceTrialSprite.name}");
            }
        }

        // First, try to find the player GameObject
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            // Convert player's transform position to Vector2
            playerPosition = new Vector2(player.transform.position.x, player.transform.position.y);
            Debug.Log($"Player found directly - Updated Player Position: {playerPosition}");
        }
        else
        {
            // If no player found, attempt to spawn a new player
            GameObject spawnedPlayer = playerSpawner.SpawnPlayer();
            if (spawnedPlayer != null)
            {
                playerPosition = new Vector2(
                    spawnedPlayer.transform.position.x,
                    spawnedPlayer.transform.position.y
                );
                Debug.Log($"Player spawned - New Player Position: {playerPosition}");
            }
            else
            {
                Debug.LogError("CRITICAL: Cannot determine player position for reward spawning!");
                return null;
            }
        }

        // Log the final player position before spawning reward
        Debug.Log($"Final Player Position before Reward Spawn: {playerPosition}");

        // Rest of the SpawnReward method remains the same...
        Vector2 rewardPosition = GetSpawnPositionAtDistance(playerPosition, FIXED_DISTANCE);

        Debug.Log($"Seting ====== Spawn Details: " +
          $"Player Position: {playerPosition}, " +
          $"Reward Position: {rewardPosition}, " +
          $"Distance Between: {Vector2.Distance(playerPosition, rewardPosition)}");

        // Validate the spawn position
        if (rewardPosition == Vector2.zero)
        {
            Debug.LogError("Failed to get valid spawn position for reward");
            return null;
        }

        // Select a random reward prefab from the current pool
        // GameObject rewardPrefab = GetRandomRewardPrefab(currentRewardPrefabs);
        GameObject rewardPrefab = GetRewardPrefabBySprite(currentRewardPrefabs, PlayerPrefs.GetString("CurrentRewardSpriteName", ""));

        Vector3 spawnPosition = new Vector3(rewardPosition.x, rewardPosition.y, 0f);

        Debug.Log($"Newwwwww Spawn Details: " +
          $"Player Position: {playerPosition}, " +
          $"Reward Position: {spawnPosition}, " +
          $"Distance Between: {Vector2.Distance(playerPosition, spawnPosition)}");

        currentReward = Instantiate(rewardPrefab, spawnPosition, Quaternion.identity);

        if (currentReward == null)
        {
            Debug.LogError("Failed to instantiate reward prefab.");
            return null;
        }

        // Set up reward parameters
        Reward rewardComponent = currentReward.GetComponent<Reward>();
        if (rewardComponent != null)
        {
            rewardComponent.SetRewardParameters(blockIndex, trialIndex, pressesRequired, scoreValue);
            Debug.Log($"Reward spawned at {spawnPosition}, Distance: {Vector2.Distance(playerPosition, rewardPosition)} cells");

            // Apply the current sprite (from DecisionManager)
            if (currentRewardSprite != null)
            {
                SpriteRenderer spriteRenderer = currentReward.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    Debug.Log($"Setting reward sprite: {currentRewardSprite.name}");
                    Debug.Log($"Sprite details - Width: {currentRewardSprite.rect.width}, Height: {currentRewardSprite.rect.height}");
                    spriteRenderer.sprite = currentRewardSprite;
                    spriteRenderer.sortingOrder = 5;
                }
            }
            return currentReward;
        }

        Debug.LogError("Reward component missing!");
        Destroy(currentReward);
        return null;
    }

    // New method to find the correct reward prefab
    private GameObject GetRewardPrefabBySprite(List<GameObject> prefabs, string spriteName)
    {
        foreach (GameObject prefab in prefabs)
        {
            SpriteRenderer spriteRenderer = prefab.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && spriteRenderer.sprite.name == spriteName)
            {
                return prefab;
            }
        }

        Debug.LogError($"No reward prefab found matching sprite: {spriteName}");
        return prefabs[0]; // Fallback to first prefab if no match
    }

    // private GameObject GetRandomRewardPrefab(List<GameObject> rewardPrefabPool)
    // {
    //     if (rewardPrefabPool == null || rewardPrefabPool.Count == 0)
    //     {
    //         Debug.LogError("No reward prefabs available in the pool.");
    //         return null;
    //     }

    //     int randomIndex = Random.Range(0, rewardPrefabPool.Count);
    //     GameObject selectedPrefab = rewardPrefabPool[randomIndex];

    //     Debug.Log($"Selected Reward Prefab: {selectedPrefab?.name ?? "NULL"} at index {randomIndex}");

    //     return selectedPrefab;
    // }


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

    public Vector2 GetSpawnPositionAtDistance(Vector2 playerPosition, float distance)
    {
        if (gridManager == null)
        {
            Debug.LogError("GridManager not set in RewardSpawner!");
            return Vector2.zero;
        }

        // Ensure exactly 5 grid cells away
        Vector2 rewardPosition = gridManager.GetPositionAtDistance(playerPosition, FIXED_DISTANCE);

        Debug.Log($"Spawn Position Details: " +
                  $"Player Position: {playerPosition}, " +
                  $"Reward Position: {rewardPosition}, " +
                  $"Distance: {Vector2.Distance(playerPosition, rewardPosition)} cells");

        return rewardPosition;
    }

    // public Vector2 GetSpawnPositionAtDistance(Vector2 playerPosition, float distance)
    // {
    //     if (gridManager == null)
    //     {
    //         Debug.LogError("GridManager not set in RewardSpawner!");
    //         return Vector2.zero;
    //     }

    //     // Extensive logging for input player position
    //     Debug.Log($"GetSpawnPositionAtDistance - Initial Input Player Position: {playerPosition}");

    //     // Ensure input is valid
    //     Vector2 validatedPlayerPosition = playerPosition;
    //     if (validatedPlayerPosition == Vector2.zero)
    //     {
    //         GameObject player = GameObject.FindGameObjectWithTag("Player");
    //         if (player != null)
    //         {
    //             // Explicitly convert to Vector2
    //             validatedPlayerPosition = new Vector2(
    //                 player.transform.position.x,
    //                 player.transform.position.y
    //             );
    //             Debug.Log($"Corrected Player Position from Player object: {validatedPlayerPosition}");
    //             Debug.Log($"Original Player Transform Position: {player.transform.position}");
    //         }
    //         else
    //         {
    //             Debug.LogError("Cannot determine player position for reward spawning!");
    //             return Vector2.zero;
    //         }
    //     }

    //     // Get position at the specified distance
    //     Vector2 rewardPosition = gridManager.GetPositionAtDistance(validatedPlayerPosition, FIXED_DISTANCE);

    //     // Validate exact distance
    //     float actualDistance = Vector2.Distance(validatedPlayerPosition, rewardPosition);

    //     Debug.Log($"Spawn Position Validation: " +
    //               $"Validated Player Position: {validatedPlayerPosition}, " +
    //               $"Reward Position: {rewardPosition}, " +
    //               $"Actual Distance: {actualDistance} cells, " +
    //               $"Target Distance: {FIXED_DISTANCE} cells");

    //     if (Mathf.Abs(actualDistance - FIXED_DISTANCE) > 0.5f)
    //     {
    //         Debug.LogError($"CRITICAL: Reward spawn position is not exactly {FIXED_DISTANCE} cells away!" +
    //                        $" Player Position: {validatedPlayerPosition}, " +
    //                        $"Reward Position: {rewardPosition}, " +
    //                        $"Actual Distance: {actualDistance} cells");

    //         // Force correction
    //         rewardPosition = validatedPlayerPosition + (rewardPosition - validatedPlayerPosition).normalized * FIXED_DISTANCE;
    //     }

    //     return rewardPosition;
    // }
    // public Vector2 GetSpawnPositionAtDistance(Vector2 playerPosition, float distance)
    // {
    //     if (gridManager == null)
    //     {
    //         Debug.LogError("GridManager not set in RewardSpawner!");
    //         return Vector2.zero;
    //     }

    //     // Ensure input is valid
    //     Vector2 validatedPlayerPosition = playerPosition;
    //     if (validatedPlayerPosition == Vector2.zero)
    //     {
    //         GameObject player = GameObject.FindGameObjectWithTag("Player");
    //         if (player != null)
    //         {
    //             validatedPlayerPosition = new Vector2(
    //                 player.transform.position.x,
    //                 player.transform.position.y
    //             );
    //         }
    //         else
    //         {
    //             Debug.LogError("Cannot determine player position for reward spawning!");
    //             return Vector2.zero;
    //         }
    //     }

    //     // Possible directions to explore (including diagonals)
    //     Vector2[] directions = new Vector2[]
    //     {
    //         Vector2.right,    // Right
    //         Vector2.left,     // Left
    //         Vector2.up,       // Up
    //         Vector2.down,     // Down
    //         new Vector2(1, 1).normalized,    // Up-Right diagonal
    //         new Vector2(1, -1).normalized,   // Down-Right diagonal
    //         new Vector2(-1, 1).normalized,   // Up-Left diagonal
    //         new Vector2(-1, -1).normalized   // Down-Left diagonal
    //     };

    //     List<Vector2> validPositions = new List<Vector2>();

    //     // Try each direction to find valid positions exactly 5 cells away
    //     foreach (Vector2 direction in directions)
    //     {
    //         // Calculate the exact position based on the integer cell grid
    //         Vector2 potentialRewardPosition = new Vector2(
    //             Mathf.RoundToInt(validatedPlayerPosition.x + (direction.x * distance)),
    //             Mathf.RoundToInt(validatedPlayerPosition.y + (direction.y * distance))
    //         );

    //         // Convert to Vector2Int for grid position validation
    //         Vector2Int gridPos = new Vector2Int(
    //             Mathf.RoundToInt(potentialRewardPosition.x), 
    //             Mathf.RoundToInt(potentialRewardPosition.y)
    //         );

    //         // Check if the potential position is within the grid
    //         if (gridManager.IsValidGridPosition(gridPos))
    //         {
    //             float actualDistance = Vector2.Distance(validatedPlayerPosition, potentialRewardPosition);

    //             Debug.Log($"Checking direction {direction}: " +
    //                       $"Player Position: {validatedPlayerPosition}, " +
    //                       $"Potential Reward Position: {potentialRewardPosition}, " +
    //                       $"Grid Position: {gridPos}, " +
    //                       $"Actual Distance: {actualDistance} cells");

    //             // Strict check for exact distance
    //             if (Mathf.Abs(actualDistance - distance) < 0.1f)
    //             {
    //                 validPositions.Add(potentialRewardPosition);
    //             }
    //         }
    //     }

    //     // If no valid positions found, fallback to finding closest valid position
    //     if (validPositions.Count == 0)
    //     {
    //         Vector2 bestPosition = Vector2.zero;
    //         float closestDistance = float.MaxValue;

    //         foreach (Vector2 direction in directions)
    //         {
    //             // Calculate the exact position based on the integer cell grid
    //             Vector2 potentialRewardPosition = new Vector2(
    //                 Mathf.RoundToInt(validatedPlayerPosition.x + (direction.x * distance)),
    //                 Mathf.RoundToInt(validatedPlayerPosition.y + (direction.y * distance))
    //             );

    //             // Convert to Vector2Int for grid position validation
    //             Vector2Int gridPos = new Vector2Int(
    //                 Mathf.RoundToInt(potentialRewardPosition.x), 
    //                 Mathf.RoundToInt(potentialRewardPosition.y)
    //             );

    //             if (gridManager.IsValidGridPosition(gridPos))
    //             {
    //                 float actualDistance = Vector2.Distance(validatedPlayerPosition, potentialRewardPosition);
    //                 float distanceDifference = Mathf.Abs(actualDistance - distance);

    //                 if (distanceDifference < closestDistance)
    //                 {
    //                     bestPosition = potentialRewardPosition;
    //                     closestDistance = distanceDifference;
    //                 }
    //             }
    //         }

    //         if (bestPosition != Vector2.zero)
    //         {
    //             Debug.LogWarning($"Fallback: Using closest valid position {bestPosition}");
    //             return bestPosition;
    //         }

    //         Debug.LogError($"CRITICAL: No valid position found at distance {distance}");
    //         return Vector2.zero;
    //     }

    //     // Randomly select from valid positions if multiple exist
    //     Vector2 selectedPosition = validPositions[Random.Range(0, validPositions.Count)];

    //     Debug.Log($"Final Spawn Position: " +
    //               $"Player Position: {validatedPlayerPosition}, " +
    //               $"Reward Position: {selectedPosition}, " +
    //               $"Distance: {Vector2.Distance(validatedPlayerPosition, selectedPosition)} cells");

    //     return selectedPosition;
    // }

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

    public void DisableRewardCollection()
    {
        if (currentReward != null)
        {
            // Disable the collider but keep sprite visible
            Collider2D collider = currentReward.GetComponent<Collider2D>();
            if (collider != null)
            {
                collider.enabled = false;
            }
        }
    }

    public List<GameObject> GetPracticeRewardPrefabs()
    {
        return practiceRewardPrefabs;
    }
}