using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the overall state and flow of the GridWorld game.
/// This script should be attached to a persistent GameObject that exists across all scenes.
/// </summary>


public class PracticeGridWorldManager : MonoBehaviour
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
    [SerializeField] private DecisionManager decisionManager;
    [SerializeField] private CountdownTimer countdownTimer;
    [SerializeField] private LogManager logManager;
    [SerializeField] private ExperimentManager experimentManager;

    [Header("Prefabs")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject rewardPrefab;

    [Header("Practice Settings")]
    [SerializeField] private float practiceTrialDuration = 10f;
    [SerializeField] private int practiceGridSize = 7;
    [SerializeField] private float practiceDurationMultiplier = 1.5f;

    [Header("Experiment Settings")]
    [SerializeField] private float defaultTrialDuration = 5.0f;
    [SerializeField] private int defaultGridSize = 7;

    private bool isTrialActive = false;
    private bool componentsInitialized = false;

    private void Awake()
    {
        // Add this line to hide the GameObject in scenes where it's not needed
        gameObject.SetActive(false);
        InitializeComponents();
    }

    private void Start()
    {
        // Check if this is the GridWorld scene
        if (SceneManager.GetActiveScene().name == "GridWorldScene")
        {
            // Make the GameObject active
            gameObject.SetActive(true);

            // Double-check components on Start
            if (!componentsInitialized)
            {
                InitializeComponents();
            }

            // Determine trial type and configure accordingly
            bool isPracticeTrial = PlayerPrefs.GetInt("IsPracticeTrial", 0) == 1;

            if (isPracticeTrial)
            {
                SetupPracticeTrialGridWorld();
            }
            else
            {
                SetupFormalTrialGridWorld();
            }
        }
        else
        {
            // Disable the GameObject in other scenes
            gameObject.SetActive(false);
        }
    }

    private void SetupPracticeTrialGridWorld()
    {
        // Any specific configuration for practice trials in GridWorld
        // For example, adjusting difficulty, providing more guidance, etc.
        Debug.Log("Configuring GridWorld for Practice Trial");

        // Ensure the practice trial sprite is loaded
        string spriteName = PlayerPrefs.GetString("CurrentRewardSpriteName", "");
        if (!string.IsNullOrEmpty(spriteName))
        {
            // Load and set the sprite for this practice trial
            Sprite practiceSprite = LoadSpriteByName(spriteName);
            // Use the sprite as needed in your GridWorld scene
        }
    }

    private void SetupFormalTrialGridWorld()
    {
        // Configuration for formal trials
        Debug.Log("Configuring GridWorld for Formal Trial");
    }

    private Sprite LoadSpriteByName(string spriteName)
    {
        // First, try loading from Resources
        Sprite sprite = Resources.Load<Sprite>(spriteName);

        if (sprite == null)
        {
            // If not found in Resources, try other methods
            // For example, search through all sprites in the project
            Sprite[] allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
            sprite = System.Array.Find(allSprites, s => s.name == spriteName);
        }

        if (sprite == null)
        {
            Debug.LogError($"Could not find sprite with name: {spriteName}");
        }

        return sprite;
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

        componentsInitialized = true;
        Debug.Log("All required components initialized successfully.");
    }

    public void SetRewardSpriteFromDecisionPhase()
    {
        if (rewardSpawner == null)
        {
            Debug.LogError("RewardSpawner is null. Cannot set reward sprite.");
            return;
        }

        Sprite effortSprite = null;

        // Check if we're in a practice trial
        if (PracticeManager.Instance.IsPracticeTrial())
        {
            // Get sprite from PracticeManager
            effortSprite = PracticeManager.Instance.GetCurrentPracticeTrialSprite();
        }
        else if (experimentManager != null)
        {
            // Get sprite from ExperimentManager for formal trials
            effortSprite = experimentManager.GetCurrentTrialSprite();
        }

        if (effortSprite != null)
        {
            Debug.Log($"Setting reward sprite in GridWorldManager: {effortSprite.name}");
            rewardSpawner.SetRewardSprite(effortSprite);
        }
        else
        {
            Debug.LogError("Current trial sprite is null!");
        }
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

            // Move to the next trial
            ExperimentManager.Instance.MoveToNextTrial();
        }
    }
}