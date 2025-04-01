using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Manages the overall state and flow of the GridWorld game.
/// This script should be attached to a persistent GameObject that exists across all scenes.
/// </summary>


public class GridWorldManager : MonoBehaviour
{
    private static GridWorldManager _instance;
    public static GridWorldManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<GridWorldManager>();
            }
            return _instance;
        }
        private set { _instance = value; }
    }

    // Events
    public event System.Action<bool> OnTrialEnded;

    [Header("Core Components")]
    public GameController gameController;
    public PlayerSpawner playerSpawner;
    public PlayerController playerController;
    public RewardSpawner rewardSpawner;
    public GridManager gridManager;
    public ScoreManager scoreManager;
    public LogManager logManager;
    public CountdownTimer countdownTimer;
    public ExperimentManager experimentManager;

    [Header("Prefabs")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject rewardPrefab;


    private bool isTrialActive = false;
    // private bool componentsInitialized = false;

    private void Awake()
    {
        // Proper singleton pattern implementation
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Always start disabled - will be enabled when needed
        gameObject.SetActive(false);
    }

    private void Start()
    {
        // Only initialize components if we're in GridWorld scene
        if (SceneManager.GetActiveScene().name == "GridWorld")
        {
            gameObject.SetActive(true);
            InitializeComponents();
            SetupTrialGridWorld();
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Only activate and initialize in GridWorld scene
        if (scene.name == "GridWorld")
        {
            gameObject.SetActive(true);
            InitializeComponents();
            SetupTrialGridWorld();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void InitializeComponents()
    {
        // Try to find components in the scene first
        if (gameController == null)
        {
            gameController = GameController.Instance;
            if (gameController == null)
            {
                Debug.LogError("Could not find GameController instance!");
                enabled = false;
                return;
            }
        }
        // Try to find components in the scene first
        // Replaced FindObjectOfType<T>() with FindFirstObjectByType<T>()
        if (gridManager == null) gridManager = Object.FindFirstObjectByType<GridManager>();
        if (playerSpawner == null) playerSpawner = Object.FindFirstObjectByType<PlayerSpawner>();
        if (rewardSpawner == null) rewardSpawner = Object.FindFirstObjectByType<RewardSpawner>();
        if (gameController == null) gameController = Object.FindFirstObjectByType<GameController>();

        // Try to find other optional components
        if (playerController == null) playerController = Object.FindFirstObjectByType<PlayerController>();
        if (scoreManager == null) scoreManager = Object.FindFirstObjectByType<ScoreManager>();
        if (countdownTimer == null) countdownTimer = Object.FindFirstObjectByType<CountdownTimer>();
        if (logManager == null) logManager = Object.FindFirstObjectByType<LogManager>();
        if (experimentManager == null) experimentManager = Object.FindFirstObjectByType<ExperimentManager>();

        // If critical components are still missing, try to create them
        if (gridManager == null)
        {
            GameObject gridObj = new GameObject("GridManager");
            gridManager = gridObj.AddComponent<GridManager>();
            Debug.LogWarning("GridManager was missing - created new instance.");
        }

        if (playerSpawner == null && playerPrefab != null)
        {
            GameObject spawnerObj = new GameObject("PlayerSpawner");
            playerSpawner = spawnerObj.AddComponent<PlayerSpawner>();
            Debug.LogWarning("PlayerSpawner was missing - created new instance.");
        }

        if (rewardSpawner == null && rewardPrefab != null)
        {
            GameObject spawnerObj = new GameObject("RewardSpawner");
            rewardSpawner = spawnerObj.AddComponent<RewardSpawner>();
            Debug.LogWarning("RewardSpawner was missing - created new instance.");
        }

        if (gameController == null)
        {
            GameObject controllerObj = new GameObject("GameController");
            gameController = controllerObj.AddComponent<GameController>();
            Debug.LogWarning("GameController was missing - created new instance.");
        }

        ValidateRequiredComponents();
    }

    private void ValidateRequiredComponents()
    {
        bool hasRequired = true;
        string missingComponents = "";

        if (gridManager == null) { missingComponents += "GridManager, "; hasRequired = false; }
        if (playerSpawner == null) { missingComponents += "PlayerSpawner, "; hasRequired = false; }
        if (rewardSpawner == null) { missingComponents += "RewardSpawner, "; hasRequired = false; }
        if (gameController == null) { missingComponents += "GameController, "; hasRequired = false; }

        if (!hasRequired)
        {
            missingComponents = missingComponents.TrimEnd(',', ' ');
            Debug.LogError($"Critical components still missing after initialization attempt: {missingComponents}");
            enabled = false; // Disable this component if critical dependencies are missing
            return;
        }

        // componentsInitialized = true;
        Debug.Log("All required components initialized successfully.");
    }


    private void SetupTrialGridWorld()
    {
        string spriteName = PlayerPrefs.GetString("CurrentRewardSpriteName", "");
        if (!string.IsNullOrEmpty(spriteName))
        {
            Sprite trialSprite = LoadSpriteByName(spriteName);
            if (trialSprite != null)
            {
                rewardSpawner.SetRewardSprite(trialSprite);
            }
        }

        // Log trial start
        int currentTrial = ExperimentManager.Instance?.GetCurrentTrialIndex() ?? 0;
        int currentBlock = ExperimentManager.Instance?.GetCurrentBlockNumber() ?? 0;
        bool isPractice = ExperimentManager.Instance?.IsCurrentTrialPractice() ?? false;
        LogTrialStart(currentTrial, currentBlock, isPractice);
    }

    private Sprite LoadSpriteByName(string spriteName)
    {
        Sprite sprite = Resources.Load<Sprite>(spriteName);

        if (sprite == null)
        {
            Sprite[] allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
            sprite = System.Array.Find(allSprites, s => s.name == spriteName);
        }

        if (sprite == null)
        {
            Debug.LogError($"Could not find sprite with name: {spriteName}");
        }

        return sprite;
    }

    private void EndTrialOnTimeUp()
    {
        if (isTrialActive)
        {
            EndTrial(false);
        }
    }

    public void EndTrial(bool rewardCollected)
    {
        if (!isTrialActive) return;

        // Log trial end
        int currentTrial = ExperimentManager.Instance?.GetCurrentTrialIndex() ?? 0;
        int currentBlock = ExperimentManager.Instance?.GetCurrentBlockNumber() ?? 0;
        LogTrialEnd(currentTrial, currentBlock, rewardCollected);

        // Key change: Do NOT immediately end the trial if reward is collected
        // Wait for the full trial duration

        // If reward is collected but timer hasn't expired, just mark it
        if (rewardCollected && countdownTimer != null && countdownTimer.TimeLeft > 0)
        {
            // Disable further reward collection but don't end trial
            if (rewardSpawner != null)
            {
                rewardSpawner.DisableRewardCollection();
            }
            return;
        }

        // Existing end trial logic remains the same
        if (countdownTimer != null && countdownTimer.TimeLeft <= 0)
        {
            isTrialActive = false;

            // Stop the countdown timer
            countdownTimer.StopTimer();
            countdownTimer.OnTimerExpired -= EndTrialOnTimeUp;

            // Notify any listeners that the trial has ended
            OnTrialEnded?.Invoke(rewardCollected);

            // Let GameController handle the trial end
            gameController.EndTrial(rewardCollected);
            playerController.DisableMovement();

            if (!rewardCollected && playerController != null)
            {
                playerController.LogMovementFailure();
            }

            // Move to the next trial
            ExperimentManager.Instance.MoveToNextTrial();
        }
    }

    #region Trial Logging
    public void LogTrialStart(int trialNumber, int blockNumber, bool isPractice)
    {
        if (LogManager.Instance != null)
        {
            LogManager.Instance.LogEvent("TrialStart", new Dictionary<string, string>
        {
            {"TrialNumber", trialNumber.ToString()},
            {"BlockNumber", blockNumber.ToString()},
            {"IsPractice", isPractice.ToString()},
            {"Timestamp", System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}
        });
        }
    }

    public void LogTrialEnd(int trialNumber, int blockNumber, bool rewardCollected)
    {
        if (LogManager.Instance != null)
        {
            LogManager.Instance.LogEvent("TrialEnd", new Dictionary<string, string>
        {
            {"TrialNumber", trialNumber.ToString()},
            {"BlockNumber", blockNumber.ToString()},
            {"RewardCollected", rewardCollected.ToString()},
            {"Timestamp", System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}
        });
        }
    }


    #endregion
}