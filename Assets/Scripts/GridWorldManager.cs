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
    [SerializeField] private GridManager gridManager;
    [SerializeField] private PlayerSpawner playerSpawner;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private RewardSpawner rewardSpawner;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private CountdownTimer countdownTimer;

    [Header("Prefabs")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject rewardPrefab;

    [Header("Tour Settings")]
    [SerializeField] private float tourTrialDuration = 30f;
    [SerializeField] private Vector2Int tourPlayerStartPos = new Vector2Int(1, 1);
    [SerializeField] private Vector2Int tourRewardPos = new Vector2Int(3, 3);
    [SerializeField] private int tourGridSize = 5;
    [SerializeField] private int tourRewardPoints = 10;

    [Header("Practice Settings")]
    [SerializeField] private float practiceTrialDuration = 15f;
    [SerializeField] private int practiceGridSize = 6;
    [SerializeField] private float practiceDurationMultiplier = 1.5f;

    [Header("Experiment Settings")]
    [SerializeField] private float defaultTrialDuration = 10f;
    [SerializeField] private int defaultGridSize = 7;

    private bool isTrialActive = false;
    private GameController gameController;
    private TourManager tourManager;
    private PracticeManager practiceManager;

    private void Awake()
    {
        SetupSingleton();
        FindAndValidateComponents();
    }

    private void Start()
    {
        tourManager = TourManager.Instance;
        practiceManager = PracticeManager.Instance;
        SetupBasedOnGameMode();
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

    private void FindAndValidateComponents()
    {
        // Find components if not assigned
        gridManager = gridManager ?? FindAnyObjectByType<GridManager>();
        playerSpawner = playerSpawner ?? FindAnyObjectByType<PlayerSpawner>();
        playerController = playerController ?? FindAnyObjectByType<PlayerController>();
        rewardSpawner = rewardSpawner ?? FindAnyObjectByType<RewardSpawner>();
        scoreManager = scoreManager ?? FindAnyObjectByType<ScoreManager>();
        countdownTimer = countdownTimer ?? FindAnyObjectByType<CountdownTimer>();
        gameController = GameController.Instance;

        // Validate critical components
        ValidateComponent(gridManager, "GridManager");
        ValidateComponent(playerSpawner, "PlayerSpawner");
        ValidateComponent(playerController, "PlayerController");
        ValidateComponent(rewardSpawner, "RewardSpawner");
        ValidateComponent(scoreManager, "ScoreManager");
    }

    private void ValidateComponent<T>(T component, string componentName) where T : UnityEngine.Object
    {
        if (component == null)
        {
            Debug.LogError($"{componentName} is missing! Please assign it in the inspector or ensure it exists in the scene.");
        }
    }

    private void SetupBasedOnGameMode()
    {
        if (tourManager == null)
        {
            Debug.Log("TourManager instance not found!");
            return;
        }

        if (tourManager.IsTourActive())
        {
            SetupForTour();
        }
        else if (practiceManager != null && practiceManager.IsPracticeTrial())
        {
            SetupForPractice();
        }
        else
        {
            SetupForFormalExperiment();
        }
    }

    private void SetupForTour()
    {
        Debug.Log("Setting up tour mode...");

        // Configure timer
        if (countdownTimer != null)
        {
            countdownTimer.SetDuration(tourTrialDuration);
            countdownTimer.gameObject.SetActive(false); // Hide timer during tour
        }

        // Spawn player at predetermined position
        GameObject player = playerSpawner.SpawnPlayer(tourPlayerStartPos);
        if (player != null)
        {
            playerController.EnableMovement();
            Debug.Log($"Tour player spawned at {tourPlayerStartPos}");
        }

        // Spawn reward at fixed position for tour
        SpawnTourReward();

        // Reset score for tour
        if (scoreManager != null)
        {
            scoreManager.ResetScore();
        }

        isTrialActive = true;
    }

    private void SpawnTourReward()
    {
        // Get the exact spawn position in world coordinates
        Vector2 spawnPosition = gridManager.GetCellCenterWorldPosition(tourRewardPos);
        
        GameObject reward = rewardSpawner.SpawnReward(
            blockIndex: 0,
            trialIndex: tourManager.GetCurrentStepIndex(),
            pressesRequired: 1,
            scoreValue: tourRewardPoints
        );

        if (reward != null)
        {
            reward.transform.position = spawnPosition;
            Debug.Log($"Tour reward spawned at {spawnPosition}");

            // Add collector component for tour
            var collector = reward.AddComponent<RewardCollector>();
            collector.OnCollected += HandleTourRewardCollection;
        }
    }

private void HandleTourRewardCollection()
{
    if (!tourManager.IsTourActive()) return;

    // Add points
    scoreManager.AddScore(tourRewardPoints, false);

    // Show collection effect
    StartCoroutine(ShowRewardCollectionEffectAndProgress());
}

private IEnumerator ShowRewardCollectionEffectAndProgress()
{
    // Add visual feedback
    GameObject effectObj = new GameObject("CollectionEffect");
    // Add your particle system or animation here
    
    yield return new WaitForSeconds(1f);
    
    if (effectObj != null)
    {
        Destroy(effectObj);
    }

    // Ensure we're in the correct tour step before progressing
    if (tourManager.GetCurrentStepIndex() == 2 || tourManager.GetCurrentStepIndex() == 5)
    {
        tourManager.ProcessNextStep();
    }
}

    private void SetupForPractice()
    {
        Debug.Log("Setting up practice mode...");
        
        // Configure grid for practice
        gridManager.SetGridSize(practiceGridSize, practiceGridSize);

        // Set longer duration for practice trials
        float practiceDuration = defaultTrialDuration * practiceDurationMultiplier;
        if (countdownTimer != null)
        {
            countdownTimer.SetDuration(practiceDuration);
            countdownTimer.gameObject.SetActive(true);
        }

        // Initialize with practice settings
        InitializeGridWorld(practiceDuration);
    }

    private void SetupForFormalExperiment()
    {
        Debug.Log("Setting up formal experiment...");
        
        // Configure grid for experiment
        gridManager.SetGridSize(defaultGridSize, defaultGridSize);

        // Initialize with default settings
        InitializeGridWorld(defaultTrialDuration);
    }

    public void InitializeGridWorld(float trialDuration = -1f)
    {
        if (!ValidateComponents()) return;

        if (trialDuration < 0)
        {
            trialDuration = defaultTrialDuration;
        }

        // Reset the game state
        EndTrial(false);

        // Setup timer
        if (countdownTimer != null)
        {
            countdownTimer.OnTimerExpired -= EndTrialOnTimeUp;
            countdownTimer.OnTimerExpired += EndTrialOnTimeUp;
            countdownTimer.StartTimer(trialDuration);
        }

        // Reset score
        if (scoreManager != null)
        {
            scoreManager.ResetScore();
        }

        // Start new trial
        isTrialActive = true;
        gameController?.StartTrial();
    }

    private bool ValidateComponents()
    {
        if (gridManager == null || playerSpawner == null || 
            rewardSpawner == null || gameController == null)
        {
            Debug.LogError("Essential components are missing. Cannot initialize GridWorld.");
            return false;
        }
        return true;
    }

    public void EndTrial(bool rewardCollected)
    {
        if (!isTrialActive) return;

        isTrialActive = false;

        // Stop timer
        if (countdownTimer != null)
        {
            countdownTimer.StopTimer();
            countdownTimer.OnTimerExpired -= EndTrialOnTimeUp;
        }

        // Disable player movement
        if (playerController != null)
        {
            playerController.DisableMovement();
        }

        // Notify listeners
        OnTrialEnded?.Invoke(rewardCollected);

        // Handle trial end in game controller
        gameController?.EndTrial(rewardCollected);
    }

    private void EndTrialOnTimeUp()
    {
        if (isTrialActive)
        {
            EndTrial(false);
        }
    }

    // Utility method for getting random positions
    public Vector2 GetRandomEmptyPosition()
    {
        return gridManager.GetRandomAvailablePosition();
    }

    // Helper class for handling reward collection
    private class RewardCollector : MonoBehaviour
    {
        public event System.Action OnCollected;
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                OnCollected?.Invoke();
                Destroy(gameObject);
            }
        }
    }
}