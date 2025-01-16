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
    // public event System.Action<int> OnRewardCollected;

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
    private PracticeManager practiceManager;

    [Header("Prefabs")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject rewardPrefab;

    private bool isTrialActive = false;
    // private bool componentsInitialized = false;


    private void Start()
    {
        InitializeComponents();

        // Subscribe to timer expiration if not already done
        if (countdownTimer != null)
        {
            countdownTimer.OnTimerExpired += HandleTimerExpired;
        }

        // Set up trial GridWorld
        SetupTrialGridWorld();

        // Additional setup if needed
        isTrialActive = true;

        // Subscribe to timer expiration if not already done
        if (countdownTimer != null)
        {
            countdownTimer.OnTimerExpired += HandleTimerExpired;
        }
    }

    private void InitializeComponents()
    {
        practiceManager = FindAnyObjectByType<PracticeManager>();
        rewardSpawner = FindAnyObjectByType<RewardSpawner>();

        if (practiceManager == null)
        {
            Debug.LogError("PracticeManager not found in the scene!");
            // Consider adding a fallback or creating a new instance
            practiceManager = FindAnyObjectByType<PracticeManager>();
            if (practiceManager == null)
            {
                practiceManager = gameObject.AddComponent<PracticeManager>();
            }
        }

        if (rewardSpawner == null)
        {
            Debug.LogError("RewardSpawner not found in the scene!");
            // Similar fallback for RewardSpawner if needed
        }

        Debug.Log($"Practice Manager initialized: {practiceManager != null}");
        Debug.Log($"Reward Spawner initialized: {rewardSpawner != null}");
    }

    // private void SetupPracticeGridWorld()
    // {
    //     Debug.Log("SetupPracticeGridWorld called");

    //     if (!practiceManager.IsPracticeTrial())
    //     {
    //         Debug.LogError("Attempting to load GridWorld without a practice trial flag!");
    //         SceneManager.LoadScene("GetReadyCheck");
    //         return;
    //     }

    //     // Log current trial details
    //     int currentTrialIndex = practiceManager.GetCurrentPracticeTrialIndex();
    //     PracticeManager.PracticeTrial currentTrial = practiceManager.GetCurrentPracticeTrial();

    //     Debug.Log($"Current Practice Trial Index: {currentTrialIndex}");

    //     if (currentTrial != null)
    //     {
    //         Debug.Log($"Current Trial Details:");
    //         Debug.Log($"  Effort Level: {currentTrial.effortLevel}");
    //         Debug.Log($"  Reward Value: {currentTrial.rewardValue}");
    //     }

    //     SetRewardSpriteFromDecisionPhase();
    // }

    private void SetupTrialGridWorld()
    {
        // Get the sprite from the current practice trial
        if (practiceManager != null)
        {
            Sprite trialSprite = practiceManager.GetCurrentPracticeTrialSprite();

            if (trialSprite != null && rewardSpawner != null)
            {
                Debug.Log($"Setting reward sprite for Practice GridWorld: {trialSprite.name}");
                SetRewardSpriteFromDecisionPhase();
            }
            else
            {
                Debug.LogError("Could not set reward sprite for Practice GridWorld - sprite or reward spawner is null");
            }
        }
        else
        {
            Debug.LogError("PracticeManager is null when setting up Practice GridWorld");
        }
    }

    public void SetRewardSpriteFromDecisionPhase()
    {
        if (rewardSpawner == null)
        {
            Debug.LogError("RewardSpawner is null. Cannot set reward sprite.");
            return;
        }

        Sprite effortSprite = practiceManager.GetCurrentPracticeTrialSprite();

        if (effortSprite != null)
        {
            Debug.Log($"Setting reward sprite in PracticeGridWorldManager: {effortSprite.name}");
            rewardSpawner.SetRewardSprite(effortSprite);
        }
        else
        {
            Debug.LogError("Current practice trial sprite is null!");
        }
    }

    private void EndTrialOnTimeUp()
    {
        if (isTrialActive)
        {
            // EndPracticeGridWorldTrial(false);
            EndTrial(false);
        }
    }

    private void HandleTimerExpired()
    {
        // Automatically end the trial when timer expires
        // EndPracticeGridWorldTrial(false);
        EndTrial(false);
    }


    //     public void EndPracticeGridWorldTrial(bool rewardCollected)
    //     {
    //         Debug.Log($"EndPracticeGridWorldTrial called:");
    //         Debug.Log($"Reward Collected: {rewardCollected}");

    //         PlayerPrefs.SetInt("IsPracticeTrial", 1);

    //         // Stop the countdown timer and unsubscribe
    //         if (countdownTimer != null)
    //         {
    //             countdownTimer.StopTimer();
    //             countdownTimer.OnTimerExpired -= HandleTimerExpired;
    //         }

    //         // Always handle trial completion and progress to next trial when in GridWorld
    //         OnTrialEnded?.Invoke(rewardCollected);
    //         gameController.EndTrial(rewardCollected);

    //         // Always pass true when from GridWorld
    //         // practiceManager.HandlePracticeTrialCompletion(fromGridWorld: true);

    //         if (practiceManager != null)
    //         {
    //             Debug.Log("Calling HandlePracticeTrialCompletion on PracticeManager");
    //             practiceManager.HandleGridWorldOutcome(true);
    //         }
    //         else
    //         {
    //             Debug.LogError("Practice Manager is NULL when ending GridWorld Trial!");
    //         }

    //         // Disable player movement
    //         if (playerController != null)
    //         {
    //             playerController.DisableMovement();
    //         }
    //     }

    public void EndTrial(bool rewardCollected)
    {
        Debug.Log($"Practice GridWorld EndTrial called. Reward Collected: {rewardCollected}");

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

        // If trial is already not active, return
        if (!isTrialActive) return;

        // Stop the countdown timer
        if (countdownTimer != null)
        {
            countdownTimer.StopTimer();
            countdownTimer.OnTimerExpired -= HandleTimerExpired;
        }

        isTrialActive = false;

        // Notify any listeners that the trial has ended
        OnTrialEnded?.Invoke(rewardCollected);

        // Let GameController handle the trial end
        if (gameController != null)
        {
            gameController.EndTrial(rewardCollected);
        }

        // Disable player movement
        if (playerController != null)
        {
            playerController.DisableMovement();
        }

        // Handle practice trial completion through PracticeManager
        if (practiceManager != null)
        {
            practiceManager.HandleGridWorldOutcome(false);
        }
        else
        {
            Debug.LogError("PracticeManager is null when ending Practice GridWorld Trial!");
        }
    }
}