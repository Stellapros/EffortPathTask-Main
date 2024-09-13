using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages the overall state and flow of the GridWorld game.
/// This script should be attached to a persistent GameObject that exists across all scenes.
/// </summary>
public class GridWorldManager : MonoBehaviour
{
    // Singleton instance
    public static GridWorldManager Instance { get; private set; }
    // Event that can be subscribed to for trial end notifications
    public event System.Action OnTrialEnded;

    [SerializeField] private GridManager gridManager;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject rewardPrefab;
    [SerializeField] private float defaultTrialDuration = 10f;
    private GameObject currentPlayer;
    private GameObject currentReward;

    private List<GameObject> currentRewards = new List<GameObject>();

    // Add references to ScoreManager and CountdownTimer
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private CountdownTimer countdownTimer;

    private void Awake()
    {
        // Singleton pattern implementation
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Find or assign necessary components
        if (gridManager == null)
        {
            gridManager = FindObjectOfType<GridManager>();
            if (gridManager == null)
            {
                Debug.LogError("GridManager not found in the scene!");
            }
        }

        // Initialize ScoreManager and CountdownTimer if they're not set
        if (scoreManager == null)
        {
            scoreManager = GetComponentInChildren<ScoreManager>();
            if (scoreManager == null)
            {
                Debug.LogError("ScoreManager not found! Please add it as a child of GridWorldManager.");
            }
        }

        if (countdownTimer == null)
        {
            countdownTimer = GetComponentInChildren<CountdownTimer>();
            if (countdownTimer == null)
            {
                Debug.LogError("CountdownTimer not found! Please add it as a child of GridWorldManager.");
            }
        }
    }

    /// <summary>
    /// Initializes the GridWorld game. This should be called when the GridWorld scene is loaded.
    /// </summary>
    /// <param name="trialDuration">The duration of the trial in seconds. If not provided, uses the default duration.</param>
    public void InitializeGridWorld(float trialDuration = -1f)
    {
        if (gridManager == null)
        {
            Debug.LogError("GridManager is not set. Cannot initialize GridWorld.");
            return;
        }

        if (trialDuration < 0)
        {
            trialDuration = defaultTrialDuration;
        }

        // Reset the game state
        EndTrial();

        // Start a new trial with random positions
        Vector2 playerPosition = gridManager.GetRandomAvailablePosition();
        Vector2 rewardPosition = gridManager.GetRandomAvailablePosition();
        while (rewardPosition == playerPosition)
        {
            rewardPosition = gridManager.GetRandomAvailablePosition();
        }

        StartTrial(playerPosition, rewardPosition, 10f); // 10f is a default reward value

        // Start the countdown timer
        if (countdownTimer != null)
        {
            countdownTimer.StartTimer(trialDuration);
        }
        else
        {
            Debug.LogWarning("CountdownTimer is not set. Timer will not start.");
        }

        // Reset the score
        if (scoreManager != null)
        {
            scoreManager.ResetScore();
        }
        else
        {
            Debug.LogWarning("ScoreManager is not set. Score will not be tracked.");
        }
    }

    /// <summary>
    /// Starts a new trial by spawning the player and reward at specified positions.
    /// </summary>
    /// <param name="playerPosition">The position to spawn the player.</param>
    /// <param name="rewardPosition">The position to spawn the reward.</param>
    /// <param name="rewardValue">The value of the reward.</param>
    public void StartTrial(Vector2 playerPosition, Vector2 rewardPosition, float rewardValue)
    {
        if (!gridManager.IsValidPosition(playerPosition) || !gridManager.IsValidPosition(rewardPosition))
        {
            Debug.LogError("Invalid player or reward position!");
            return;
        }

        EndTrial(); // Clean up any existing trial objects

        SpawnPlayer(playerPosition);
        SpawnReward(rewardPosition, rewardValue);
    }

    /// <summary>
    /// Ends the current trial and cleans up spawned objects.
    /// </summary>
    public void EndTrial()
    {
        if (currentPlayer != null)
        {
            Destroy(currentPlayer);
            currentPlayer = null;
        }

        foreach (GameObject reward in currentRewards)
        {
            if (reward != null)
            {
                Destroy(reward);
            }
        }
        currentRewards.Clear();

        // Stop the countdown timer
        if (countdownTimer != null)
        {
            countdownTimer.StopTimer();
        }

        // Notify any listeners that the trial has ended
        OnTrialEnded?.Invoke();
    }

    public void SpawnPlayer(Vector2 position)
    {
        if (playerPrefab != null)
        {
            currentPlayer = Instantiate(playerPrefab, new Vector3(position.x, position.y, 0f), Quaternion.identity);
            PlayerController playerController = currentPlayer.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.EnableMovement();
            }
            else
            {
                Debug.LogError("PlayerController component not found on spawned player!");
            }
        }
        else
        {
            Debug.LogError("PlayerPrefab is not assigned in the GridWorldManager!");
        }
    }

    public void SpawnReward(Vector2 position, float value)
    {
        if (rewardPrefab != null)
        {
            GameObject rewardObject = Instantiate(rewardPrefab, new Vector3(position.x, position.y, 0f), Quaternion.identity);
            currentRewards.Add(rewardObject);

            Reward rewardComponent = rewardObject.GetComponent<Reward>();
            if (rewardComponent != null)
            {
                rewardComponent.SetValue(value);
                rewardComponent.SetRewardParameters(0, 0, 1); // Default parameters, adjust as needed
            }
            else
            {
                Debug.LogError("Reward component not found on rewardPrefab!");
            }
        }
        else
        {
            Debug.LogError("RewardPrefab is not assigned in the GridWorldManager!");
        }
    }

    private void Update()
    {
        // Check for collision between player and reward
        if (currentPlayer != null && currentReward != null)
        {
            if (Vector3.Distance(currentPlayer.transform.position, currentReward.transform.position) < 0.5f)
            {
                HandleRewardCollection();
            }
        }
    }

    public void HandleRewardCollection()
    {
        if (scoreManager != null)
            scoreManager.AddScore(1);

        // TODO: Update score
        Debug.Log("Reward collected!");

        // Destroy current player and reward
        Destroy(currentPlayer);
        Destroy(currentReward);
        EndTrial();

        // Notify GameController that the trial is complete
        // You might want to create a method in GameController to handle this
        // GameController.Instance.EndGridWorldTrial(true);
    }

    public Vector2 GetRandomEmptyPosition()
    {
        return gridManager.GetRandomAvailablePosition();
    }
}