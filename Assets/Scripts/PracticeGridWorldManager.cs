using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;

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

    private void HandleTimerExpired()
    {
        // Automatically end the trial when timer expires
        // EndPracticeGridWorldTrial(false);
        EndTrial(false);
    }


    public void EndTrial(bool rewardCollected)
    {
        // Get the transaction ID that was stored in OnDecisionMade when work was chosen
        string transactionId = PlayerPrefs.GetString("WorkDecisionTransactionId", "");

        // Check if we've already processed this work decision
        int isProcessed = PlayerPrefs.GetInt("WorkDecisionProcessed", 0);

        // Add explicit logging for debugging
        Debug.Log($"[GRIDWORLD-{transactionId}] End Trial called. Reward Collected: {rewardCollected}, IsProcessed: {isProcessed}");

        // First check if this is even a Work decision
        bool isWorkDecision = PlayerPrefs.GetInt("IsWorkDecision", 1) == 1;
        if (!isWorkDecision)
        {
            Debug.LogWarning($"[GRIDWORLD-{transactionId}] EndTrial called for a Skip decision - this should never happen! Skipping advancement.");
            return;
        }

        // If already processed, prevent double processing
        if (isProcessed == 1)
        {
            Debug.Log($"[GRIDWORLD-{transactionId}] This work decision has already been processed. Preventing duplicate advancement.");
            return;
        }

        // If trial is already not active, prevent double processing
        if (!isTrialActive)
        {
            Debug.Log($"[GRIDWORLD-{transactionId}] Trial already ended, preventing double processing");
            return;
        }

        // Mark trial as inactive IMMEDIATELY to prevent double processing
        isTrialActive = false;

        // CRITICAL: Mark this work decision as processed
        PlayerPrefs.SetInt("WorkDecisionProcessed", 1);
        PlayerPrefs.Save();

        // Log trial end
        int currentTrial = practiceManager?.GetCurrentPracticeTrialIndex() ?? 0;
        LogTrialEnd(currentTrial, rewardCollected);

        // If reward is collected but timer hasn't expired, just mark it
        if (rewardCollected && countdownTimer != null && countdownTimer.TimeLeft > 0)
        {
            // Disable further reward collection but don't end trial yet
            if (rewardSpawner != null)
            {
                rewardSpawner.DisableRewardCollection();
            }
        }

        // Stop the countdown timer
        if (countdownTimer != null)
        {
            countdownTimer.StopTimer();
            countdownTimer.OnTimerExpired -= HandleTimerExpired;
        }

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

        // Store the transaction ID for verification
        PlayerPrefs.SetString("GridWorldOutcomeTransactionId", transactionId);
        PlayerPrefs.Save();

        // IMPORTANT: Work trial, so isSkip should be false - use a delay to prevent race conditions
        if (practiceManager != null)
        {
            // CRITICAL FIX: Increased delay from 1.0f to 1.5f for WebGL compatibility
            StartCoroutine(CallHandleGridWorldOutcomeWithDelay(false, transactionId, 1.5f));
        }
        else
        {
            Debug.LogError($"[GRIDWORLD-{transactionId}] PracticeManager is null when ending Practice GridWorld Trial!");
        }

        // Ensure score persists
        if (PracticeScoreManager.Instance != null)
        {
            Debug.Log($"[GRIDWORLD-{transactionId}] Current Practice Score at EndTrial: {PracticeScoreManager.Instance.GetCurrentScore()}");
        }
    }

    private IEnumerator CallHandleGridWorldOutcomeWithDelay(bool isSkip, string transactionId, float delay)
    {
        yield return new WaitForSeconds(delay);

        // Log to confirm we're calling with the right parameters
        Debug.Log($"Calling HandleGridWorldOutcome after delay. isSkip: {isSkip}, transactionId: {transactionId}");

        // Check again if this transaction has been processed
        int isProcessed = PlayerPrefs.GetInt("WorkDecisionProcessed", 0);
        if (isProcessed != 1)
        {
            Debug.LogWarning($"WorkDecisionProcessed flag is not set when trying to call HandleGridWorldOutcome. This could cause double processing.");
        }

        if (practiceManager != null)
        {
            practiceManager.HandleGridWorldOutcome(isSkip, transactionId);
        }
        else
        {
            Debug.LogError("PracticeManager is null in delayed call!");
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