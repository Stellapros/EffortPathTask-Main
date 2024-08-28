using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    // Serialized fields for Unity inspector
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerSpawner playerSpawner;
    [SerializeField] private RewardSpawner rewardSpawner;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private CountdownTimer countdownTimer;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private int rewardValue = 1;
    [SerializeField] private float maxTrialDuration = 10f; // Maximum duration for each trial
    [SerializeField] private float restDuration = 3f;
    [SerializeField] private int[] pressesPerStep = { 3, 2, 1 };
    [SerializeField] private int trialsPerBlock = 20;

    // Private fields for game state
    private GameObject currentPlayer;
    private GameObject currentReward;
    private int currentBlockIndex = 0;
    private int currentTrialInBlock = 0;
    public bool rewardCollected = false;

    // Event for reward collection
    public event System.Action OnRewardCollected;

    // Reference to the experiment manager
    private IExperimentManager _experimentManager;

    // Store the actual reward position
    private Vector2 actualRewardPosition;

    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// Here we try to find the ExperimentManager in the scene.
    /// </summary>
    private void Awake()
    {
        _experimentManager = FindObjectOfType<ExperimentManager>();
        if (_experimentManager == null)
        {
            Debug.LogError("ExperimentManager not found in the scene!");
        }
    }

    /// <summary>
    /// Set up the experiment manager and subscribe to its OnTrialEnded event.
    /// </summary>
    /// <param name="experimentManager">The experiment manager instance</param>
    public void SetExperimentManager(IExperimentManager experimentManager)
    {
        _experimentManager = experimentManager;
        _experimentManager.OnTrialEnded += OnTrialEnd;
    }

    /// <summary>
    /// Start is called before the first frame update.
    /// Here we start the experiment if the ExperimentManager is set.
    /// </summary>
    private void Start()
    {
        if (_experimentManager == null)
        {
            Debug.LogError("ExperimentManager not found in GameController!");
            return;
        }

        StartExperiment();
    }

    /// <summary>
    /// This method is called when a new trial starts.
    /// It spawns the player, enables movement, and sets up other trial-specific logic.
    /// </summary>
    public void StartTrial()
    {
        // Get the initial position for the player from the experiment manager
        Vector2 initialPlayerPosition = _experimentManager.GetCurrentTrialPlayerPosition();

        // Spawn the player at the initial position
        SpawnPlayer(initialPlayerPosition);

        // Enable player movement
        if (playerController != null)
        {
            playerController.EnableMovement();
        }

        // Spawn the reward
        int effortLevel = (int)_experimentManager.GetCurrentTrialEV();
        SpawnReward(effortLevel);

        // Start the countdown timer
        if (countdownTimer != null)
        {
            countdownTimer.StartTimer(maxTrialDuration);
            StartCoroutine(CheckTimerCoroutine());
        }
        else
        {
            Debug.LogError("CountdownTimer is not assigned!");
        }

        // Log the trial start with the actual reward position
        Debug.Log($"Trial started - Player at: {initialPlayerPosition}, Reward at: {actualRewardPosition}, Effort Level: {effortLevel}");
    }

    /// <summary>
    /// Coroutine to check if the timer has ended.
    /// </summary>
    private IEnumerator CheckTimerCoroutine()
    {
        // Wait until the timer reaches zero
        while (countdownTimer.TimeLeft > 0)
        {
            yield return null;
        }
        EndTrial(false); // Time's up, end trial without reward collection
    }

    /// <summary>
    /// Start the experiment by calling StartTrial on the ExperimentManager.
    /// </summary>
    private void StartExperiment()
    {
        _experimentManager.StartTrial();
    }

    /// <summary>
    /// Callback for when a trial ends. This method is subscribed to the ExperimentManager's OnTrialEnded event.
    /// </summary>
    /// <param name="rewardCollected">Whether the reward was collected or not</param>
    private void OnTrialEnd(bool rewardCollected)
    {
        EndTrial(rewardCollected);
    }

    /// <summary>
    /// Method to end the current trial. It cleans up the scene and notifies the ExperimentManager.
    /// </summary>
    /// <param name="rewardCollected">Whether the reward was collected or not</param>
    private void EndTrial(bool rewardCollected)
    {
        if (playerController != null)
        {
            playerController.DisableMovement();
            playerController.EndTrial();
            playerController.OnRewardCollected -= RewardCollected;
        }

        DespawnPlayer();
        ClearReward();

        if (countdownTimer != null)
        {
            countdownTimer.StopTimer();
        }

        StopAllCoroutines(); // Stop the timer checking coroutine

        string trialOutcome = rewardCollected ? "Reward Collected" : "Time Out";
        Debug.Log($"Trial ended - Block:{currentBlockIndex};Trial:{currentTrialInBlock};Outcome:{trialOutcome}");

        _experimentManager.EndTrial(rewardCollected);
    }

    /// <summary>
    /// Method called when a reward is collected. It updates the game state and score.
    /// </summary>
    public void RewardCollected()
    {
        OnRewardCollected?.Invoke();
        rewardCollected = true;
        scoreManager.IncreaseScore(rewardValue);
        EndTrial(true);
    }

    /// <summary>
    /// Method to spawn the player at a given position.
    /// </summary>
    /// <param name="position">The position to spawn the player</param>
    private void SpawnPlayer(Vector2 playerPosition)
    {
        if (playerSpawner != null)
        {
            currentPlayer = playerSpawner.SpawnPlayer(playerPosition);
            if (currentPlayer != null)
            {
                playerController = currentPlayer.GetComponent<PlayerController>();
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
    /// Method to spawn the reward with a specific effort level.
    /// </summary>
    /// <param name="effortLevel">The effort level for the reward</param>
    private void SpawnReward(int effortLevel)
    {
        if (rewardSpawner != null)
        {
            currentReward = rewardSpawner.SpawnReward(currentBlockIndex, currentTrialInBlock, effortLevel);
            if (currentReward != null)
            {
                actualRewardPosition = currentReward.transform.position;
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

    /// <summary>
    /// Method to despawn the player.
    /// </summary>
    private void DespawnPlayer()
    {
        if (currentPlayer != null)
        {
            playerSpawner.DespawnPlayer(currentPlayer);
            currentPlayer = null;
            playerController = null;
        }
    }

    /// <summary>
    /// Method to clear the reward from the scene.
    /// </summary>
    private void ClearReward()
    {
        if (currentReward != null)
        {
            rewardSpawner.ClearReward();
            currentReward = null;
        }
    }

    /// <summary>
    /// Method to initialize the game state for a new experiment.
    /// </summary>
    public void InitializeGameState()
    {
        currentBlockIndex = 0;
        currentTrialInBlock = 0;
        rewardCollected = false;
        scoreManager.ResetScore();

        Vector2 playerPosition = _experimentManager.GetCurrentTrialPlayerPosition();
        if (playerController != null)
        {
            playerController.ResetPosition(playerPosition);
        }
        else
        {
            Debug.LogError("PlayerController is null when trying to reset position!");
        }
    }

    /// <summary>
    /// Method to load the decision scene.
    /// </summary>
    public void LoadDecisionScene()
    {
        SceneManager.LoadScene("DecisionPhase");
    }
}