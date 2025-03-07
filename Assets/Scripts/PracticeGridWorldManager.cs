using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;

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

        // Log trial start
        int currentTrial = practiceManager?.GetCurrentPracticeTrialIndex() ?? 0;
        LogTrialStart(currentTrial, true); // Always true for practice trials
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

    public void EndTrial(bool rewardCollected)
    {
    Debug.Log($"Practice GridWorld EndTrial called. Reward Collected: {rewardCollected}");

    // Log trial end
    int currentTrial = practiceManager?.GetCurrentPracticeTrialIndex() ?? 0;
    LogTrialEnd(currentTrial, rewardCollected);

    // Log outcome type
    LogManager.Instance.LogEvent("OutcomeType", new Dictionary<string, string>
    {
        {"TrialNumber", (currentTrial + 1).ToString()},
        {"OutcomeType", rewardCollected ? "Success" : "Failure"}
    });
    
    if (rewardCollected)
    {
        LogManager.Instance.LogEvent("OutcomeType", new Dictionary<string, string>
        {
            {"TrialNumber", currentTrial.ToString()},
            {"OutcomeType", "Success"}
        });
    }
    else
    {
        LogManager.Instance.LogEvent("OutcomeType", new Dictionary<string, string>
        {
            {"TrialNumber", currentTrial.ToString()},
            {"OutcomeType", "Failure"}
        });
    }
    
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

        // Ensure score persists
        if (PracticeScoreManager.Instance != null)
        {
            Debug.Log($"Current Practice Score at EndTrial: {PracticeScoreManager.Instance.GetCurrentScore()}");
        }
    }

    #region Trial Logging
    public void LogTrialStart(int trialNumber, bool isPractice)
    {
        if (LogManager.Instance != null)
        {
            LogManager.Instance.LogEvent("PracticeTrialStart", new Dictionary<string, string>
        {
            {"TrialNumber", (trialNumber + 1).ToString()},
            {"IsPractice", isPractice.ToString()},
            {"Timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}
        });
        }
    }

    public void LogTrialEnd(int trialNumber, bool rewardCollected)
    {
        if (LogManager.Instance != null)
        {
            LogManager.Instance.LogEvent("PracticeTrialEnd", new Dictionary<string, string>
        {
            {"TrialNumber", trialNumber.ToString()},
            {"RewardCollected", rewardCollected.ToString()},
            {"Timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}
        });
        }
    }
    #endregion
}