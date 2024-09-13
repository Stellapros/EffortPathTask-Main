using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

/// <summary>
/// Controls the gameplay mechanics for each trial in the GridWorld scene and manages the flow between DecisionPhase and GridWorld scenes.
/// </summary>
public class GameController : MonoBehaviour
{
    #region Serialized Fields
    [SerializeField] private float maxTrialDuration = 10f;
    [SerializeField] private float restDuration = 3f;
    [SerializeField] private int[] pressesPerEffortLevel = { 1, 2, 3 };
    #endregion

    #region Private Fields
    private PlayerSpawner playerSpawner;
    private RewardSpawner rewardSpawner;
    private GridManager gridManager;
    private EffortSpriteUI effortSpriteUI;
    private GameObject currentPlayer;
    private GameObject currentReward;
    private bool rewardCollected = false;
    private float trialStartTime;
    private int buttonPressCount;
    private bool isTrialActive = false;
    private Vector2 actualRewardPosition;
    private CountdownTimer countdownTimer;
    private ScoreManager scoreManager;
    private ExperimentManager experimentManager;
    private int currentEffortLevel = 0;
    #endregion

    #region Unity Lifecycle Methods
    private void Awake()
    {
        // Only initialize if we're in the GridWorld scene
        if (SceneManager.GetActiveScene().name == "GridWorld")
        {
            Initialize();
        }
        else
        {
            // Disable this component if we're not in the GridWorld scene
            enabled = false;
            Debug.LogWarning("GameController disabled: Not in GridWorld scene");
        }
    }

    private void Start()
    {
        if (enabled)
        {
            StartCoroutine(StartTrial());
        }
    }

    private void Update()
    {
        if (isTrialActive)
        {
            // Check if trial time is up
            if (Time.time - trialStartTime >= maxTrialDuration)
            {
                EndTrial(false);
            }
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (experimentManager != null)
        {
            experimentManager.OnTrialEnded -= OnTrialEnd;
        }
    }
    #endregion

    #region Initialization Methods
    /// <summary>
    /// Initializes the GameController, finding necessary components and subscribing to events.
    /// </summary>
    private void Initialize()
    {
        // Find all necessary components in the scene
        experimentManager = ExperimentManager.Instance;
        countdownTimer = FindObjectOfType<CountdownTimer>();
        scoreManager = FindObjectOfType<ScoreManager>();
        gridManager = FindObjectOfType<GridManager>();
        effortSpriteUI = FindObjectOfType<EffortSpriteUI>();
        playerSpawner = FindObjectOfType<PlayerSpawner>();
        rewardSpawner = FindObjectOfType<RewardSpawner>();

        // Validate that all components are found
        ValidateComponents();

        // Subscribe to events
        if (experimentManager != null)
        {
            experimentManager.OnTrialEnded += OnTrialEnd;
        }
    }

    /// <summary>
    /// Validates that all required components are assigned.
    /// </summary>
    private void ValidateComponents()
    {
        if (experimentManager == null) Debug.LogError("ExperimentManager not found!");
        if (countdownTimer == null) Debug.LogError("CountdownTimer not found in the scene!");
        if (scoreManager == null) Debug.LogError("ScoreManager not found in the scene!");
        if (gridManager == null) Debug.LogError("GridManager not found in the scene!");
        if (effortSpriteUI == null) Debug.LogError("EffortSpriteUI not found in the scene!");
        if (playerSpawner == null) Debug.LogError("PlayerSpawner not found in the scene!");
        if (rewardSpawner == null) Debug.LogError("RewardSpawner not found in the scene!");
    }
    #endregion

    #region Trial Control Methods
    /// <summary>
    /// Starts a new trial, spawning player and reward, and setting up the timer.
    /// </summary>
    private IEnumerator StartTrial()
    {
        yield return new WaitForSeconds(0.5f); // Short delay to ensure all components are ready

        if (isTrialActive)
        {
            Debug.Log("Trial already active, ignoring start request.");
            yield break;
        }

        isTrialActive = true;
        rewardCollected = false;
        buttonPressCount = 0;
        trialStartTime = Time.time;

        ShowGrid();
        SpawnPlayer();
        SpawnReward();

        // Start the countdown timer
        if (countdownTimer != null)
        {
            countdownTimer.StartTimer(maxTrialDuration);
        }
        else
        {
            Debug.LogError("CountdownTimer is not assigned!");
        }

        // Set current effort level
        currentEffortLevel = (int)experimentManager.GetCurrentTrialEV();

        // Log the trial start
        Debug.Log($"Trial started - Player at: {currentPlayer.transform.position}, Reward at: {actualRewardPosition}, Effort Level: {currentEffortLevel}");
    }

    /// <summary>
    /// Ends the current trial.
    /// </summary>
    public void EndTrial(bool rewardCollected) // Changed to public
    {
        if (!isTrialActive) return;

        isTrialActive = false;
        float trialDuration = Time.time - trialStartTime;

        // Log trial data
        experimentManager.LogTrialData(rewardCollected, trialDuration, buttonPressCount);

        // Clean up the scene
        CleanupTrial();

        // Notify ExperimentManager that the trial has ended
        experimentManager.EndTrial(rewardCollected);
    }

    /// <summary>
    /// Cleans up the scene after a trial ends.
    /// </summary>
    private void CleanupTrial()
    {
        if (currentPlayer != null)
        {
            PlayerController playerController = currentPlayer.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.DisableMovement();
            }
            Destroy(currentPlayer);
            currentPlayer = null;
        }

        if (currentReward != null)
        {
            Destroy(currentReward);
            currentReward = null;
        }

        countdownTimer.StopTimer();
        HideGrid();
    }

    /// <summary>
    /// Callback for when a trial ends. This method is subscribed to the ExperimentManager's OnTrialEnded event.
    /// </summary>
    private void OnTrialEnd(bool rewardCollected)
    {
        // This method is called by the ExperimentManager, so we don't need to do anything here
        // The next trial will be started by the ExperimentManager
    }
    #endregion

    #region Spawning Methods
    /// <summary>
    /// Spawns the player at a random position using the PlayerSpawner.
    /// </summary>
    private void SpawnPlayer()
    {
        if (playerSpawner != null)
        {
            Vector2 playerPosition = playerSpawner.GetRandomSpawnPosition();
            currentPlayer = playerSpawner.SpawnPlayer(playerPosition);
            if (currentPlayer != null)
            {
                PlayerController playerController = currentPlayer.GetComponent<PlayerController>();
                if (playerController == null)
                {
                    Debug.LogError("PlayerController component not found on spawned player!");
                }
                else
                {
                    playerController.OnRewardCollected += RewardCollected;
                }
            }
            else
            {
                Debug.LogError("Failed to spawn player!");
            }
        }
        else
        {
            Debug.LogError("PlayerSpawner is not assigned!");
        }
    }

    /// <summary>
    /// Spawns the reward at a random position using the RewardSpawner.
    /// </summary>
    private void SpawnReward()
    {
        if (rewardSpawner != null)
        {
            Vector2 rewardPosition = rewardSpawner.GetRandomSpawnPosition();
            int pressesRequired = GetPressesRequired(currentEffortLevel);
            currentReward = rewardSpawner.SpawnReward(rewardPosition, pressesRequired);

            if (currentReward != null)
            {
                actualRewardPosition = currentReward.transform.position;
                Debug.Log($"Reward spawned at position: {actualRewardPosition}");
            }
            else
            {
                Debug.LogError("Failed to spawn reward!");
            }
        }
        else
        {
            Debug.LogError("RewardSpawner is not assigned!");
        }
    }

    // #region Spawning Methods
    // /// <summary>
    // /// Spawns the player at a position determined by the ExperimentManager.
    // /// </summary>
    // private void SpawnPlayer()
    // {
    //     if (playerSpawner != null)
    //     {
    //         Vector2 playerPosition = experimentManager.GetCurrentTrialPlayerPosition();
    //         currentPlayer = playerSpawner.SpawnPlayer(playerPosition);
    //         if (currentPlayer != null)
    //         {
    //             PlayerController playerController = currentPlayer.GetComponent<PlayerController>();
    //             if (playerController == null)
    //             {
    //                 Debug.LogError("PlayerController component not found on spawned player!");
    //             }
    //             else
    //             {
    //                 playerController.OnRewardCollected += RewardCollected;
    //             }
    //         }
    //         else
    //         {
    //             Debug.LogError("Failed to spawn player!");
    //         }
    //     }
    //     else
    //     {
    //         Debug.LogError("PlayerSpawner is not assigned!");
    //     }
    // }

    // /// <summary>
    // /// Spawns the reward at a position determined by the ExperimentManager.
    // /// </summary>
    // private void SpawnReward()
    // {
    //     if (rewardSpawner != null)
    //     {
    //         Vector2 rewardPosition = experimentManager.GetCurrentTrialRewardPosition();
    //         int pressesRequired = GetPressesRequired(currentEffortLevel);
    //         currentReward = rewardSpawner.SpawnReward(rewardPosition, pressesRequired);

    //         if (currentReward != null)
    //         {
    //             actualRewardPosition = currentReward.transform.position;
    //             Debug.Log($"Reward spawned at position: {actualRewardPosition}");
    //         }
    //         else
    //         {
    //             Debug.LogError("Failed to spawn reward!");
    //         }
    //     }
    //     else
    //     {
    //         Debug.LogError("RewardSpawner is not assigned!");
    //     }
    // }

    /// <summary>
    /// Determines the number of button presses required based on the effort level.
    /// </summary>
    /// <param name="effortLevel">The current effort level</param>
    /// <returns>The number of button presses required</returns>
    private int GetPressesRequired(int effortLevel)
    {
        // Ensure effortLevel is within bounds
        effortLevel = Mathf.Clamp(effortLevel, 1, pressesPerEffortLevel.Length);
        return pressesPerEffortLevel[effortLevel - 1];
    }
    #endregion

    #region Grid Management
    /// <summary>
    /// Shows the grid in the scene.
    /// </summary>
    private void ShowGrid()
    {
        if (gridManager != null)
        {
            gridManager.ShowGrid();
        }
        else
        {
            Debug.LogError("GridManager is null. Cannot show grid.");
        }
    }

    /// <summary>
    /// Hides the grid in the scene.
    /// </summary>
    private void HideGrid()
    {
        if (gridManager != null)
        {
            gridManager.HideGrid();
        }
        else
        {
            Debug.LogError("GridManager is null. Cannot hide grid.");
        }
    }
    #endregion

    #region Reward Collection
    /// <summary>
    /// Called when the player collects the reward.
    /// </summary>
    public void RewardCollected()
    {
        if (!isTrialActive) return;

        rewardCollected = true;
        float rewardValue = experimentManager.GetCurrentTrialRewardValue();
        scoreManager.IncreaseScore(Mathf.RoundToInt(rewardValue));
        EndTrial(true);
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Increments the button press count for the current trial.
    /// </summary>
    public void IncrementButtonPressCount()
    {
        buttonPressCount++;
    }
    #endregion
}