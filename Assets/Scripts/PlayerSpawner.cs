using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GridManager gridManager;

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

    // 添加这个方法来获取随机位置
    public Vector2 GetRandomSpawnPosition()
    {
        return gridManager.GetRandomAvailablePosition();
    }

    public void DespawnPlayer(GameObject player)
    {
        if (player != null)
        {
            gridManager.ReleasePosition(player.transform.position);
            Destroy(player);
        }
    }
}