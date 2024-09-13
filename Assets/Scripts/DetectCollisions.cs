using UnityEngine;

public class DetectCollisions : MonoBehaviour
{
    [SerializeField] private GameController gameController;
    private PlayerController playerController;
    [SerializeField] private GridWorldManager gridWorldManager;

    private void Start()
    {
        if (gameController == null)
            gameController = FindObjectOfType<GameController>();

        if (gridWorldManager == null)
            gridWorldManager = FindObjectOfType<GridWorldManager>();

        playerController = GetComponent<PlayerController>();
        if (playerController == null)
            Debug.LogError("PlayerController not found on this GameObject!");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Reward") && other.TryGetComponent<Reward>(out var reward))
        {
            int buttonPresses = playerController.GetButtonPressCount();
            int pressesRequired = reward.GetPressesRequired();

            if (buttonPresses >= pressesRequired)
            {
                // Collect the reward
                gameController.RewardCollected();
                playerController.HandleRewardCollection();

                // Destroy the reward object - The DetectCollisions script is attached to the player object, not the reward
                Destroy(other.gameObject);

                // Instead of destroying the player, we might want to disable it or handle it differently
                // gameObject.SetActive(false);
                playerController.DisableMovement();

                Debug.Log($"Reward collected!");

                // End the trial
                EndTrial(true);
            }
            else
            {
                Debug.Log($"Not enough button presses. Required: {pressesRequired}, Current: {buttonPresses}");
            }
        }
    }
    private void EndTrial(bool rewardCollected)
    {
        // Update score if reward was collected
        if (rewardCollected)
        {
            // Assuming ScoreManager is a component of GameController
            gameController.GetComponent<ScoreManager>()?.AddScore(1);
        }

        // End the trial in GridWorldManager
        gridWorldManager.EndTrial();

        // Notify GameController that the trial has ended
        gameController.EndTrial(rewardCollected);
    }
}