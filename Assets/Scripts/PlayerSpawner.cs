using UnityEngine;

/// <summary>
/// Manages the spawning and despawning of the player in the GridWorld scene.
/// </summary>
public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GridManager gridManager;

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
            gridManager = FindAnyObjectByType<GridManager>();
            if (gridManager == null)
            {
                Debug.LogError("GridManager not found in the scene. Please ensure it exists in the scene.");
            }
        }

        if (playerPrefab == null)
        {
            Debug.LogError("Player prefab is not assigned in PlayerSpawner. Please assign it in the inspector.");
        }
    }

    /// <summary>
    /// Spawns the player at the given position.
    /// </summary>
    /// <param name="playerPosition">The position to spawn the player.</param>
    /// <returns>The spawned player GameObject, or null if spawning failed.</returns>
    // public GameObject SpawnPlayer(Vector2 playerPosition)
    // {
    //     if (PlayerController.Instance != null)
    //     {
    //         // If the player instance already exists, just move it to the new position
    //         PlayerController.Instance.transform.position = new Vector3(playerPosition.x, playerPosition.y, 0f);
    //         PlayerController.Instance.ResetPosition(playerPosition);
    //         PlayerController.Instance.EnableMovement();
    //         Debug.Log($"Existing player moved to position: {playerPosition}");
    //         return PlayerController.Instance.gameObject;
    //     }

    //     if (playerPrefab == null)
    //     {
    //         Debug.LogError("Cannot spawn player: Player prefab is not assigned.");
    //         return null;
    //     }

    //     // Convert Vector2 to Vector3 for instantiation
    //     Vector3 spawnPosition = new Vector3(playerPosition.x, playerPosition.y, 0f);
    //     GameObject spawnedPlayer = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);

    //     if (spawnedPlayer != null && gridManager != null)
    //     {
    //         gridManager.OccupyPosition(playerPosition);
    //     }
    //     // if (spawnedPlayer == null)
    //     // {
    //     //     Debug.LogError("Failed to instantiate player prefab.");
    //     //     return null;
    //     // }

    //     Debug.Log($"New player spawned at position: {spawnPosition}");

    //     PlayerController controller = spawnedPlayer.GetComponent<PlayerController>();
    //     if (controller != null)
    //     {
    //         controller.EnableMovement();
    //         Debug.Log("PlayerController found and movement enabled.");
    //     }
    //     else
    //     {
    //         Debug.LogError("PlayerController component not found on spawned player!");
    //     }

    //     return spawnedPlayer;
    // }

    public GameObject SpawnPlayer()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("Cannot spawn player: Player prefab is not assigned.");
            return null;
        }

        Vector2 spawnPosition = GetRandomSpawnPosition();
        Vector3 spawnPosition3D = new Vector3(spawnPosition.x, spawnPosition.y, 0f);
        GameObject spawnedPlayer = Instantiate(playerPrefab, spawnPosition3D, Quaternion.identity);

        if (spawnedPlayer != null)
        {
            PlayerController controller = spawnedPlayer.GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.EnableMovement();
            }
            else
            {
                Debug.LogError("PlayerController component not found on spawned player!");
            }
        }

        return spawnedPlayer;
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
    /// Despawns the player and releases the grid position.
    /// </summary>
    /// <param name="player">The player GameObject to despawn.</param>
    public void DespawnPlayer(GameObject player)
    {
        if (player == null)
        {
            Debug.LogWarning("Attempted to despawn a null player object.");
            return;
        }

        if (gridManager != null)
        {
            gridManager.ReleasePosition(player.transform.position);
        }
        else
        {
            Debug.LogWarning("GridManager is null. Unable to release grid position.");
        }

        player.SetActive(false);
        Debug.Log("Player despawned and disabled.");
    }
}