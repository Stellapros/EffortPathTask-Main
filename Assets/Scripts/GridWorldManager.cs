using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the overall state and flow of the GridWorld game.
/// This script should be attached to a persistent GameObject that exists across all scenes.
/// </summary>


public class GridWorldManager : MonoBehaviour
{
    // Singleton instance
    public static GridWorldManager Instance { get; private set; }

    // Events
    public event System.Action<bool> OnTrialEnded;
    public event System.Action<int> OnRewardCollected;

    [Header("Core Components")]
    [SerializeField] private GameController gameController;
    [SerializeField] private PlayerSpawner playerSpawner;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private RewardSpawner rewardSpawner;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private CountdownTimer countdownTimer;
    [SerializeField] private LogManager logManager;
    [SerializeField] private ExperimentManager experimentManager;

    [Header("Prefabs")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject rewardPrefab;

    [Header("Practice Settings")]
    [SerializeField] private float practiceTrialDuration = 15f;
    [SerializeField] private int practiceGridSize = 7;
    [SerializeField] private float practiceDurationMultiplier = 1.5f;

    [Header("Experiment Settings")]
    [SerializeField] private float defaultTrialDuration = 10f;
    [SerializeField] private int defaultGridSize = 7;

    private bool isTrialActive = false;
    private bool componentsInitialized = false;

    private void Awake()
    {
        SetupSingleton();
        InitializeComponents();
    }

    private void Start()
    {
        // Double-check components on Start in case they weren't available in Awake
        if (!componentsInitialized)
        {
            InitializeComponents();
        }
    }

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
        if (gridManager == null) gridManager = FindAnyObjectByType<GridManager>();
        if (playerSpawner == null) playerSpawner = FindAnyObjectByType<PlayerSpawner>();
        if (rewardSpawner == null) rewardSpawner = FindAnyObjectByType<RewardSpawner>();
        if (gameController == null) gameController = FindAnyObjectByType<GameController>();

        // Try to find other optional components
        if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();
        if (scoreManager == null) scoreManager = FindAnyObjectByType<ScoreManager>();
        if (countdownTimer == null) countdownTimer = FindAnyObjectByType<CountdownTimer>();
        if (logManager == null) logManager = FindAnyObjectByType<LogManager>();
        if (experimentManager == null) experimentManager = FindAnyObjectByType<ExperimentManager>();

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

        componentsInitialized = true;
        Debug.Log("All required components initialized successfully.");
    }


    // Instead of:
    // InitializeGridWorld(duration, effortLevel, pressesRequired, sprite);
    // Use:
    // StartCoroutine(InitializeGridWorld(duration, effortLevel, pressesRequired, sprite));
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

// public IEnumerator InitializeGridWorld(float trialDuration = -1f, int effortLevel = 0, int pressesRequired = 0, Sprite rewardSprite = null)
// {
//     Debug.Log("Starting GridWorld initialization...");
    
//     // First, ensure scene is loaded
//     AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("GridWorld");
//     while (!asyncLoad.isDone)
//     {
//         yield return null;
//     }

//     // Wait one frame for all components to initialize
//     yield return new WaitForEndOfFrame();

//     // Force component initialization
//     InitializeComponents();
    
//     if (!ValidateComponents())
//     {
//         Debug.LogError("Critical components missing - cannot initialize GridWorld");
//         yield break;
//     }

//     // Initialize grid first
//     gridManager.EnsureInitialization();

//     // Spawn player with explicit movement enable
//     Vector2 spawnPos = playerSpawner.GetRandomSpawnPosition();
//     GameObject player = playerSpawner.SpawnPlayer(spawnPos);
//     if (player != null)
//     {
//         PlayerController playerCtrl = player.GetComponent<PlayerController>();
//         if (playerCtrl != null)
//         {
//             playerCtrl.EnableMovement();
//             Debug.Log("Player movement enabled");
//         }
//     }

//     // Initialize reward spawner with trial properties
//     if (rewardSpawner != null)
//     {
//         rewardSpawner.EnsureInitialization();
//         rewardSpawner.SetRewardProperties(effortLevel, pressesRequired);
//         if (rewardSprite != null)
//         {
//             rewardSpawner.SetRewardSprite(rewardSprite);
//         }
        
//         // Spawn reward with explicit trial data
//         GameObject reward = rewardSpawner.SpawnRewardInternal(
//             experimentManager.GetCurrentBlockNumber(),
//             experimentManager.GetCurrentTrialIndex(),
//             pressesRequired,
//             experimentManager.GetCurrentTrialRewardValue()
//         );

//         if (reward == null)
//         {
//             Debug.LogError("Failed to spawn reward");
//         }
//         else
//         {
//             Debug.Log("Reward spawned successfully");
//         }
//     }

//     // Setup timer with explicit duration
//     if (countdownTimer != null)
//     {
//         countdownTimer.OnTimerExpired -= EndTrialOnTimeUp;
//         countdownTimer.OnTimerExpired += EndTrialOnTimeUp;
//         float duration = trialDuration > 0 ? trialDuration : defaultTrialDuration;
//         countdownTimer.StartTimer(duration);
//         Debug.Log($"Timer started with duration: {duration}");
//     }

//     // Start the trial
//     isTrialActive = true;
//     gameController?.StartTrial();
// }

//     private bool ValidateComponents()
//     {
//         if (gridManager == null || rewardSpawner == null || experimentManager == null)
//         {
//             Debug.LogError("One or more required components are missing in GridWorldManager.");
//             return false;
//         }
//         return true;
//     }

    // public void SetRewardSpriteFromDecisionPhase()
    // {
    //     if (rewardSpawner == null || experimentManager == null)
    //     {
    //         Debug.LogError("RewardSpawner or ExperimentManager is null. Cannot set reward sprite.");
    //         return;
    //     }

    //     Sprite effortSprite = experimentManager.GetCurrentTrialSprite();
    //     rewardSpawner.SetRewardSprite(effortSprite);
    // }

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

        // Add this line to move to the next trial
        ExperimentManager.Instance.MoveToNextTrial();
    }

    // Utility method for getting random positions
    public Vector2 GetRandomEmptyPosition()
    {
        return gridManager.GetRandomAvailablePosition();
    }
}