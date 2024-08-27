using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GridManager gridManager;

    // Spawn the player at the given position
    public GameObject SpawnPlayer(Vector2 position)
    {
        if (playerPrefab == null)
        {
            Debug.LogError("Player prefab is not assigned in PlayerSpawner.");
            return null;
        }

        GameObject spawnedPlayer = Instantiate(playerPrefab, position, Quaternion.identity);
        Debug.Log($"Player spawned at position: {position}");
    
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