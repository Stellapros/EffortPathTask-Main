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
    //[SerializeField] private CountdownTimer countdownTimer;
    //[SerializeField] private ScoreManager scoreManager;

    // Lazy-loaded components
    private CountdownTimer _countdownTimer;
    private ScoreManager _scoreManager;
    [SerializeField] private int rewardValue = 10;
    [SerializeField] private float maxTrialDuration = 10f; // Maximum duration for each trial
    [SerializeField] private float restDuration = 3f;
    [SerializeField] private int[] pressesPerStep = { 3, 2, 1 };
    [SerializeField] private int trialsPerBlock = 20;

    // Private fields for game state
    private GameObject currentPlayer;
    private GameObject currentReward;
    private int currentBlockIndex = 0;
    private int currentTrialInBlock = 0;
    private int currentTrialNumber = 0;
    private const int TotalTrials = 90;
    public bool rewardCollected = false;
    private float trialStartTime;
    private int buttonPressCount;
    private bool isTrialActive = false;

    // Get ready
    public float getReadyTime;
    public float getReadyDuration;


    // Full screen check
    public bool FLAG_fullScreenModeError;
    public const int STATE_STARTSCREEN = 0;


    // Event for reward collection
    public event System.Action OnRewardCollected;

    // Reference to the experiment manager
    private IExperimentManager _experimentManager;


    // Store the actual reward position
    private Vector2 actualRewardPosition;



    #region Unity Lifecycle Methods
    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// Here we try to find the ExperimentManager in the scene.
    /// </summary>

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
        }
    }

    private void Initialize()
    {
        DontDestroyOnLoad(gameObject);

        ExperimentManager experimentManagerInstance = ExperimentManager.Instance;
        if (experimentManagerInstance != null)
        {
            _experimentManager = experimentManagerInstance as IExperimentManager;
            if (_experimentManager == null)
            {
                Debug.LogError("ExperimentManager does not implement IExperimentManager!");
            }
            else
            {
                // Subscribe to the OnTrialEnded event
                _experimentManager.OnTrialEnded += OnTrialEnd;
            }
        }
        else
        {
            Debug.LogError("ExperimentManager instance not found!");
        }

        // Ensure GridManager is assigned
        if (gridManager == null)
        {
            gridManager = FindObjectOfType<GridManager>();
            if (gridManager == null)
            {
                Debug.LogError("GridManager not found in the scene!");
            }
        }

        ValidateComponents();
    }

    private void EnsureExperimentManagerExists()
    {
        if (ExperimentManager.Instance == null)
        {
            Debug.LogWarning("ExperimentManager not found. Attempting to load Managers scene.");
            StartCoroutine(LoadPersistentScene());
        }
    }

    private IEnumerator LoadPersistentScene()
    {
        // AsyncOperation asyncLoad = SceneManagement.LoadSceneAsync("Persistent", LoadSceneMode.Additive);
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("Persistent", LoadSceneMode.Additive);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        if (ExperimentManager.Instance == null)
        {
            Debug.LogError("Failed to load ExperimentManager. Please ensure it's in the Managers scene.");
        }
    }

    /// <summary>
    /// Start is called before the first frame update.
    /// Here we start the experiment if the ExperimentManager is set.
    /// </summary>
    private void Start()
    {
        // Ensure all required components are assigned
        if (enabled)
        { ValidateComponents(); }
    }

    private void Update()
    {
        if (isTrialActive)
        {
            // Count button presses
            if (Input.GetKeyDown(KeyCode.Space))
            {
                buttonPressCount++;
            }

            // Check if trial time is up
            if (Time.time - trialStartTime >= maxTrialDuration)
            {
                EndTrial(false);
            }
        }
    }
    /// <summary>
    /// Validates that all required components are assigned.
    /// </summary>
    private void ValidateComponents()
    {
        if (playerController == null) Debug.LogError("PlayerController is not assigned!");
        if (playerSpawner == null) Debug.LogError("PlayerSpawner is not assigned!");
        if (rewardSpawner == null) Debug.LogError("RewardSpawner is not assigned!");
        if (gridManager == null) Debug.LogError("GridManager is not assigned!");
        //if (countdownTimer == null) Debug.LogError("CountdownTimer is not assigned!");
        //if (scoreManager == null) Debug.LogError("ScoreManager is not assigned!");
    }
    #endregion



    #region Experiment Control Methods
    /// <summary>
    /// Starts a new trial, spawning player and reward, and setting up the timer.
    /// </summary>
    public void StartTrial()
    {
        if (isTrialActive)
        {
            Debug.Log("Trial already active, ignoring start request.");
            return;
        }

        isTrialActive = true;
        rewardCollected = false;
        buttonPressCount = 0;
        trialStartTime = Time.time;

        ShowGrid();

        SpawnPlayer();

        // Enable player movement
        if (playerController != null)
        {
            playerController.EnableMovement();
        }
        else
        {
            Debug.LogError("PlayerController is null. Cannot enable movement.");
        }

        // Spawn the reward
        int effortLevel = (int)_experimentManager.GetCurrentTrialEV();
        SpawnReward(effortLevel);

        // Start the countdown timer
        if (countdownTimer != null)
        {
            countdownTimer.StartTimer(maxTrialDuration);
        }
        else
        {
            Debug.LogError("CountdownTimer is not assigned!");
        }

        // Log the trial start
        Debug.Log($"Trial started - Player at: {currentPlayer.transform.position}, Reward at: {actualRewardPosition}, Effort Level: {effortLevel}");
    }


    /// <summary>
    /// Coroutine to check if the timer has ended.
    /// </summary>
    private IEnumerator CheckTimerCoroutine()
    {
        while (countdownTimer.TimeLeft > 0 && !rewardCollected)
        {
            yield return null;
        }
        if (!rewardCollected)
        {
            EndTrial(false); // Time's up, end trial without reward collection
        }
    }

    /// <summary>
    /// Start the experiment by calling StartTrial on the ExperimentManager.
    /// </summary>
    private void StartExperiment()
    {
        currentTrialNumber = 0;
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
    /// Ends the current trial.
    /// </summary>
    private void EndTrial(bool rewardCollected)
    {
        if (!isTrialActive) return;

        isTrialActive = false;
        float trialDuration = Time.time - trialStartTime;

        // Log trial data
        _experimentManager.LogTrialData(rewardCollected, trialDuration, buttonPressCount);

        // Clean up the scene
        CleanupTrial();

        // Notify ExperimentManager that the trial has ended
        _experimentManager.EndTrial(rewardCollected);
    }



    /// <summary>
    /// Cleans up the scene after a trial ends.
    /// </summary>
    private void CleanupTrial()
    {
        if (playerController != null)
        {
            playerController.DisableMovement();
            playerController.OnRewardCollected -= RewardCollected;
        }

        if (currentPlayer != null)
        {
            playerSpawner.DespawnPlayer(currentPlayer);
            currentPlayer = null;
        }

        if (currentReward != null)
        {
            rewardSpawner.ClearReward();
            currentReward = null;
        }

        countdownTimer.StopTimer();
    }


    // Properties for lazy-loaded components
    private CountdownTimer countdownTimer
    {
        get
        {
            if (_countdownTimer == null)
            {
                _countdownTimer = FindObjectOfType<CountdownTimer>();
                if (_countdownTimer == null)
                {
                    Debug.LogError("CountdownTimer not found in the scene!");
                }
            }
            return _countdownTimer;
        }
    }

    private ScoreManager scoreManager
    {
        get
        {
            if (_scoreManager == null)
            {
                _scoreManager = FindObjectOfType<ScoreManager>();
                if (_scoreManager == null)
                {
                    Debug.LogError("ScoreManager not found in the scene!");
                }
            }
            return _scoreManager;
        }
    }



    /// <summary>
    /// Coroutine to start the next trial after a short delay.
    /// </summary>
    private IEnumerator StartNextTrialAfterDelay()
    {
        yield return new WaitForSeconds(1f); // 1 second delay between trials
        StartTrial();
    }

    /// <summary>
    /// Called when the player collects the reward.
    /// </summary>
    public void RewardCollected()
    {
        if (!isTrialActive) return;

        scoreManager.IncreaseScore(rewardValue);
        EndTrial(true);
    }

    /// <summary>
    /// Ends the experiment and transitions to the end screen.
    /// </summary>


    private void EndExperiment()
    {
        Debug.Log("Experiment completed. Loading end screen.");
        SceneManager.LoadScene("EndExperiment");
    }

    #endregion

    #region Spawning Methods
    /// <summary>
    /// Spawns the player at the given position.
    /// </summary>
    private void SpawnPlayer()
    {
        if (playerSpawner != null)
        {
            Vector2 initialPlayerPosition = _experimentManager.GetCurrentTrialPlayerPosition();
            currentPlayer = playerSpawner.SpawnPlayer(initialPlayerPosition);
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
    /// Spawns the reward at the given position with the specified effort level.
    /// </summary>
    private void SpawnReward(int effortLevel)
    {
        if (rewardSpawner != null)
        {
            int pressesRequired = GetPressesRequired(effortLevel);
            currentReward = rewardSpawner.SpawnReward(currentBlockIndex, currentTrialInBlock, pressesRequired);
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

    private int GetPressesRequired(int effortLevel)
    {
        // Ensure effortLevel is within bounds
        effortLevel = Mathf.Clamp(effortLevel, 1, pressesPerStep.Length);
        return pressesPerStep[effortLevel - 1];
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
        else
        {
            Debug.LogWarning("RewardSpawner is null during ClearReward.");
        }
    }
    #endregion

    #region Grid Management
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
}
