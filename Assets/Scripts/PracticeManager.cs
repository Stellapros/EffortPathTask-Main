using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;


public class PracticeManager : MonoBehaviour
{
    public static PracticeManager Instance { get; private set; }

    [Header("Practice Configuration")]
    [SerializeField] private string decisionPhaseScene = "PracticeDecisionPhase";
    [SerializeField] private string getReadyCheckScene = "GetReadyCheck";
    [SerializeField] private string blockInstructionScene = "PracticeBlockInstruction";

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI instructionText;
    private bool sceneInitialized = false;

    [Header("Sprite Configuration")]
    [SerializeField] private Sprite appleSprite; // Sprite for effort level 1
    [SerializeField] private Sprite grapesSprite; // Sprite for effort level 3
    [SerializeField] private Sprite watermelonSprite; // Sprite for effort level 5


    public enum PracticeBlockType
    {
        EqualRatio,
        HighLowRatio,
        LowHighRatio
    }

    // Trial states
    private enum PracticeTrialState
    {
        DecisionPhase,
        GridWorld,
        Completed
    }

    [Serializable]
    public class TrialDifficulty
    {
        public int effortLevel;
        public float rewardValue;
        public Sprite rewardSprite;
    }

    [Serializable]
    public class PracticeTrial
    {
        public int effortLevel;
        public float rewardValue;
        public Sprite rewardSprite;
        public bool wasSkipped;
        public bool wasAttempted;
    }

    private List<TrialDifficulty> trialDifficulties;
    private List<PracticeTrial> practiceTrials = new List<PracticeTrial>();
    private int currentPracticeTrialIndex = -1;
    private int practiceAttempts = 0;
    private const int MaxPracticeAttempts = 2;
    public event Action OnPracticeCompleted;

    [Header("Blocks")]
    // Track current practice block and timing
    private PracticeBlockType currentPracticeBlock = PracticeBlockType.EqualRatio;
    private float blockStartTime = 0f;
    private float blockDuration = 120f; // 2 minutes per block
    private int practiceBlockIndex = 0;
    private bool isAdvancingTrial = false;
    private List<PracticeBlockType> practiceBlockSequence;
    private bool blockTimeExpired = false;
    private bool isShowingBlockInstructions = false;
    private bool useExperimentManagerDistribution = true;
    private bool updatesPaused = false;

    [Header("Cursor Settings")]
    [SerializeField] private bool keepCursorHidden = true;
    private bool cursorStateOverridden = false;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Ensure the manager persists across scenes
            if (keepCursorHidden)
            {
                HideCursor();
            }
            InitializePracticeManager(); // Initialize trials and state
        }
        else if (Instance != this)
        {
            Debug.LogWarning("Duplicate PracticeManager detected. Destroying duplicate.");
            Destroy(gameObject); // Destroy duplicate instances
        }

        // Show cursor for this form scene
        // ShowCursor();
        // ForceResetCursorState();
        Debug.Log($"PracticeManager Awake - PracticeAttempts: {PlayerPrefs.GetInt("PracticeAttempts", -1)}, NeedsPracticeRetry: {PlayerPrefs.GetInt("NeedsPracticeRetry", -1)}");
    }

    private void Update()
    {
        CheckPlayerPrefsSynchronization();

        // If updates are paused, don't process anything
        if (updatesPaused)
        {
            return;
        }

        // Always check for retry status at a regular interval
        if (Time.frameCount % 60 == 0) // Check once per second
        {
            bool isRetryAttempt = IsRetryAttempt();
            if (isRetryAttempt && SceneManager.GetActiveScene().name == "PracticePhase")
            {
                // Debug.Log("RETRY STATUS CHECK: Retry detected, ensuring input handling is enabled");
                sceneInitialized = true;
            }
        }

        // DIRECT KEY HANDLING: Always process space/return key in retry scenarios
        if (Input.GetKeyDown(KeyCode.Space))
        {
            bool isRetryAttempt = IsRetryAttempt();
            if (isRetryAttempt && SceneManager.GetActiveScene().name == "PracticePhase")
            {
                // Debug.Log("RETRY KEY DETECTED: Forcing practice mode start");
                StartPracticeMode();
                return;
            }
            else if (sceneInitialized)
            {
                StartPracticeMode();
            }
        }

        // If showing instructions, don't count time towards block duration
        if (isShowingBlockInstructions)
        {
            // Adjust block start time to account for instruction display time
            blockStartTime += Time.deltaTime;
            return;
        }

        // Modified: Check if block timing should be updated - always check during practice regardless of retry status
        if (PlayerPrefs.GetInt("IsPracticeTrial", 0) == 1)
        {
            UpdatePracticeBlockState();
        }

        // In the Update method where you check for retry status
        if (Time.frameCount % 60 == 0) // Once per second check
        {
            // Direct check of PlayerPrefs values
            int currentAttempts = PlayerPrefs.GetInt("PracticeAttempts", 0);
            int needsRetry = PlayerPrefs.GetInt("NeedsPracticeRetry", 0);

            if (currentAttempts > 0 || needsRetry == 1)
            {
                Debug.Log($"CRITICAL: Retry detected in Update but space not enabled. Forcing space button.");
                // spaceButtonEnabled = true;
                sceneInitialized = true;

                // Try to update text if accessible
                var textComponent = GameObject.Find("InstructionText")?.GetComponent<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.text = "Press 'Space' to continue. Use ↑ ↓ to scroll";
                }
            }
        }

        // Add this inside the Update method
        if (Time.frameCount % 300 == 0 && PlayerPrefs.GetInt("IsPracticeTrial", 0) == 1)
        {
            // Every 5 seconds, verify block timing is working
            bool blockStarted = PlayerPrefs.GetInt("BlockOfficiallyStarted", 0) == 1;
            float storedStartTime = PlayerPrefs.GetFloat("BlockStartTime", 0);

            if (blockStarted && storedStartTime > 0)
            {
                // float elapsedTime = Time.time - storedStartTime;
                float elapsedTime = Time.realtimeSinceStartup - storedStartTime;
                Debug.Log($"TIMING CHECK: Block has been running for {elapsedTime:F1}s / {blockDuration}s");
            }
        }
    }

    private bool IsRetryAttempt()
    {
        // Force synchronize PlayerPrefs first
        PlayerPrefs.Save();

        // Check all possible sources of retry information
        int failedAttempts = PlayerPrefs.GetInt("PracticeAttempts", 0);
        bool needsRetry = PlayerPrefs.GetInt("NeedsPracticeRetry", 0) == 1;
        bool failedCheck = PlayerPrefs.GetInt("FailedCheck", 0) == 1;
        bool explicitRetry = PlayerPrefs.GetInt("IsExplicitRetryAttempt", 0) == 1;

        // Use OR logic between all conditions
        bool isRetry = (failedAttempts > 0 || needsRetry || failedCheck || explicitRetry);
        return isRetry;
    }

    private void ResetBlockTimingState()
    {
        // Reset in-memory timing variables
        blockStartTime = Time.realtimeSinceStartup;
        blockTimeExpired = false;

        // CRITICAL: Make sure BlockOfficiallyStarted is set to 0 (not started)
        PlayerPrefs.SetInt("BlockOfficiallyStarted", 0);
        PlayerPrefs.SetFloat("BlockStartTime", 0f);  // Set to 0 to ensure a fresh start
        PlayerPrefs.Save();

        Debug.Log("TIMING RESET: Block timing state fully reset");
    }

    public void PauseUpdates(bool pause)
    {
        updatesPaused = pause;
        Debug.Log($"PracticeManager updates {(pause ? "paused" : "resumed")}");
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        // Only clean up if we're not in a retry attempt
        if (PlayerPrefs.GetInt("RetryInProgress", 0) == 0)
        {
            // Clean up all possible retry-related PlayerPrefs
            PlayerPrefs.DeleteKey("PracticeAttempts");
            PlayerPrefs.DeleteKey("NeedsPracticeRetry");
            PlayerPrefs.DeleteKey("IsExplicitRetryAttempt");
            PlayerPrefs.DeleteKey("RetryInProgress");
            PlayerPrefs.DeleteKey("BlockOfficiallyStarted");
            PlayerPrefs.DeleteKey("BlockStartTime");
        }

        // Always clean up these keys
        PlayerPrefs.DeleteKey("IsPracticeTrial");
        PlayerPrefs.DeleteKey("CurrentPracticeTrialIndex");
        PlayerPrefs.DeleteKey("CurrentPracticeEffortLevel");
        PlayerPrefs.DeleteKey("CurrentPracticeBlockIndex");
        PlayerPrefs.DeleteKey("CurrentPracticeBlockType");

        HideCursor();
    }

    private void HideCursor()
    {
        if (keepCursorHidden && !cursorStateOverridden)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            Debug.Log("Cursor hidden and locked");
        }
    }

    public void InitializePracticeManager()
    {
        // Check if we're resuming from a check
        if (PlayerPrefs.GetInt("ResumeFromCheck", 0) == 1)
        {
            ResumeFromCheck();
            return;
        }

        // Rest of your existing initialization code
        useExperimentManagerDistribution = true;

        ValidateSprites();
        PrepareDifficulties();
        GeneratePracticeTrials();
        InitializePracticeBlocks(); // Initialize the practice blocks
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Retrieve the trial index from PlayerPrefs or set it to 0 if not found
        currentPracticeTrialIndex = PlayerPrefs.GetInt("CurrentPracticeTrialIndex", 0);

        // Set practice mode for the current block
        UpdateGridManagerForCurrentBlock();
    }

    private void InitializePracticeBlocks()
    {
        // Reset block timing variables — important: DO NOT start timing yet
        blockStartTime = 0f;
        blockTimeExpired = false;

        PlayerPrefs.SetInt("BlockOfficiallyStarted", 0); // Not officially started yet
        PlayerPrefs.SetFloat("BlockStartTime", 0f);
        PlayerPrefs.Save();

        // Debug.Log("InitializePracticeBlocks: Block timing reset (NOT started yet)");

        // Set up practice block sequence
        practiceBlockSequence = new List<PracticeBlockType> { PracticeBlockType.EqualRatio };

        // Create a list of the other block types
        List<PracticeBlockType> remainingBlocks = new List<PracticeBlockType>
    {
        PracticeBlockType.HighLowRatio,
        PracticeBlockType.LowHighRatio
    };

        // Randomize order
        if (UnityEngine.Random.value > 0.5f)
        {
            PracticeBlockType temp = remainingBlocks[0];
            remainingBlocks[0] = remainingBlocks[1];
            remainingBlocks[1] = temp;
        }

        // Add randomized blocks to sequence
        practiceBlockSequence.AddRange(remainingBlocks);

        practiceBlockIndex = 0;
        currentPracticeBlock = practiceBlockSequence[practiceBlockIndex];

        // Save block index and type
        PlayerPrefs.SetInt("CurrentPracticeBlockIndex", practiceBlockIndex);
        PlayerPrefs.SetString("CurrentPracticeBlockType", currentPracticeBlock.ToString());
        PlayerPrefs.Save();
    }

    private void UpdatePracticeBlockState()
    {
        // Check if we're in practice mode
        if (PlayerPrefs.GetInt("IsPracticeTrial", 0) != 1)
            return;

        bool blockStarted = PlayerPrefs.GetInt("BlockOfficiallyStarted", 0) == 1;
        string currentScene = SceneManager.GetActiveScene().name;

        // Debug status periodically to troubleshoot retry issues
        if (Time.frameCount % 60 == 0) // Check once per second
        {
            bool isRetry = IsRetryAttempt();
            if (isRetry)
            {
                // Debug.Log($"RETRY STATUS: Block timing check - blockStarted={blockStarted}, " +
                //           $"BlockStartTime={PlayerPrefs.GetFloat("BlockStartTime", 0f)}, " +
                //           $"Current time={Time.realtimeSinceStartup}, " +
                //           $"Elapsed={Time.realtimeSinceStartup - PlayerPrefs.GetFloat("BlockStartTime", 0f)}s");
            }
        }

        if (!blockStarted)
        {
            // Block hasn't officially started yet, do nothing
            return;
        }

        float storedStartTime = PlayerPrefs.GetFloat("BlockStartTime", 0f);

        // CRITICAL: Validate storedStartTime to catch invalid values
        if (storedStartTime <= 0f)
        {
            Debug.LogWarning("TIMING ERROR: Invalid stored block start time detected. Fixing...");
            storedStartTime = Time.realtimeSinceStartup; // Reset to current realtime as fallback
            PlayerPrefs.SetFloat("BlockStartTime", storedStartTime);
            PlayerPrefs.Save();
        }

        // CRITICAL CHANGE: Always use realtimeSinceStartup consistently
        float timeInBlock = Time.realtimeSinceStartup - storedStartTime;

        if (Time.frameCount % 60 == 0) // Log once per second
        {
            // Debug.Log($"Block timing: {timeInBlock:F1}s / {blockDuration}s. " +
            //           $"blockStartTime={storedStartTime}, currentTime={Time.realtimeSinceStartup}");
        }

        // Force check PlayerPrefs to ensure we're using the latest value
        PlayerPrefs.Save();
        float currentStoredTime = PlayerPrefs.GetFloat("BlockStartTime", 0f);
        if (Math.Abs(currentStoredTime - storedStartTime) > 0.1f)
        {
            // Debug.LogWarning($"TIMING DISCREPANCY: Stored time changed from {storedStartTime} to {currentStoredTime}");
            storedStartTime = currentStoredTime;
            timeInBlock = Time.realtimeSinceStartup - storedStartTime;
        }

        if (timeInBlock >= blockDuration && !blockTimeExpired)
        {
            // Debug.Log($"BLOCK EXPIRED: Block duration reached ({blockDuration}s). Advancing block.");
            blockTimeExpired = true;
            StartCoroutine(AdvanceBlockAfterDelay(0.5f));
        }
    }

    private IEnumerator AdvanceBlockAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Debug.Log($"ADVANCING BLOCK: Current block index {practiceBlockIndex} of {practiceBlockSequence.Count - 1}");

        // First check if we've reached the maximum block index
        if (practiceBlockIndex >= practiceBlockSequence.Count - 1)
        {
            // Debug.Log("All practice blocks completed. Ending practice mode.");
            EndPracticeMode();
            yield break;
        }

        // Check if this is the first block completion
        if (practiceBlockIndex == 0)
        {
            // Debug.Log("First practice block completed. Redirecting to GetReadyCheck");
            // Save current block state for resuming
            PlayerPrefs.SetInt("PracticeBlocksCompleted", 1);
            PlayerPrefs.SetInt("ResumeFromCheck", 1);
            PlayerPrefs.Save();

            // Reset the flag
            blockTimeExpired = false;

            // Load GetReadyCheck scene
            SceneManager.LoadScene(getReadyCheckScene);
            yield break;
        }

        // Otherwise, advance to the next block
        // Debug.Log($"Advancing to next block: {practiceBlockIndex + 1}");
        practiceBlockIndex++;
        currentPracticeBlock = practiceBlockSequence[practiceBlockIndex];

        // Reset block timing state
        ResetBlockTimingState();

        // Save current block info
        PlayerPrefs.SetInt("CurrentPracticeBlockIndex", practiceBlockIndex);
        PlayerPrefs.SetString("CurrentPracticeBlockType", currentPracticeBlock.ToString());
        PlayerPrefs.Save();

        // Update GridManager with the new block type
        UpdateGridManagerForCurrentBlock();

        // Generate new trials for this block
        GeneratePracticeTrials();

        // Reset trial index to start fresh with the new distribution
        SetCurrentPracticeTrialIndex(0);

        // Load block instruction scene
        SceneManager.LoadScene(blockInstructionScene);
    }


    private GridManager _registeredGridManager;

    public void RegisterGridManager(GridManager gridManager)
    {
        _registeredGridManager = gridManager;

        // Apply practice mode settings immediately if needed
        if (PlayerPrefs.GetInt("IsPracticeTrial", 0) == 1)
        {
            UpdateGridManagerForCurrentBlock();
        }
    }

    private void UpdateGridManagerForCurrentBlock()
    {
        // GridManager gridManager = FindAnyObjectByType<GridManager>();
        GridManager gridManager = _registeredGridManager ?? FindAnyObjectByType<GridManager>();
        if (gridManager != null)
        {
            gridManager.SetPracticeMode(true); // Always set practice mode to true

            // Debug current block before changing
            // Debug.Log($"PracticeManager updating GridManager with block type: {currentPracticeBlock}");

            ExperimentManager.BlockType blockType;
            switch (currentPracticeBlock)
            {
                case PracticeBlockType.EqualRatio:
                    blockType = ExperimentManager.BlockType.EqualRatio;
                    break;
                case PracticeBlockType.HighLowRatio:
                    blockType = ExperimentManager.BlockType.HighLowRatio;
                    break;
                case PracticeBlockType.LowHighRatio:
                    blockType = ExperimentManager.BlockType.LowHighRatio;
                    break;
                default:
                    blockType = ExperimentManager.BlockType.EqualRatio;
                    break;
            }

            // Always apply the block type, even if it's the same
            gridManager.SetBlockType(blockType);
        }
        else
        {
            // Debug.LogWarning("GridManager not found when attempting to update block type");
        }
    }

    private void OnEnable()
    {
        // Debug.Log("Practice Phase Scene Enabled");
    }

    private void OnDisable()
    {
        // Remove scene loading event listener
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void VerifyBlockTimingOnSceneLoad(Scene scene)
    {
        if (PlayerPrefs.GetInt("IsPracticeTrial", 0) != 1)
            return;

        bool blockStarted = PlayerPrefs.GetInt("BlockOfficiallyStarted", 0) == 1;
        float storedTime = PlayerPrefs.GetFloat("BlockStartTime", 0f);

        // If we're in the decision phase and block should be started but timing looks wrong
        if (scene.name == "PracticeDecisionPhase" && blockStarted)
        {
            if (storedTime <= 0f || (Time.realtimeSinceStartup - storedTime) < 0f)
            {
                Debug.LogWarning("TIMING REPAIR: Invalid block timing detected, fixing...");
                storedTime = Time.realtimeSinceStartup;
                PlayerPrefs.SetFloat("BlockStartTime", storedTime);
                blockStartTime = storedTime;
                PlayerPrefs.Save();
            }

            // Check if we've exceeded the block duration
            float elapsed = Time.realtimeSinceStartup - storedTime;
            if (elapsed >= blockDuration && !blockTimeExpired)
            {
                Debug.LogWarning("TIMING REPAIR: Block duration already exceeded. Marking as expired.");
                blockTimeExpired = true;
                StartCoroutine(AdvanceBlockAfterDelay(1.0f));
            }
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"Scene loaded: {scene.name}");
        ForceResetCursorState();

        // CRITICAL: Add block timing verification on every scene load
        VerifyBlockTimingOnSceneLoad(scene);

        // Check retry status first thing
        bool isRetryAttempt = IsRetryAttempt();

        // FOR WEBGL: Always validate trial index at scene transitions
        if (PlayerPrefs.GetInt("IsPracticeTrial", 0) == 1)
        {
            // Get the latest value from PlayerPrefs
            int storedIndex = PlayerPrefs.GetInt("CurrentPracticeTrialIndex", 0);

            // If memory and PlayerPrefs are out of sync, use PlayerPrefs value
            if (currentPracticeTrialIndex != storedIndex)
            {
                Debug.LogWarning($"CRITICAL: Trial index mismatch at scene load. " +
                                  $"Memory={currentPracticeTrialIndex}, PlayerPrefs={storedIndex}. " +
                                  $"Using PlayerPrefs value.");
                currentPracticeTrialIndex = storedIndex;
            }

            // If we're in the decision phase, ensure we have valid trials
            if (scene.name == "PracticeDecisionPhase")
            {
                EnsurePracticeTrialsExist();

                // Final check if the current trial index is valid
                if (currentPracticeTrialIndex < 0 ||
                    practiceTrials == null ||
                    currentPracticeTrialIndex >= practiceTrials.Count)
                {
                    Debug.LogError($"Invalid trial index {currentPracticeTrialIndex} at scene load.");
                    // Reset to a safe value
                    SetCurrentPracticeTrialIndex(0);
                }

                // Log the current trial for debugging
                Debug.Log($"Decision phase loaded with trial index: {currentPracticeTrialIndex}");
            }
        }

        // IMPORTANT: Always handle block timing initialization here for practice decision phase
        if (scene.name == "PracticeDecisionPhase" && PlayerPrefs.GetInt("IsPracticeTrial", 0) == 1)
        {
            bool blockStarted = PlayerPrefs.GetInt("BlockOfficiallyStarted", 0) == 1;

            // Only set timing if block is already marked as started
            if (blockStarted)
            {
                // Check for valid BlockStartTime
                float storedTime = PlayerPrefs.GetFloat("BlockStartTime", 0f);
                if (storedTime <= 0f)
                {
                    // Fix invalid stored time
                    Debug.LogWarning("TIMING FIX: Invalid BlockStartTime detected in decision phase. Fixing...");
                    storedTime = Time.realtimeSinceStartup;
                    PlayerPrefs.SetFloat("BlockStartTime", storedTime);
                    blockStartTime = storedTime;
                    PlayerPrefs.Save();
                }

                // Log elapsed time since block start
                float elapsed = Time.realtimeSinceStartup - storedTime;
                // Debug.Log($"DECISION PHASE: Block already started. Elapsed time: {elapsed:F1}s / {blockDuration}s");

                // CRITICAL: Force expire if we've already exceeded the time
                if (elapsed >= blockDuration && !blockTimeExpired)
                {
                    // Debug.Log("CRITICAL: Block duration already exceeded on scene load. Forcing expiration.");
                    blockTimeExpired = true;
                    StartCoroutine(AdvanceBlockAfterDelay(1.0f));
                }
            }
        }

        // Ensure trials exist regardless of scene
        EnsurePracticeTrialsExist();

        // Update GridManager for current practice block if we're in practice mode
        if (PlayerPrefs.GetInt("IsPracticeTrial", 0) == 1 &&
            (scene.name == "PracticeGridWorld" || scene.name == "PracticeDecisionPhase"))
        {
            UpdateGridManagerForCurrentBlock();
        }

        // Check for failed attempts and ensure we're properly set up
        if (isRetryAttempt && scene.name == "PracticePhase")
        {
            // Debug.Log($"Detected retry attempt, ensuring input is enabled");
            // Force enable input immediately for retry attempts
            sceneInitialized = true;
        }

        // Only initialize if we're in the PracticePhase scene
        if (scene.name == "PracticePhase")
        {
            // Debug.Log("PracticePhase scene loaded - Initializing scene");
            StartCoroutine(InitializeSceneAfterLoad());
            ForceResetCursorState(); // Apply again to be sure
        }
        else
        {
            // Don't reset sceneInitialized for retry attempts
            if (!isRetryAttempt)
            {
                sceneInitialized = false;
            }
        }
    }

    public void EnsurePracticeTrialsExist()
    {
        if (practiceTrials == null || practiceTrials.Count == 0)
        {
            // Debug.Log("Ensuring practice trials exist - regenerating trials");
            PrepareDifficulties();
            GeneratePracticeTrials();
        }
    }

    private IEnumerator InitializeSceneAfterLoad()
    {
        // Debug.Log("Starting robust scene initialization...");

        // CRITICAL: Always check retry status first thing and force PlayerPrefs sync
        PlayerPrefs.Save();
        bool isRetryAttempt = IsRetryAttempt();

        // CRITICAL: Always set sceneInitialized to true for retry attempts immediately
        if (isRetryAttempt)
        {
            sceneInitialized = true;
            // Debug.Log("RETRY DETECTED: Forcing scene initialization");
        }

        // Wait a moment for UI to initialize
        yield return new WaitForSeconds(0.2f);

        // Try to find instruction text component
        instructionText = GameObject.Find("InstructionText")?.GetComponent<TextMeshProUGUI>();

        if (instructionText != null)
        {
            instructionText.text = "Press 'Space' to continue. Use ↑ ↓ to scroll";
            sceneInitialized = true;
        }
        else if (!sceneInitialized) // Only set this if not already set for retry
        {
            Debug.LogWarning("InstructionText not found. Enabling space key anyway for robustness.");
            sceneInitialized = true;
        }

        // Additional safety check for retry attempts
        if (isRetryAttempt && !sceneInitialized)
        {
            // Debug.Log("CRITICAL FAILSAFE: Retry attempt but scene not initialized - forcing initialization");
            sceneInitialized = true;
        }

        ForceResetCursorState();

        // Add a direct input listener for retry attempts
        if (isRetryAttempt)
        {
            StartCoroutine(EmergencyRetryMonitor());
        }
    }

    private IEnumerator EmergencyRetryMonitor()
    {
        // Debug.Log("EMERGENCY: Starting persistent input monitor for retry attempts");
        string currentScene = SceneManager.GetActiveScene().name;

        // Continue monitoring until scene changes
        while (SceneManager.GetActiveScene().name == currentScene)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.E))
            {
                // Debug.Log("EMERGENCY: Detected key press through direct monitor");
                StartPracticeMode();
                yield break;
            }

            yield return null; // Wait for next frame
        }
    }

    public void ResetPracticeForNewAttempt()
    {
        // Debug.Log("Resetting practice for new attempt");

        // Make sure we increment the attempt counter and save it
        int attempts = PlayerPrefs.GetInt("PracticeAttempts", 0) + 1;
        PlayerPrefs.SetInt("NeedsPracticeRetry", 1);
        PlayerPrefs.SetInt("PracticeAttempts", attempts);
        PlayerPrefs.SetInt("IsExplicitRetryAttempt", 1);

        // CRITICAL: Reset block timing state with explicit values
        blockStartTime = 0f;  // Set to 0 to ensure fresh start
        blockTimeExpired = false;
        PlayerPrefs.SetInt("BlockOfficiallyStarted", 0);
        PlayerPrefs.SetFloat("BlockStartTime", 0f);
        PlayerPrefs.Save();
        // Debug.Log("RETRY: Block timing state explicitly reset to zero");

        // Reset block index and reinitialize
        practiceBlockIndex = 0;
        currentPracticeBlock = PracticeBlockType.EqualRatio;

        // Reset trial tracking
        SetCurrentPracticeTrialIndex(0);

        // Generate trials using the same method as first attempt
        GeneratePracticeTrials();

        // Reset PlayerPrefs values
        PlayerPrefs.SetInt("IsPracticeTrial", 1);
        PlayerPrefs.SetInt("CurrentPracticeTrialIndex", 0);
        PlayerPrefs.SetInt("CurrentPracticeBlockIndex", 0);
        PlayerPrefs.SetString("CurrentPracticeBlockType", PracticeBlockType.EqualRatio.ToString());

        // CRITICAL: Force save after each key change
        PlayerPrefs.Save();

        // Reinitialize blocks (with proper time reset)
        InitializePracticeBlocks();

        // Reset cursor state
        ForceResetCursorState();

        // Re-register scene loaded event
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Load the appropriate starting scene
        SceneManager.LoadScene("GetReadyPractice");

        // Debug.Log("Practice state reset and scene reloaded.");
    }

    private void ForceResetCursorState()
    {
        if (!keepCursorHidden)
        {
            // Only show cursor if we're not keeping it hidden
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            Debug.Log("Cursor state reset - visible and unlocked");
        }
        else
        {
            // Otherwise maintain hidden state
            HideCursor();
        }
    }

    private void ValidateSprites()
    {
        // Check if sprites are assigned
        if (appleSprite == null || grapesSprite == null || watermelonSprite == null)
        {
            Debug.LogError("One or more reward sprites are not assigned in the inspector!");
        }
    }

    private void PrepareDifficulties()
    {
        // Create trial difficulties with specific sprites for each effort level
        trialDifficulties = new List<TrialDifficulty>
        {
            // Effortlevel 1: 4 trials with Apple sprite
            new TrialDifficulty { effortLevel = 1, rewardValue = 10f, rewardSprite = appleSprite },
            new TrialDifficulty { effortLevel = 1, rewardValue = 10f, rewardSprite = appleSprite },
            new TrialDifficulty { effortLevel = 1, rewardValue = 10f, rewardSprite = appleSprite },
            new TrialDifficulty { effortLevel = 1, rewardValue = 10f, rewardSprite = appleSprite },
            // new TrialDifficulty { effortLevel = 1, rewardValue = 10f, rewardSprite = appleSprite },
            // new TrialDifficulty { effortLevel = 1, rewardValue = 10f, rewardSprite = appleSprite },

            // Effortlevel 2: 4 trials with Grapes sprite
            new TrialDifficulty { effortLevel = 2, rewardValue = 10f, rewardSprite = grapesSprite },
            new TrialDifficulty { effortLevel = 2, rewardValue = 10f, rewardSprite = grapesSprite },
            new TrialDifficulty { effortLevel = 2, rewardValue = 10f, rewardSprite = grapesSprite },
            new TrialDifficulty { effortLevel = 2, rewardValue = 10f, rewardSprite = grapesSprite },

            // Effortlevel 2: 4 trials with Watermelon sprite
            new TrialDifficulty { effortLevel = 3, rewardValue = 10f, rewardSprite = watermelonSprite },
            new TrialDifficulty { effortLevel = 3, rewardValue = 10f, rewardSprite = watermelonSprite },
            new TrialDifficulty { effortLevel = 3, rewardValue = 10f, rewardSprite = watermelonSprite },
            new TrialDifficulty { effortLevel = 3, rewardValue = 10f, rewardSprite = watermelonSprite },
        };
    }

    private void GeneratePracticeTrials()
    {
        practiceTrials.Clear();

        // Generate plenty of trials (more than would be needed for the block duration)
        // This ensures we never run out of trials during a block
        int generousTrialCount = 30; // Much more than would be completed in blockDuration

        if (useExperimentManagerDistribution && ExperimentManager.Instance != null)
        {
            // Get current block type from the practice block sequence
            ExperimentManager.BlockType blockType = ConvertToExperimentBlockType(currentPracticeBlock);

            // Tell ExperimentManager to reset its ratios for this block type
            ExperimentManager.Instance.ResetBlockRatiosForType(blockType);

            // Get the distribution from ExperimentManager
            Dictionary<int, int> blockRatios = ExperimentManager.Instance.GetCurrentBlockRatios();

            if (blockRatios != null && blockRatios.Count > 0)
            {
                // Debug.Log($"Using ExperimentManager distribution for practice block {currentPracticeBlock}");

                // Create a weighted list based on the ratios
                List<int> weightedEffortLevels = new List<int>();
                foreach (var kvp in blockRatios)
                {
                    int effortLevel = kvp.Key;
                    int weight = kvp.Value;

                    for (int i = 0; i < weight; i++)
                    {
                        weightedEffortLevels.Add(effortLevel);
                    }
                }

                // Shuffle the weighted list
                for (int i = weightedEffortLevels.Count - 1; i > 0; i--)
                {
                    int j = UnityEngine.Random.Range(0, i + 1);
                    int temp = weightedEffortLevels[i];
                    weightedEffortLevels[i] = weightedEffortLevels[j];
                    weightedEffortLevels[j] = temp;
                }

                // Generate trials using the weighted list
                for (int i = 0; i < generousTrialCount; i++)
                {
                    int effortLevel = weightedEffortLevels[i % weightedEffortLevels.Count];
                    Sprite rewardSprite = GetSpriteForEffortLevel(effortLevel);

                    practiceTrials.Add(new PracticeTrial
                    {
                        effortLevel = effortLevel,
                        rewardValue = 10f,
                        rewardSprite = rewardSprite,
                        wasAttempted = false
                    });
                }
            }
            else
            {
                Debug.LogWarning("Failed to get block ratios from ExperimentManager, using default distribution");
                GenerateDefaultTrials(generousTrialCount);
            }
        }
        else
        {
            GenerateDefaultTrials(generousTrialCount);
        }

        // Debug.Log($"Generated {practiceTrials.Count} practice trials");
    }

    // Method to generate trials using the original approach
    private void GenerateDefaultTrials(int trialsToGenerate)
    {
        // Define trial difficulties with effort levels and sprites
        trialDifficulties = new List<TrialDifficulty>
    {
        new TrialDifficulty { effortLevel = 1, rewardValue = 10f, rewardSprite = appleSprite },
        new TrialDifficulty { effortLevel = 1, rewardValue = 10f, rewardSprite = appleSprite },
        new TrialDifficulty { effortLevel = 2, rewardValue = 10f, rewardSprite = grapesSprite },
        new TrialDifficulty { effortLevel = 2, rewardValue = 10f, rewardSprite = grapesSprite },
        new TrialDifficulty { effortLevel = 3, rewardValue = 10f, rewardSprite = watermelonSprite },
        new TrialDifficulty { effortLevel = 3, rewardValue = 10f, rewardSprite = watermelonSprite },
    };

        // Shuffle and generate trials
        List<TrialDifficulty> shuffledDifficulties = new List<TrialDifficulty>(trialDifficulties);
        for (int i = shuffledDifficulties.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            TrialDifficulty temp = shuffledDifficulties[i];
            shuffledDifficulties[i] = shuffledDifficulties[j];
            shuffledDifficulties[j] = temp;
        }

        for (int i = 0; i < trialsToGenerate; i++)
        {
            TrialDifficulty difficulty = shuffledDifficulties[i % shuffledDifficulties.Count];
            practiceTrials.Add(new PracticeTrial
            {
                effortLevel = difficulty.effortLevel,
                rewardValue = difficulty.rewardValue,
                rewardSprite = difficulty.rewardSprite,
                wasAttempted = false
            });
        }
    }

    // Convert our practice block type to ExperimentManager's block type
    private ExperimentManager.BlockType ConvertToExperimentBlockType(PracticeBlockType practiceBlockType)
    {
        // Debug.Log($"Converting practice block type: {practiceBlockType}");

        switch (practiceBlockType)
        {
            case PracticeBlockType.HighLowRatio:
                return ExperimentManager.BlockType.HighLowRatio;
            case PracticeBlockType.LowHighRatio:
                return ExperimentManager.BlockType.LowHighRatio;
            case PracticeBlockType.EqualRatio:
                return ExperimentManager.BlockType.EqualRatio; // Add this case
            default:
                return ExperimentManager.BlockType.EqualRatio; // Changed default to Normal
        }
    }
    public void StartPracticeMode()
    {
        Debug.Log($"StartPracticeMode called! Practice attempts: {PlayerPrefs.GetInt("PracticeAttempts", 0)}");

        try
        {
            InitializePracticeBlocks(); // Resets block settings

            // After reinitializing blocks, reset practice block index and trial index
            practiceBlockIndex = 0;
            currentPracticeBlock = practiceBlockSequence[practiceBlockIndex];

            PlayerPrefs.SetInt("CurrentPracticeBlockIndex", practiceBlockIndex);
            PlayerPrefs.SetString("CurrentPracticeBlockType", currentPracticeBlock.ToString());
            PlayerPrefs.SetInt("IsPracticeTrial", 1); // Mark that this is a practice
            PlayerPrefs.SetInt("BlockOfficiallyStarted", 0); // Important: Do NOT set block officially started yet!
            PlayerPrefs.SetFloat("BlockStartTime", 0f);
            PlayerPrefs.Save();

            // Set trial index to 0 and regenerate practice trials
            SetCurrentPracticeTrialIndex(0);
            GeneratePracticeTrials();

            // Reset score if needed
            PracticeScoreManager.Instance?.ResetScore();

            if (practiceAttempts >= MaxPracticeAttempts)
            {
                // Debug.Log("Maximum practice attempts reached. Ending experiment.");
                SceneManager.LoadScene("EndExperiment");
                return;
            }

            LogPracticeTrialStart(currentPracticeTrialIndex);

            // Debug.Log($"Starting practice attempt {practiceAttempts + 1}, loading PracticeBlockInstruction scene");
            SceneManager.LoadScene(blockInstructionScene);
        }
        catch (Exception)
        {
            // Debug.LogError($"CRITICAL ERROR in StartPracticeMode: {ex.Message}");
            SceneManager.LoadScene(blockInstructionScene);
        }
    }


    public void HandleGridWorldOutcome(bool isSkip, string transactionId = null)
    {
        // Generate transaction ID if none provided (for backward compatibility)
        if (string.IsNullOrEmpty(transactionId))
        {
            transactionId = System.Guid.NewGuid().ToString();
        }

        // Add a prefix to all logs for easier debugging
        string logPrefix = $"[HGO-{transactionId.Substring(0, 8)}]";
        Debug.Log($"{logPrefix} Handling GridWorld outcome. IsSkip: {isSkip}");

        // CRITICAL: Use a centralized lock object to prevent concurrent modifications
        lock (typeof(PracticeManager))
        {
            try
            {
                // Validate transaction ID to prevent duplicate processing
                string lastTransactionId = PlayerPrefs.GetString("LastTrialTransactionId", "");

                // If we have a transaction ID and it matches the last one processed, skip processing
                if (!string.IsNullOrEmpty(transactionId) && transactionId == lastTransactionId)
                {
                    Debug.Log($"{logPrefix} Ignoring duplicate transaction");
                    return;
                }

                // Check if we're already advancing a trial - use a more unique key
                string processingKey = "ProcessingTrialAdvancement_" + transactionId.Substring(0, 8);
                if (PlayerPrefs.GetInt(processingKey, 0) == 1)
                {
                    Debug.LogWarning($"{logPrefix} Trial advancement already in progress. Preventing duplicate advancement.");
                    return;
                }

                // Set the processing flag with timeout (5 seconds from now)
                PlayerPrefs.SetInt(processingKey, 1);
                PlayerPrefs.SetString(processingKey + "_timeout", (Time.realtimeSinceStartup + 5.0f).ToString());
                PlayerPrefs.Save();

                // Store this transaction ID to prevent duplicate processing
                PlayerPrefs.SetString("LastTrialTransactionId", transactionId);
                PlayerPrefs.Save();

                // Get the current trial index directly from PlayerPrefs for safety
                int currentIndex = PlayerPrefs.GetInt("CurrentPracticeTrialIndex", 0);

                // Double check with in-memory value
                if (currentIndex != currentPracticeTrialIndex)
                {
                    Debug.LogWarning($"{logPrefix} Index mismatch: PlayerPrefs={currentIndex}, In-memory={currentPracticeTrialIndex}");
                    // Use PlayerPrefs as the source of truth
                    currentPracticeTrialIndex = currentIndex;
                }

                // Log the current index before advancing
                Debug.Log($"{logPrefix} Current trial index before advancing: {currentIndex}");

                // CRITICAL FIX: Calculate next trial index - ONLY INCREMENT BY 1
                int nextIndex = currentIndex + 1;

                // Check if we've reached the end of practice trials
                if (nextIndex >= practiceTrials.Count)
                {
                    Debug.Log($"{logPrefix} End of practice trials reached. Ending practice mode.");

                    // CRITICAL FIX: Set the processing flag to false before ending practice mode
                    PlayerPrefs.DeleteKey(processingKey);
                    PlayerPrefs.DeleteKey(processingKey + "_timeout");
                    PlayerPrefs.Save();

                    // End practice mode - IMPORTANT: This will handle transitions to check questions
                    EndPracticeMode();
                    return;
                }

                Debug.Log($"{logPrefix} Advancing practice trial from {currentIndex} to {nextIndex}");

                // IMPORTANT: Update both the in-memory value AND PlayerPrefs
                currentPracticeTrialIndex = nextIndex;
                PlayerPrefs.SetInt("CurrentPracticeTrialIndex", nextIndex);

                // Store the time when we updated the index for debugging
                PlayerPrefs.SetString("LastTrialUpdateTime", System.DateTime.Now.ToString("HH:mm:ss.fff"));

                // Force save PlayerPrefs immediately
                PlayerPrefs.Save();

                // [WebGL sync code remains unchanged]

                // Extra verification
                int verifiedIndex = PlayerPrefs.GetInt("CurrentPracticeTrialIndex", -1);
                if (verifiedIndex != nextIndex)
                {
                    Debug.LogError($"{logPrefix} Critical error: Index not properly updated. Expected: {nextIndex}, Actual: {verifiedIndex}");
                    // Try one more time with a more aggressive approach
                    currentPracticeTrialIndex = nextIndex; // Ensure in-memory value is correct
                    PlayerPrefs.SetInt("CurrentPracticeTrialIndex", nextIndex);
                    PlayerPrefs.Save();

                    // [WebGL sync code remains unchanged]
                }

                // Log to confirm the update
                Debug.Log($"{logPrefix} Updated trial index in PlayerPrefs: {PlayerPrefs.GetInt("CurrentPracticeTrialIndex", -1)}");

                // Load decision phase with appropriate delay
                StartCoroutine(LoadDecisionPhaseAfterDelay(0.5f, processingKey, logPrefix));
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{logPrefix} Error in HandleGridWorldOutcome: {ex.Message}\n{ex.StackTrace}");

                // Attempt recovery by reloading the decision phase
                StartCoroutine(LoadDecisionPhaseAfterDelay(0.5f, "recovery_" + transactionId, logPrefix));
            }
        }
    }


    private IEnumerator WebGLExtraSync(int targetIndex, string logPrefix)
    {
        // Try multiple times with increasing delays
        for (int i = 0; i < 3; i++)
        {
            yield return new WaitForSeconds(0.1f * (i + 1));

            int currentValue = PlayerPrefs.GetInt("CurrentPracticeTrialIndex", -999);
            if (currentValue != targetIndex)
            {
                Debug.LogWarning($"{logPrefix} WebGL sync attempt {i + 1}: value={currentValue}, target={targetIndex}");
                PlayerPrefs.SetInt("CurrentPracticeTrialIndex", targetIndex);
                PlayerPrefs.Save();

                try
                {
                    Application.ExternalCall("syncfs");
                }
                catch { }
            }
            else
            {
                Debug.Log($"{logPrefix} WebGL sync successful on attempt {i + 1}");
                break;
            }
        }
    }

    private IEnumerator LoadDecisionPhaseAfterDelay(float delay, string processingKey, string logPrefix)
    {
        // Only add a small delay for WebGL platforms
#if UNITY_WEBGL && !UNITY_EDITOR
        yield return new WaitForSeconds(delay);
#else
        // For other platforms, minimal delay
        yield return new WaitForEndOfFrame();
#endif

        // Double check the current index one last time 
        int finalIndex = PlayerPrefs.GetInt("CurrentPracticeTrialIndex", -1);
        Debug.Log($"{logPrefix} Final trial index before scene load: {finalIndex}");

        // Clear the processing flag
        if (!string.IsNullOrEmpty(processingKey))
        {
            PlayerPrefs.DeleteKey(processingKey);
            PlayerPrefs.DeleteKey(processingKey + "_timeout");
        }

        // Clear any leftover processing flags
        PlayerPrefs.SetInt("ProcessingTrialAdvancement", 0);

        // Reset WorkDecisionProcessed flag to ensure fresh state
        PlayerPrefs.SetInt("WorkDecisionProcessed", 0);

        // Force save
        PlayerPrefs.Save();

        // Load the decision phase scene
        SceneManager.LoadScene("PracticeDecisionPhase");
    }



    private void OnApplicationPause(bool pause)
    {
        if (!pause) // When returning from pause
        {
            // Re-read PlayerPrefs values
            int storedIndex = PlayerPrefs.GetInt("CurrentPracticeTrialIndex", 0);
            if (storedIndex != currentPracticeTrialIndex)
            {
                Debug.LogWarning($"Index mismatch after pause: memory={currentPracticeTrialIndex}, prefs={storedIndex}");
                currentPracticeTrialIndex = storedIndex;
            }
        }
        else // When pausing
        {
            // Force sync before pause
            PlayerPrefs.Save();
        }
    }


    public void EndPracticeMode()
    {
        // Check if we've completed all practice blocks
        bool allBlocksCompleted = (practiceBlockIndex >= practiceBlockSequence.Count - 1);

        // CRITICAL FIX: Make sure we're not transitioning incorrectly
        Debug.Log($"EndPracticeMode - All blocks completed: {allBlocksCompleted}, Current block index: {practiceBlockIndex}");

        // Reset practice trial index
        currentPracticeTrialIndex = -1;

        // When transitioning from practice trials to formal trials, ensure the IsPracticeTrial flag is reset
        PlayerPrefs.SetInt("IsPracticeTrial", 0);
        PlayerPrefs.DeleteKey("CurrentPracticeTrialIndex");
        PlayerPrefs.DeleteKey("CurrentPracticeEffortLevel");

        // IMPORTANT: Make sure BlockOfficiallyStarted is reset
        PlayerPrefs.SetInt("BlockOfficiallyStarted", 0);

        // Save all changes immediately
        PlayerPrefs.Save();

        // Set practice mode off on GridManager if it exists
        GridManager gridManager = FindAnyObjectByType<GridManager>();
        if (gridManager != null)
        {
            gridManager.SetPracticeMode(false);
            Debug.Log("Turning off practice mode in GridManager");
        }

        // Trigger practice completed event
        OnPracticeCompleted?.Invoke();

        // Redirect to Check2_Recognition only if all blocks completed
        if (allBlocksCompleted)
        {
            Debug.Log("All practice blocks completed. Loading check scene.");
            SceneManager.LoadScene(getReadyCheckScene);
        }
        else
        {
            // If not all blocks completed, check if we need to go to Check1_Preference
            if (practiceBlockIndex == 0)
            {
                Debug.Log("First practice block completed. Loading Check1_Preference.");
                // Save state for resuming from check
                PlayerPrefs.SetInt("PracticeBlocksCompleted", 1);
                PlayerPrefs.SetInt("ResumeFromCheck", 1);
                PlayerPrefs.Save();
                // Load Check1_Preference scene
                SceneManager.LoadScene("Check1_Preference");
            }
            else
            {
                // Otherwise, continue to next block
                Debug.Log("Loading next practice block instruction scene.");
                SceneManager.LoadScene(blockInstructionScene);
            }
        }

        RestoreExperimentControllers();
    }

    public void ResumeFromCheck()
    {
        // Check if we need to resume from check
        if (PlayerPrefs.GetInt("ResumeFromCheck", 0) == 1)
        {
            // Debug.Log("Resuming practice from check");

            // Reset ResumeFromCheck flag immediately
            PlayerPrefs.SetInt("ResumeFromCheck", 0);
            PlayerPrefs.Save(); // Make sure to save immediately

            // Get current blocks completed
            int blocksCompleted = PlayerPrefs.GetInt("PracticeBlocksCompleted", 0);
            // Debug.Log($"Resuming with {blocksCompleted} blocks completed");

            // Set up for next block
            practiceBlockIndex = blocksCompleted;

            // Initialize block sequence if needed
            if (practiceBlockSequence == null || practiceBlockSequence.Count == 0)
            {
                InitializePracticeBlocks();
            }

            // Ensure we're within bounds
            if (practiceBlockIndex < practiceBlockSequence.Count)
            {
                // Set current block to the next one we should do
                currentPracticeBlock = practiceBlockSequence[practiceBlockIndex];

                // CRITICAL: Reset the "officially started" flag for the new block
                PlayerPrefs.SetInt("BlockOfficiallyStarted", 0);

                // CRITICAL: Reset the block start time
                blockStartTime = Time.time;
                blockTimeExpired = false;

                // Save current block info
                PlayerPrefs.SetInt("CurrentPracticeBlockIndex", practiceBlockIndex);
                PlayerPrefs.SetString("CurrentPracticeBlockType", currentPracticeBlock.ToString());
                PlayerPrefs.SetInt("IsPracticeTrial", 1);
                PlayerPrefs.Save();

                // Update GridManager
                UpdateGridManagerForCurrentBlock();

                // Generate new trials
                GeneratePracticeTrials();

                // Reset trial index
                SetCurrentPracticeTrialIndex(0);

                // Debug.Log($"Resuming practice with block {practiceBlockIndex}: {currentPracticeBlock}");

                // Load block instruction scene directly
                SceneManager.LoadScene(blockInstructionScene);
            }
            else
            {
                // Debug.Log("All practice blocks completed. Moving to final check.");
                // All blocks completed, go to final check
                SceneManager.LoadScene(getReadyCheckScene);
            }
        }
        else
        {
            // Debug.Log("ResumeFromCheck called but flag not set");
        }
    }

    // Getters for current trial information
    public Sprite GetCurrentPracticeTrialSprite()
    {
        return (currentPracticeTrialIndex >= 0 && currentPracticeTrialIndex < practiceTrials.Count)
            ? practiceTrials[currentPracticeTrialIndex].rewardSprite
            : null;
    }

    public PracticeTrial GetCurrentPracticeTrial()
    {
        int retryAttempt = PlayerPrefs.GetInt("PracticeAttempts", 0);
        // Debug.Log($"GetCurrentPracticeTrial called. Current index: {currentPracticeTrialIndex}, Total trials: {practiceTrials.Count}, Retry attempt: {retryAttempt}");

        // Check if trials exist
        if (practiceTrials == null || practiceTrials.Count == 0)
        {
            Debug.LogWarning("Practice trials list is empty. Regenerating trials.");
            GeneratePracticeTrials();
        }

        if (currentPracticeTrialIndex >= 0 && currentPracticeTrialIndex < practiceTrials.Count)
        {
            PracticeTrial trial = practiceTrials[currentPracticeTrialIndex];
            // Debug.Log($"Returning trial with effort level: {trial.effortLevel}, reward: {trial.rewardValue}");
            return trial;
        }
        else
        {
            Debug.LogError($"Invalid practice trial index: {currentPracticeTrialIndex}. Total trials: {practiceTrials.Count}");
            // Return a default trial to prevent null reference exceptions
            return new PracticeTrial
            {
                effortLevel = 1,
                rewardValue = 10f,
                rewardSprite = appleSprite,
                wasAttempted = false
            };
        }
    }

    // Now also using the calibrated PressesPerEffortLevel 
    public int GetCurrentTrialPressesRequired()
    {
        PracticeTrial currentTrial = GetCurrentPracticeTrial();
        if (currentTrial != null)
        {
            // Get the effort level directly from the current trial
            int effortLevel = currentTrial.effortLevel;

            // Use PlayerPrefs to retrieve the calibrated presses per effort level
            // Subtract 1 from effortLevel to match the PlayerPrefs keys (which are 0-indexed)
            int pressesRequired = PlayerPrefs.GetInt($"PressesPerEffortLevel_{effortLevel}", 0);

            // Debug.Log($"Practice Trial - Effort Level: {effortLevel}, Calibrated Presses Required: {pressesRequired}");

            // Fallback to default values if no calibrated value is found
            if (pressesRequired == 0)
            {
                switch (effortLevel)
                {
                    case 1: return 1; // Apple - 1 press per step
                    case 2: return 3; // Grapes - 3 presses per step
                    case 3: return 5; // Watermelon - 5 presses per step
                    default:
                        Debug.LogWarning($"Unexpected effort level: {effortLevel}. Defaulting to 1.");
                        return 1;
                }
            }

            return pressesRequired;
        }

        Debug.LogError("No current practice trial found. Defaulting to 1 press per step.");
        return 1;
    }

    public bool IsPracticeTrial()
    {
        // Don't reference totalPracticeTrials here
        return currentPracticeTrialIndex >= 0 && PlayerPrefs.GetInt("IsPracticeTrial", 0) == 1;
    }

    public int GetCurrentPracticeTrialIndex()
    {
        // Always prioritize the PlayerPrefs value
        if (PlayerPrefs.HasKey("CurrentPracticeTrialIndex"))
        {
            int storedIndex = PlayerPrefs.GetInt("CurrentPracticeTrialIndex");

            // If in-memory value doesn't match, update it
            if (currentPracticeTrialIndex != storedIndex)
            {
                Debug.Log($"Retrieved currentPracticeTrialIndex from PlayerPrefs: {storedIndex}");
                currentPracticeTrialIndex = storedIndex;
            }
        }
        else
        {
            Debug.LogWarning("CurrentPracticeTrialIndex not found in PlayerPrefs. Defaulting to 0.");
            currentPracticeTrialIndex = 0;
            // Save this value for consistency
            PlayerPrefs.SetInt("CurrentPracticeTrialIndex", 0);
            PlayerPrefs.Save();
        }

        return currentPracticeTrialIndex;
    }

    public int GetCurrentTrialEffortLevel()
    {
        PracticeTrial currentTrial = GetCurrentPracticeTrial();
        if (currentTrial != null)
        {
            // Map effort level to presses per step
            switch (currentTrial.effortLevel)
            {
                case 1: return 1; // Apple - 1 press per step
                case 2: return 2; // Grapes - 3 presses per step
                case 3: return 3; // Watermelon - 5 presses per step
                default:
                    Debug.LogWarning($"Unexpected effort level: {currentTrial.effortLevel}. Defaulting to 1.");
                    return 1;
            }
        }

        Debug.LogError("No current practice trial found. Defaulting to 1 press per step.");
        return 1;
    }

    private Sprite GetSpriteForEffortLevel(int effortLevel)
    {
        switch (effortLevel)
        {
            case 1: return appleSprite;
            case 2: return grapesSprite;
            case 3: return watermelonSprite;
            default: return appleSprite;
        }
    }

    private void RestoreExperimentControllers()
    {
        var experimentManager = FindAnyObjectByType<ExperimentManager>();
        var gameController = FindAnyObjectByType<GameController>();

        if (experimentManager != null)
            experimentManager.enabled = true;

        if (gameController != null)
            gameController.enabled = true;
    }

    public void SetCurrentPracticeTrialIndex(int index)
    {
        Debug.Log($"Setting currentPracticeTrialIndex from {currentPracticeTrialIndex} to {index}");

        // Update the in-memory value
        currentPracticeTrialIndex = index;

        // Update PlayerPrefs with immediate save
        PlayerPrefs.SetInt("CurrentPracticeTrialIndex", index);
        PlayerPrefs.Save();

        // For WebGL, add extra handling
#if UNITY_WEBGL
        try
        {
            // Try to force synchronization
            Application.ExternalCall("syncfs");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"WebGL PlayerPrefs sync failed: {e.Message}");
        }
#endif

        // Double-check the value was properly saved
        int verifyIndex = PlayerPrefs.GetInt("CurrentPracticeTrialIndex", -999);
        if (verifyIndex != index)
        {
            Debug.LogError($"CRITICAL: Failed to set trial index! Expected {index}, got {verifyIndex}");
        }
    }

    private void CheckPlayerPrefsSynchronization()
    {
        // Only run this check once every few seconds
        if (Time.frameCount % 60 != 0) return;

        // Only check if we're in practice mode
        if (PlayerPrefs.GetInt("IsPracticeTrial", 0) != 1) return;

        // Get the current trial index from PlayerPrefs
        int storedIndex = PlayerPrefs.GetInt("CurrentPracticeTrialIndex", 0);

        // Check if it matches our memory variable
        if (storedIndex != currentPracticeTrialIndex)
        {
            Debug.LogWarning($"SYNC WARNING: Trial index mismatch detected. " +
                            $"Memory={currentPracticeTrialIndex}, PlayerPrefs={storedIndex}. " +
                            $"Synchronizing to PlayerPrefs value.");

            // Fix the discrepancy by using the PlayerPrefs value
            currentPracticeTrialIndex = storedIndex;
        }
    }

    public void LogPracticeTrialStart(int trialIndex)
    {
        // int effortLevel = GetCurrentTrialEffortLevel();
        // int requiredPresses = GetCurrentTrialEV();
        //  int requiredPresses = PracticeManager.Instance.GetCurrentTrialPressesRequired();

        LogManager.Instance.LogEvent("TrialStart", new Dictionary<string, string>
        {
            {"TrialNumber", (trialIndex + 1).ToString()}, // Adjust to 1-based index
            {"BlockNumber", "0"}, // Assuming practice trials are in block 0
            // {"EffortLevel", effortLevel.ToString()},
            // {"RequiredPresses", requiredPresses.ToString()},
            {"AdditionalInfo", "Practice"}
        });
    }

    public void LogPracticeTrialOutcome(int trialIndex, bool wasSkipped, bool rewardCollected, float completionTime)
    {
        // Since we've removed skip functionality, wasSkipped should always be false
        string outcome = rewardCollected ? "Success" : "Failure";

        LogManager.Instance.LogEvent("TrialEnd", new Dictionary<string, string>
        {
            {"TrialNumber", (trialIndex + 1).ToString()},
            {"BlockNumber", "0"}, // Assuming practice trials are in block 0
            {"DecisionType", "Work"}, // Always "Work" now since skipping is removed
            {"OutcomeType", outcome},
            {"RewardCollected", rewardCollected.ToString()},
            {"MovementDuration", completionTime.ToString("F3")},
            {"ButtonPresses", "0"}, // Assuming no button presses are logged here
            {"AdditionalInfo", "Practice"}
        });
    }

}