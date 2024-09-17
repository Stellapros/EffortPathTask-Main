using UnityEngine;

/// <summary>
/// Handles collision detection for the player, particularly with rewards.
/// </summary>
public class DetectCollisions : MonoBehaviour
{
    [SerializeField] private GameController gameController;
    [SerializeField] private GridWorldManager gridWorldManager;
    private PlayerController playerController;

    private void Start()
    {
        // Find necessary components if not assigned
        if (gameController == null)
            gameController = FindObjectOfType<GameController>();

        if (gridWorldManager == null)
            gridWorldManager = FindObjectOfType<GridWorldManager>();

        playerController = GetComponent<PlayerController>();
        if (playerController == null)
            Debug.LogError("PlayerController not found on this GameObject!");

        // Log warnings for missing components
        if (gameController == null)
            Debug.LogWarning("GameController not found in the scene!");
        if (gridWorldManager == null)
            Debug.LogWarning("GridWorldManager not found in the scene!");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Reward") && other.TryGetComponent<Reward>(out var reward))
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
        // int buttonPresses = playerController.GetButtonPressCount();
        gameController.RewardCollected(true);
        // int pressesRequired = reward.GetPressesRequired();
    }


    /// <summary>
    /// Collects the reward and ends the trial.
    /// </summary>
    /// <param name="rewardObject">The GameObject of the reward to be collected.</param>
    // private void CollectReward(GameObject rewardObject)
    // {
    //     gameController.GetComponent<ScoreManager>()?.AddScore(1);
    //     gameController.RewardCollected();
    //     playerController.HandleRewardCollection(rewardObject);

    //     // Destroy only the reward object
    //     Destroy(rewardObject);

    //     // Disable player movement
    //     playerController.DisableMovement();
    //     Debug.Log("Reward collected!");

    //     // End the trial
    //     EndTrial(true);
    // }

    /// <summary>
    /// Ends the current trial and updates the game state.
    /// </summary>
    /// <param name="rewardCollected">Whether a reward was collected during the trial.</param>
    private void EndTrial(bool rewardCollected)
    {
        // // Update score if reward was collected
        // if (rewardCollected)
        // {
        //     // Assuming ScoreManager is a component of GameController
        //     gameController.GetComponent<ScoreManager>()?.AddScore(1);
        // }

        // End the trial in GridWorldManager
        gridWorldManager.EndTrial(rewardCollected);

        // Notify GameController that the trial has ended
        gameController.EndTrial(rewardCollected);
    }
}