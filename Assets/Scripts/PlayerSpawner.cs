using UnityEngine;
using System.Collections;

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
    /// <returns>The spawned player GameObject, or null if spawning failed.</returns>
    public GameObject SpawnPlayer()
    {
        Debug.Log("Attempting to spawn player...");

        if (playerPrefab == null)
        {
            Debug.LogError("Cannot spawn player: Player prefab is not assigned.");
            return null;
        }

        Vector2 spawnPosition = GetRandomSpawnPosition();
        Vector3 spawnPosition3D = new Vector3(spawnPosition.x, spawnPosition.y, 0f);
        GameObject spawnedPlayer = Instantiate(playerPrefab, spawnPosition3D, Quaternion.identity);

        if (spawnedPlayer == null)
        {
            Debug.LogError("CRITICAL: Player instantiation failed!");
            return null;
        }

        PlayerController controller = spawnedPlayer.GetComponent<PlayerController>();
        if (controller != null)
        {
            // Give a small delay to ensure all components are properly initialized
            StartCoroutine(EnablePlayerMovementAfterDelay(controller));
        }
        else
        {
            Debug.LogError("PlayerController component not found on spawned player!");
        }

        return spawnedPlayer;
    }

    private IEnumerator EnablePlayerMovementAfterDelay(PlayerController controller)
    {
        // Wait for one frame to ensure all components are initialized
        yield return null;

        Debug.Log("Updating presses per step before enabling movement");
        controller.UpdatePressesPerStep(); // Ensure pressesPerStep is set correctly

        Debug.Log("Enabling player movement after initialization");
        controller.EnableMovement();
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