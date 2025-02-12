using UnityEngine;

/// <summary>
/// Handles collision detection for the player, particularly with rewards.
/// </summary>
public class DetectCollisions : MonoBehaviour
{
    [SerializeField] private GameController gameController;
    [SerializeField] private GridWorldManager gridWorldManager;
    private PlayerController playerController;
    private RewardSpawner rewardSpawner;
    private bool hasCollectedReward = false;

    private void Start()
    {
        // Find necessary components if not assigned
        if (gameController == null)
            gameController = FindAnyObjectByType<GameController>();

        if (gridWorldManager == null)
            gridWorldManager = FindAnyObjectByType<GridWorldManager>();

        rewardSpawner = FindAnyObjectByType<RewardSpawner>(); // Find RewardSpawner
        if (rewardSpawner == null)
            Debug.LogWarning("RewardSpawner not found in the scene!");

        playerController = GetComponent<PlayerController>();
        if (playerController == null)
            Debug.LogError("PlayerController not found on this GameObject!");

        // Log warnings for missing components
        if (gameController == null)
            Debug.LogWarning("GameController not found in the scene!");
        if (gridWorldManager == null)
            Debug.LogWarning("GridWorldManager not found in the scene!");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!hasCollectedReward && other.CompareTag("Reward") && other.TryGetComponent<Reward>(out var reward))
        {
            HandleRewardCollision(reward, other.gameObject);
        }
    }

    /// <summary>
    /// Handles the collision with a reward object.
    /// </summary>
    /// <param name="reward">The Reward component of the collided object.</param>
    /// <param name="rewardObject">The GameObject of the reward.</param>
    private void HandleRewardCollision(Reward reward, GameObject rewardObject)
    {
        hasCollectedReward = true;
        gameController.RewardCollected(true);

        // Use RewardSpawner to clear the reward properly
        if (rewardSpawner != null)
        {
            rewardSpawner.ClearReward();
        }
        else
        {
            Debug.LogWarning("RewardSpawner not found, destroying reward directly");
            Destroy(rewardObject);
        }
    }

}