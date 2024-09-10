using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    /// <summary>
    /// Spawns players using positions from GridManager.
    /// </summary>
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GridManager gridManager;

    // Spawn the player at the given position
    public GameObject SpawnPlayer(Vector2 playerPosition)
    {
        if (playerPrefab == null)
        {
            Debug.LogError("Player prefab is not assigned in PlayerSpawner.");
            return null;
        }

        GameObject spawnedPlayer = Instantiate(playerPrefab, playerPosition, Quaternion.identity);
        Debug.Log($"Player spawned at position: {playerPosition}");

        PlayerController controller = spawnedPlayer.GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.EnableMovement();
            Debug.Log("PlayerController found and movement enabled.");
        }
        else
        {
            Debug.LogError("PlayerController not found on spawned player!");
        }

        return spawnedPlayer;
    }

    // Get a random spawn position from the grid manager
    public Vector2 GetRandomSpawnPosition()
    {
        return gridManager.GetRandomAvailablePosition();
    }

    // Despawn the player
    public void DespawnPlayer(GameObject player)
    {
        if (player != null)
        {
            gridManager.ReleasePosition(player.transform.position);
            Destroy(player);
        }
    }
}