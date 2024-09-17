using UnityEngine;
using System.Collections;

/// <summary>
/// Manages the overall state and flow of the GridWorld game.
/// This script should be attached to a persistent GameObject that exists across all scenes.
/// </summary>
public class GridWorldManager : MonoBehaviour
{
    // Singleton instance
    public static GridWorldManager Instance { get; private set; }

    // Event that can be subscribed to for trial end notifications
    public event System.Action<bool> OnTrialEnded;

    [SerializeField] private GridManager gridManager;
    [SerializeField] private PlayerSpawner playerSpawner;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private RewardSpawner rewardSpawner;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject rewardPrefab;
    [SerializeField] private float defaultTrialDuration = 10f;

    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private CountdownTimer countdownTimer;

    private bool isTrialActive = false;
    private GameController gameController;

    private void Awake()
    {
        SetupSingleton();
        FindComponents();
    }

    /// <summary>
    /// Sets up the singleton instance of the GridWorldManager.
    /// </summary>
    private void SetupSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Finds or assigns necessary components.
    /// </summary>
    private void FindComponents()
    {
        gridManager = gridManager ?? FindObjectOfType<GridManager>();
        playerSpawner = playerSpawner ?? FindObjectOfType<PlayerSpawner>();
        rewardSpawner = rewardSpawner ?? FindObjectOfType<RewardSpawner>();
        scoreManager = scoreManager ?? FindObjectOfType<ScoreManager>();
        countdownTimer = countdownTimer ?? FindObjectOfType<CountdownTimer>();
        gameController = GameController.Instance;

        // Log warnings for missing components
        if (gridManager == null) Debug.LogWarning("GridManager not found in the scene!");
        if (playerSpawner == null) Debug.LogWarning("PlayerSpawner not found in the scene!");
        if (rewardSpawner == null) Debug.LogWarning("RewardSpawner not found in the scene!");
        if (scoreManager == null) Debug.LogWarning("ScoreManager not found in the scene!");
        if (countdownTimer == null) Debug.LogWarning("CountdownTimer not found in the scene!");
        if (gameController == null) Debug.LogWarning("GameController not found!");
    }

    /// <summary>
    /// Initializes the GridWorld game. This should be called when the GridWorld scene is loaded.
    /// </summary>
    /// <param name="trialDuration">The duration of the trial in seconds. If not provided, uses the default duration.</param>
    public void InitializeGridWorld(float trialDuration = -1f)
    {
        if (gridManager == null || playerSpawner == null || rewardSpawner == null || gameController == null)
        {
            Debug.LogError("Essential components are missing. Cannot initialize GridWorld.");
            return;
        }

        if (trialDuration < 0)
        {
            trialDuration = defaultTrialDuration;
        }

        // Reset the game state
        EndTrial(false);

        // Start a new trial
        gameController.StartTrial();

        // Start the countdown timer
        if (countdownTimer != null)
        {
            countdownTimer.StartTimer(trialDuration);
            countdownTimer.OnTimerExpired += EndTrialOnTimeUp; // Add this line
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

        isTrialActive = true;
    }

    private void EndTrialOnTimeUp()
    {
        if (isTrialActive)
        {
            EndTrial(false);
        }
    }

    /// <summary>
    /// Ends the current trial and cleans up spawned objects.
    /// </summary>
    /// <param name="rewardCollected">Whether the reward was collected in this trial.</param>
    public void EndTrial(bool rewardCollected)
    {
        if (!isTrialActive) return;

        isTrialActive = false;

        // Stop the countdown timer
        if (countdownTimer != null)
        {
            countdownTimer.StopTimer();
            countdownTimer.OnTimerExpired -= EndTrialOnTimeUp; // Remove the event subscription
        }

        // Notify any listeners that the trial has ended
        OnTrialEnded?.Invoke(rewardCollected);

        // Let GameController handle the trial end
        gameController.EndTrial(rewardCollected);
        playerController.DisableMovement();
    }

    /// <summary>
    /// Handles the collection of a reward by the player.
    /// </summary>
    // public void HandleRewardCollection()
    // {
    //     if (!isTrialActive) return;

    //     // if (scoreManager != null)
    //     //     scoreManager.AddScore(1);

    //     Debug.Log("Reward collected!");

    //     // EndTrial(true);
    // }

    /// <summary>
    /// Gets a random empty position on the grid.
    /// </summary>
    /// <returns>A random available position on the grid.</returns>
    public Vector2 GetRandomEmptyPosition()
    {
        return gridManager.GetRandomAvailablePosition();
    }
}