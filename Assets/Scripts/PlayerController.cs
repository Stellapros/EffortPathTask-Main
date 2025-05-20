using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Controls the player's movement and interactions in the GridWorld game.
/// </summary>

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }
    // [SerializeField] private int gridWidth = 18;
    // [SerializeField] private int gridHeight = 10;
    [SerializeField] private float maxTrialDuration = 5.0f; // Configurable timeout
                                                            // private float trialStartTime;
    private bool hasLoggedTrialOutcome = false;
    public event System.Action OnRewardCollected;
    // [SerializeField] private float moveStepSize = 1.0f;
    [SerializeField] private int pressesPerStep = 1;
    [SerializeField] private AudioClip errorSound;
    [SerializeField] private AudioClip rewardSound;
    [SerializeField] private AudioClip stepSound; //step sound
    [SerializeField] private ExperimentManager experimentManager;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private PracticeManager practiceManager;
    // private Vector2 lastMoveDirection = Vector2.right; // Initialize facing right
    // private Vector2 lastNonZeroMovement = Vector2.right; // Initialize facing right
    private SpriteRenderer spriteRenderer;
    private Vector2 lastHorizontalDirection = Vector2.right; // To keep track of last horizontal movement; Initialize facing right
    private bool isTrialRunning = false;
    private bool isMoving = false;
    private int[] directionCounters = new int[4]; // 0: Up, 1: Down, 2: Left, 3: Right
    private int totalButtonPresses = 0;
    private Vector2 initialPosition;
    private Vector2 currentPosition;
    private Rigidbody playerRigidbody;
    private AudioSource audioSource;
    public delegate void MovementRecordedHandler(Vector2 startPos, Vector2 endPos);
    public event MovementRecordedHandler OnMovementRecorded;

    // Create a list to store button presses for later batch logging
    private List<KeyValuePair<float, string>> buttonPressList = new List<KeyValuePair<float, string>>();

    // Movement timing variables - consolidated
    private float trialStartTime;
    private float movementStartTime; // When movement first began
    private bool movementTimerStarted = false;
    private Dictionary<int, float> stepStartTimes = new Dictionary<int, float>();
    private List<float> stepDurations = new List<float>(); // Individual step records

    public bool IsInitialized { get; private set; }

    private void Awake()
    {
        SetupSingleton();
        SetupComponents();

        // Initialize movement state
        isMoving = false;
        isTrialRunning = false;
        ResetCounters();
    }

    private void SetupSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        PlayerController.Instance.OnMovementRecorded += HandleMovementRecorded;
        UpdatePressesPerStep(); // Ensure pressesPerStep is set correctly at the start

        experimentManager = ExperimentManager.Instance;

        IsInitialized = true;
    }


    void HandleMovementRecorded(Vector2 startPos, Vector2 endPos)
    {
        Debug.Log($"Player moved from {startPos} to {endPos}");
        // Store or process the movement coordinates as needed
    }

    void OnDestroy()
    {
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.OnMovementRecorded -= HandleMovementRecorded;
        }
    }

    private void SetupComponents()
    {
        playerRigidbody = GetComponent<Rigidbody>();
        ConfigureRigidbody();

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            Debug.Log("SpriteRenderer component added to PlayerController.");
        }

        if (spriteRenderer != null)
        {
            // Force white with maximum values
            spriteRenderer.color = new Color(1f, 1f, 1f, 1f);
            Debug.Log("Player color forced to white");
        }

        audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            Debug.Log("AudioSource component added to PlayerController.");
        }

        gridManager = gridManager ?? FindAnyObjectByType<GridManager>();
        if (gridManager == null) Debug.LogError("GridManager not found in the scene!");

        // Initialize PracticeManager
        practiceManager = FindAnyObjectByType<PracticeManager>();
        if (practiceManager == null)
        {
            Debug.LogWarning("PracticeManager not found - defaulting to ExperimentManager values");
        }
    }

    private void ConfigureRigidbody()
    {
        if (playerRigidbody != null)
        {
            playerRigidbody.useGravity = false;
            playerRigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
            playerRigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
            playerRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        }
        else
        {
            Debug.LogError("Rigidbody component not found on the player object!");
        }
    }

    private void Update()
    {
        if (!isTrialRunning) return;
        HandleInput();

        // Add trial timeout check
        if (!hasLoggedTrialOutcome && (Time.time - trialStartTime) > maxTrialDuration)
        {
            Debug.Log("Trial timed out - logging movement failure");
            LogMovementFailure();

            // Notify GameController
            GameController.Instance?.RewardCollected(false);

            // Prevent multiple logging
            hasLoggedTrialOutcome = true;
            DisableMovement();
        }
    }

    private void IncrementCounter(int index, Vector2 direction)
    {
        if (!isTrialRunning)
        {
            Debug.Log("IncrementCounter - Trial not running, ignoring input");
            return;
        }

        if (isMoving)
        {
            Debug.Log("IncrementCounter - Currently moving, ignoring input");
            return;
        }

        // Start step timer on first button press for this step
        if (directionCounters[index] == 0)
        {
            // Record the start time for this specific step
            float stepStartTime = Time.time;
            // Store the step start time in a dictionary using the direction index
            stepStartTimes[index] = stepStartTime;
            Debug.Log($"Starting timer for direction {index} at {stepStartTime}");
        }

        directionCounters[index]++;

        Debug.Log($"Button press {totalButtonPresses} in direction {direction}, " +
                  $"Current count: {directionCounters[index]}, Required: {pressesPerStep}");

        // Check if we've reached the required number of presses
        if (directionCounters[index] >= pressesPerStep)
        {
            isMoving = true;
            // Pass the stored step start time for this direction
            AttemptMove(direction, stepStartTimes[index]);
            ResetCounters();
            isMoving = false;
        }
        else
        {
            // Update facing direction even when not moving
            UpdateFacingDirection(direction);
        }
    }

    // In HandleInput() method, start the timer on first button press
    private void HandleInput()
    {
        if (!isTrialRunning) return;

        // Only log button press if a relevant key is actually pressed
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow) ||
            Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            // Start movement timer on first button press of the trial
            StartMovementTimer(); // ADD THIS LINE

            string direction = "none";
            if (Input.GetKeyDown(KeyCode.UpArrow)) direction = "up";
            else if (Input.GetKeyDown(KeyCode.DownArrow)) direction = "down";
            else if (Input.GetKeyDown(KeyCode.LeftArrow)) direction = "left";
            else if (Input.GetKeyDown(KeyCode.RightArrow)) direction = "right";

            // Log button press
            int trialIndex = PlayerPrefs.GetInt("IsPracticeTrial", 0) == 1
                ? PracticeManager.Instance.GetCurrentPracticeTrialIndex()
                : ExperimentManager.Instance.GetCurrentTrialIndex();

            float timeSinceTrialStart = Time.time - trialStartTime;
            buttonPressList.Add(new KeyValuePair<float, string>(timeSinceTrialStart, direction));
            totalButtonPresses++;
            Debug.Log($"Button Press ({direction}) - Total: {totalButtonPresses}, Timer Started: {movementTimerStarted}"); // Enhanced debug
        }

        // Only process input if we're not currently moving
        if (isMoving) return;

        if (Input.GetKeyDown(KeyCode.UpArrow))
            IncrementCounter(0, Vector2.up);
        else if (Input.GetKeyDown(KeyCode.DownArrow))
            IncrementCounter(1, Vector2.down);
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
            IncrementCounter(2, Vector2.left);
        else if (Input.GetKeyDown(KeyCode.RightArrow))
            IncrementCounter(3, Vector2.right);
    }

    private void StartMovementTimer()
    {
        if (!movementTimerStarted)
        {
            movementStartTime = Time.time;
            movementTimerStarted = true;
            Debug.Log($"Movement timer started at {movementStartTime}");
        }
    }

    // private void AttemptMove(Vector2 direction)
    // {
    //     Vector2 startPosition = transform.position;
    //     Vector2 newPosition = (Vector2)transform.position + (direction * moveStepSize);
    //     float stepStartTime = Time.time; // Record the start time of the step

    //     Debug.Log($"Attempting move from {startPosition} to {newPosition}");
    //     Debug.Log($"Step start time: {stepStartTime}");

    //     // Log the start of movement
    //     LogMovementStart(startPosition);

    //     if (gridManager != null && gridManager.IsValidPosition(newPosition))
    //     {
    //         // Record movement before executing it
    //         OnMovementRecorded?.Invoke(startPosition, newPosition);

    //         // Move the character and calculate the step duration
    //         MoveCharacter(newPosition);
    //         UpdateFacingDirection(direction);
    //         PlayStepSound();

    //         currentPosition = newPosition;
    //         float stepDuration = Time.time - stepStartTime; // Calculate the step duration
    //         Debug.Log($"Player moved. New position: {currentPosition}, Step duration: {stepDuration}s");

    //         // Log individual steps with the correct step duration
    //         LogMovementStep(startPosition, newPosition, stepDuration);
    //     }
    //     else
    //     {
    //         Debug.Log("Invalid move attempted. Playing error sound.");
    //         PlaySound(errorSound);
    //     }
    // }

    private IEnumerator SmoothMove(Vector2 startPos, Vector2 endPos, float moveDuration, float stepStartTime)
    {
        float elapsedTime = 0f;

        Debug.Log($"SmoothMove started. Start: {startPos}, End: {endPos}, Duration: {moveDuration}");

        while (elapsedTime < moveDuration)
        {
            // Interpolate the position over time
            transform.position = Vector2.Lerp(startPos, endPos, elapsedTime / moveDuration);
            elapsedTime += Time.deltaTime;
            yield return null; // Wait for the next frame
        }

        // Ensure the player reaches the exact end position
        transform.position = endPos;
        Debug.Log($"SmoothMove completed. Final position: {endPos}");

        // Calculate the true step duration - from first button press to completion of movement
        float stepDuration = Time.time - stepStartTime;
        Debug.Log($"True step duration: {stepDuration}s (from first button press to completion)");

        // Log individual steps with the correct step duration
        LogMovementStep(startPos, endPos, stepDuration, true);
    }

    // private void AttemptMove(Vector2 direction, float stepStartTime)
    // {
    //     Vector2 startPosition = transform.position;
    //     Vector2 newPosition = (Vector2)transform.position + (direction * moveStepSize);

    //     // Convert to grid position first for more accurate checking
    //     Vector2Int gridPos = gridManager.WorldToGridPosition(newPosition);

    //     Debug.Log($"Attempting move from {startPosition} to {newPosition}");
    //     Debug.Log($"Attempting move to: World={newPosition}, Grid={gridPos}");
    //     Debug.Log($"World={newPosition} → Grid={gridPos} | Valid? {gridManager.IsValidFloorPosition(gridPos)}");

    //     // Log the start of movement
    //     LogMovementStart(startPosition);

    //     if (gridManager != null && gridManager.IsValidFloorPosition(gridPos))
    //     {
    //         // Record movement before executing it
    //         OnMovementRecorded?.Invoke(startPosition, newPosition);

    //         // Start the smooth movement coroutine and pass the stepStartTime
    //         StartCoroutine(SmoothMove(startPosition, newPosition, 0.1f, stepStartTime));

    //         UpdateFacingDirection(direction);
    //         PlayStepSound();

    //         currentPosition = newPosition;
    //     }
    //     else
    //     {
    //         Debug.Log("Invalid move attempted. Playing error sound.");
    //         Debug.Log("BLOCKED: Trying to move into a wall!");
    //         PlaySound(errorSound);

    //         // Log the failed step with correct duration
    //         float stepDuration = Time.time - stepStartTime;
    //         LogMovementStep(startPosition, startPosition, stepDuration, false);
    //     }
    // }

    private void AttemptMove(Vector2 direction, float stepStartTime)
    {
        Vector2 startPosition = transform.position;

        // Calculate the new position in grid coordinates first
        Vector2Int currentGridPos = gridManager.WorldToGridPosition(startPosition);
        Vector2Int targetGridPos = currentGridPos + new Vector2Int(
            Mathf.RoundToInt(direction.x),
            Mathf.RoundToInt(direction.y)
        );

        // Convert target grid position back to world position
        Vector2 targetWorldPos = gridManager.GridToWorldPosition(targetGridPos);

        Debug.Log($"Move attempt: Current Grid={currentGridPos}, Target Grid={targetGridPos}");
        Debug.Log($"Move attempt: Current World={startPosition}, Target World={targetWorldPos}");

        // Log the start of movement
        LogMovementStart(startPosition);

        // Check if the target position is valid on the grid
        if (gridManager != null && gridManager.IsValidFloorPosition(targetGridPos))
        {
            // Record movement before executing it
            OnMovementRecorded?.Invoke(startPosition, targetWorldPos);

            // Start the smooth movement coroutine and pass the stepStartTime
            StartCoroutine(SmoothMove(startPosition, targetWorldPos, 0.1f, stepStartTime));

            UpdateFacingDirection(direction);
            PlayStepSound();

            currentPosition = targetWorldPos;
        }
        else
        {
            Debug.Log($"BLOCKED: Invalid move to grid position {targetGridPos}");
            PlaySound(errorSound);

            // Log the failed step with correct duration
            float stepDuration = Time.time - stepStartTime;
            LogMovementStep(startPosition, startPosition, stepDuration, false);
        }
    }

    // Add this utility method to ensure the player is exactly on a grid cell
    public void SnapToGrid()
    {
        Vector2Int currentGridPos = gridManager.WorldToGridPosition(transform.position);
        Vector2 snappedPosition = gridManager.GridToWorldPosition(currentGridPos);

        // Preserve the z-coordinate
        transform.position = new Vector3(snappedPosition.x, snappedPosition.y, transform.position.z);
        currentPosition = snappedPosition;

        Debug.Log($"Snapped player to grid: World={snappedPosition}, Grid={currentGridPos}");
    }


    private void MoveCharacter(Vector2 newPosition)
    {
        Vector3 targetPosition = new Vector3(newPosition.x, newPosition.y, transform.position.z);
        Debug.Log($"Moving from {transform.position} to {targetPosition}");

        if (playerRigidbody != null)
        {
            // Use Rigidbody.MovePosition for smooth movement
            playerRigidbody.MovePosition(targetPosition);
        }
        else
        {
            // Fallback to direct position update if Rigidbody is missing
            transform.position = targetPosition;
        }

        Debug.Log($"Player moved. New position: {transform.position}");
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
            audioSource.volume = 4.0f;
            Debug.Log($"Played sound: {clip.name}");
        }
        else
        {
            Debug.LogWarning("Unable to play sound. Sound clip or AudioSource is missing.");
        }
    }

    private void PlayStepSound()
    {
        PlaySound(stepSound);
    }

    // Update ResetCounters to also clear step start times
    public void ResetCounters()
    {
        for (int i = 0; i < directionCounters.Length; i++)
            directionCounters[i] = 0;

        // Clear step start times
        stepStartTimes.Clear();
    }
    // public void ResetPosition(Vector2 position)
    // {
    //     // If you want to ensure absolute grid center
    //     Vector2 gridCenterPosition = gridManager.GridToWorldPosition(
    //         new Vector2Int(gridWidth / 2, gridHeight / 2)
    //     );
    //     transform.position = new Vector3(position.x, position.y, transform.position.z);
    //     initialPosition = position;
    //     currentPosition = position;

    //     ResetCounters();
    //     totalButtonPresses = 0;
    //     if (playerRigidbody != null) playerRigidbody.linearVelocity = Vector2.zero;
    //     ApplyFacingDirection(); // Apply the last known facing direction
    //     Debug.Log($"Player position reset to: {position}, Total button presses reset to 0");
    // }

    public void ResetPosition(Vector2 position)
    {
        // Calculate the grid position from the world position
        Vector2Int gridPosition;

        // If position is (0,0), assume we want the center of the grid
        if (position == Vector2.zero)
        {
            gridPosition = new Vector2Int(gridManager.GridWidth / 2, gridManager.GridHeight / 2);
            // Convert this grid position back to a world position to ensure alignment
            position = gridManager.GridToWorldPosition(gridPosition);
        }
        else
        {
            // Convert the provided world position to a grid position first
            gridPosition = gridManager.WorldToGridPosition(position);
            // Then back to world to ensure it's aligned with the grid
            position = gridManager.GridToWorldPosition(gridPosition);
        }

        // Set the position with consistent z-coordinate
        transform.position = new Vector3(position.x, position.y, transform.position.z);
        initialPosition = position;
        currentPosition = position;

        ResetCounters();
        totalButtonPresses = 0;
        if (playerRigidbody != null) playerRigidbody.linearVelocity = Vector2.zero;
        ApplyFacingDirection(); // Apply the last known facing direction
        Debug.Log($"Player position reset to: World={position}, Grid={gridPosition}");
    }
    public void UpdateFacingDirection(Vector2 direction)
    {
        // Only update for horizontal movement
        if (direction.x != 0)
        {
            lastHorizontalDirection = new Vector2(direction.x, 0);
            ApplyFacingDirection();
        }
    }

    private void ApplyFacingDirection()
    {
        if (spriteRenderer != null)
        {
            // Flip the sprite horizontally if moving left
            spriteRenderer.flipX = (lastHorizontalDirection.x < 0);
        }
        else
        {
            Debug.LogWarning("SpriteRenderer is missing. Unable to flip sprite.");
        }
    }

    public Vector2 GetInitialPosition() => initialPosition;
    public Vector2 GetCurrentPosition() => currentPosition;
    public int GetButtonPressCount() => totalButtonPresses;

    public void SetPressesPerStep(int presses)
    {
        // Check if experimentManager is initialized
        if (ExperimentManager.Instance == null)
        {
            Debug.LogError("ExperimentManager is null in SetPressesPerStep!");
            return;
        }

        pressesPerStep = presses;
        ResetCounters();
        Debug.Log($"PlayerController: Presses per step set to {pressesPerStep}");
    }

    public void UpdatePressesPerStep()
    {
        Debug.Log("UpdatePressesPerStep called with details:");
        Debug.Log($"IsPracticeTrial: {PlayerPrefs.GetInt("IsPracticeTrial", 0)}");
        // Debug.Log($"Current Practice Trial Index: {PlayerPrefs.GetInt("CurrentPracticeTrialIndex", -1)}");

        // Check if it's a practice trial
        if (PlayerPrefs.GetInt("IsPracticeTrial", 0) == 1)
        {
            // Try to use PracticeManager
            if (practiceManager != null)
            {
                // Ensure the trial index is valid
                if (practiceManager.GetCurrentPracticeTrialIndex() >= 0)
                {
                    // Use PracticeManager to get presses required
                    pressesPerStep = practiceManager.GetCurrentTrialPressesRequired();
                    Debug.Log($"Practice trial - using presses required: {pressesPerStep}");
                }
                else
                {
                    Debug.LogError("Invalid practice trial index. Defaulting to 1 press per step.");
                    pressesPerStep = 1;
                }
            }
            else
            {
                Debug.LogError("PracticeManager not found. Defaulting to 1 press per step.");
                pressesPerStep = 1;
            }
        }
        else
        {
            // Normal trial logic - use ExperimentManager
            if (ExperimentManager.Instance != null)
            {
                pressesPerStep = ExperimentManager.Instance.GetCurrentTrialEV();
                Debug.Log($"Formal trial - using presses required: {pressesPerStep}");
            }
            else
            {
                Debug.LogError("ExperimentManager not found - defaulting to 1 press per step");
                pressesPerStep = 1;
            }
        }

        Debug.Log($"PlayerController: Final presses per step value: {pressesPerStep}");
    }

    public void EnableMovement()
    {
        Debug.Log("EnableMovement called - Starting initialization sequence");

        // Reset the trial outcome when starting a new trial
        PlayerPrefs.SetString("CurrentTrialOutcome", "Pending");

        // Ensure clean initial state
        isMoving = false;

        trialStartTime = Time.time;
        hasLoggedTrialOutcome = false;
        isTrialRunning = false;
        totalButtonPresses = 0;
        ResetCounters();

        // Clear step durations list
        stepDurations.Clear();

        // Reset movement tracking
        movementTimerStarted = false;
        movementStartTime = 0f;
        buttonPressList.Clear();

        // Update presses per step
        UpdatePressesPerStep();

        // Enable physics if needed
        if (playerRigidbody != null)
        {
            playerRigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
            playerRigidbody.linearVelocity = Vector3.zero;
        }

        // Important: Enable trial LAST, after all setup is complete
        isTrialRunning = true;

        Debug.Log($"Movement fully enabled - Presses per step: {pressesPerStep}, Moving: {isMoving}, Trial Running: {isTrialRunning}");
    }

    public void DisableMovement()
    {
        isTrialRunning = false;
        isMoving = false;  // Reset moving state

        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.constraints = RigidbodyConstraints.FreezeAll;
        }
        // Reset all input counters
        ResetCounters();
        totalButtonPresses = 0;
        Debug.Log("Player movement disabled from PlayerController");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Reward") && isTrialRunning)
        {
            // Set the trial outcome to Success when reward is collected
            PlayerPrefs.SetString("CurrentTrialOutcome", "Success");

            // Mark as logged to prevent duplicate logs
            hasLoggedTrialOutcome = true;

            // Record collision time
            float collisionTime = Time.time;

            // Calculate total trial duration
            float trialDuration = collisionTime - trialStartTime;

            // Calculate the actual movement duration from when movement first started
            float movementDuration = 0f;
            if (movementTimerStarted)
            {
                movementDuration = collisionTime - movementStartTime;
                Debug.Log($"Movement duration calculation: {collisionTime} - {movementStartTime} = {movementDuration}");
            }
            else
            {
                Debug.LogWarning("Movement timer was never started, duration will be 0");
            }

            // Calculate sum of step durations for verification
            float totalStepDuration = stepDurations.Sum();
            Debug.Log($"Total step durations: {totalStepDuration}s, Movement duration: {movementDuration}s");

            // Determine if this is a practice trial
            bool isPracticeTrial = PlayerPrefs.GetInt("IsPracticeTrial", 0) == 1;

            // Get trial index based on trial type
            int trialIndex = isPracticeTrial
                ? PracticeManager.Instance.GetCurrentPracticeTrialIndex()
                : ExperimentManager.Instance.GetCurrentTrialIndex();
            Debug.Log($"Reward collected - Trial Index: {trialIndex}, IsPractice: {isPracticeTrial}, PlayerPrefs Index: {PlayerPrefs.GetInt("CurrentPracticeTrialIndex", -1)}");

            // Get block number based on trial type
            int blockNumber = isPracticeTrial ? -1 : ExperimentManager.Instance.GetCurrentBlockNumber();

            Debug.Log($"Logging decision outcome - Trial: {trialIndex}, Block: {blockNumber}, isPractice: {isPracticeTrial}");

            // Get decision time, effort level, and required presses based on trial type
            float decisionTime = isPracticeTrial
                ? PlayerPrefs.GetFloat("PracticeDecisionTime", 0f)
                : PlayerPrefs.GetFloat("DecisionTime", 0f);

            int effortLevel = isPracticeTrial
                ? PracticeManager.Instance.GetCurrentTrialEffortLevel()
                : ExperimentManager.Instance.GetCurrentTrialEffortLevel();

            int requiredPresses = isPracticeTrial
                ? PracticeManager.Instance.GetCurrentTrialPressesRequired()
                : ExperimentManager.Instance.GetCurrentTrialEV();

            // Handle reward collection
            GameController gameController = GameController.Instance;
            if (gameController != null)
            {
                gameController.RewardCollected(true);
            }

            var currentTotal = ScoreManager.Instance?.GetTotalScore() ?? 0;
            var currentPractice = ScoreManager.Instance?.GetPracticeScore() ?? 0;

            // Log the collision time with detailed information
            LogManager.Instance.LogEvent("CollisionTime", new Dictionary<string, string>
        {
            {"TrialNumber", (trialIndex + 1).ToString()},
            {"Time", collisionTime.ToString("F3")},
            {"TrialDuration", trialDuration.ToString("F3")},
            {"MovementDuration", movementDuration.ToString("F3")},
            {"IsPractice", isPracticeTrial.ToString()},
            {"Timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}
        });

            // Always log movement duration as a separate event
            LogManager.Instance.LogEvent("MovementDuration", new Dictionary<string, string>
        {
            {"TrialNumber", (trialIndex + 1).ToString()},
            {"MovementDuration", movementDuration.ToString("F3")},
            {"TotalStepDuration", totalStepDuration.ToString("F3")},
            {"OutcomeType", "Success"}
        });

            // Get practice type if this is a practice trial
            string practiceType = "";
            if (isPracticeTrial && PracticeManager.Instance != null)
            {
                // Get the current practice block type from PlayerPrefs
                practiceType = PlayerPrefs.GetString("CurrentPracticeBlockType", "EqualRatio");
                Debug.Log($"Found practice type for logging: {practiceType}");
            }
            // Pass the movement duration to LogDecisionOutcome
            LogManager.Instance.LogDecisionOutcome(
                trialIndex,
                blockNumber,
                PlayerPrefs.GetString("DecisionType", "Work"),
                true, // rewardCollected
                decisionTime,
                movementDuration,
                totalButtonPresses,
                effortLevel,
                requiredPresses,
                false, // skipAdjustment
                string.Join("|", buttonPressList.Select(bp => $"{bp.Key:F3}:{bp.Value}")),
                movementDuration / Mathf.Max(1, totalButtonPresses),
                10, // points actually earned
                loggedTotalScore: currentTotal,
                loggedPracticeScore: currentPractice,
                practiceType // Add the practice type parameter
            );

            Debug.Log($"Logging trial outcome - Trial: {trialIndex}, Block: {blockNumber}");

            // Log final movement details
            LogMovementEnd(currentPosition, true, true); // This is the final move

            // Log success outcome
            LogManager.Instance.LogEvent("OutcomeType", new Dictionary<string, string>
        {
            {"TrialNumber", (trialIndex + 1).ToString()},
            {"OutcomeType", "Success"}
        });

            // Log reward collection
            LogManager.Instance.LogEvent("RewardCollected", new Dictionary<string, string>
        {
            {"TrialNumber", (trialIndex + 1).ToString()},
            {"Collected", "True"}
        });

            // Play reward sound and disable reward object
            PlaySound(rewardSound);
            other.gameObject.SetActive(false);

            // Invoke reward collected event before disabling movement
            OnRewardCollected?.Invoke();
            LogButtonPressesAtTrialEnd(trialIndex);
            DisableMovement();
            return;
        }
    }
    #region Movement Logging
    private void LogMovementStart(Vector2 startPosition)
    {
        // Round the start position to 2 decimal places
        float startX = Mathf.Round(startPosition.x * 10) / 10; // Round to 2 decimal places
        float startY = Mathf.Round(startPosition.y * 10) / 10; // Round to 2 decimal places

        // Get trial index based on trial type
        bool isPracticeTrial = PlayerPrefs.GetInt("IsPracticeTrial", 0) == 1;
        int trialIndex = isPracticeTrial
            ? PracticeManager.Instance.GetCurrentPracticeTrialIndex()
            : ExperimentManager.Instance.GetCurrentTrialIndex();

        // Get block number based on trial type
        int blockNumber = isPracticeTrial ? -1 : ExperimentManager.Instance.GetCurrentBlockNumber();

        if (LogManager.Instance != null)
        {
            LogManager.Instance.LogEvent("MovementStart", new Dictionary<string, string>
        {
            {"TrialNumber", (trialIndex + 1).ToString()},
            {"BlockNumber", (blockNumber + 1).ToString()},
            {"StartX", startX.ToString("F2")}, // Use rounded value
            {"StartY", startY.ToString("F2")}, // Use rounded value
            {"Timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}
        });
        }
    }

    // Track step durations
    private void LogMovementStep(Vector2 startPos, Vector2 endPos, float stepDuration, bool successful)
    {
        // Calculate timing values
        float stepStartTime = Time.time - stepDuration;
        float stepEndTime = Time.time;

        // Add this step's duration to our tracking list
        stepDurations.Add(stepDuration);

        // Determine if this is a practice trial
        bool isPracticeTrial = PlayerPrefs.GetInt("IsPracticeTrial", 0) == 1;

        // Get trial index based on trial type
        int trialIndex = isPracticeTrial
            ? PracticeManager.Instance.GetCurrentPracticeTrialIndex()
            : ExperimentManager.Instance.GetCurrentTrialIndex();

        // Get block number based on trial type
        int blockNumber = isPracticeTrial ? -1 : ExperimentManager.Instance.GetCurrentBlockNumber();

        // Get the specific practice block type if this is a practice trial
        string blockType = "";
        if (isPracticeTrial)
        {
            string practiceBlockType = PlayerPrefs.GetString("CurrentPracticeBlockType", "EqualRatio");
            blockType = "Practice_" + practiceBlockType; // Format as Practice_[BlockType]
            Debug.Log($"Using block type for movement step: {blockType}");
        }

        // Get effort level based on trial type
        int effortLevel = isPracticeTrial
            ? PracticeManager.Instance.GetCurrentTrialEffortLevel()
            : ExperimentManager.Instance.GetCurrentTrialEffortLevel();

        // Round the coordinates to 2 decimal places
        float startX = Mathf.Round(startPos.x * 10) / 10;
        float startY = Mathf.Round(startPos.y * 10) / 10;
        float endX = Mathf.Round(endPos.x * 10) / 10;
        float endY = Mathf.Round(endPos.y * 10) / 10;

        Debug.Log($"Original Start: ({startPos.x}, {startPos.y}), Rounded Start: ({startX}, {startY})");
        Debug.Log($"Original End: ({endPos.x}, {endPos.y}), Rounded End: ({endX}, {endY})");

        if (LogManager.Instance != null)
        {
            // Update the LogMovementStep method call to include all the necessary parameters
            LogManager.Instance.LogMovementStep(
                trialIndex + 1, // 1-based index
                startPos,
                endPos,
                stepDuration,
                successful,
                stepStartTime,
                stepEndTime,
                pressesPerStep,
                blockNumber + 1, // 1-based index
                blockType // Pass the formatted practice block type
            );
        }
    }

    private void LogMovementEnd(Vector2 endPosition, bool rewardCollected, bool isFinalMove = false)
    {
        // Calculate total movement time if this is the final move and timer was started
        float totalTime = (isFinalMove && movementTimerStarted) ? (Time.time - movementStartTime) : 0f;

        // Get trial index based on trial type
        int trialIndex = PlayerPrefs.GetInt("IsPracticeTrial", 0) == 1
            ? PracticeManager.Instance.GetCurrentPracticeTrialIndex()
            : ExperimentManager.Instance.GetCurrentTrialIndex();


        if (LogManager.Instance != null && isFinalMove)
        {
            LogManager.Instance.LogEvent("MovementComplete", new Dictionary<string, string>
        {
            {"TrialNumber", (trialIndex + 1).ToString()},
            {"TotalMovementDuration", totalTime.ToString("F3")},
            {"TotalButtonPresses", totalButtonPresses.ToString()},
            {"RewardCollected", rewardCollected.ToString()},
            {"Timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}
        });

            // Only log this for the final outcome, not intermediate steps
            if (isFinalMove)
            {
                // Add separate reward collection event
                LogManager.Instance.LogEvent("RewardCollected", new Dictionary<string, string>
            {
                {"TrialNumber", (trialIndex + 1).ToString()},
                {"Collected", rewardCollected.ToString()}
            });
            }
        }
    }


    public void LogMovementFailure()
    {
        if (hasLoggedTrialOutcome) return;
        // Mark as logged immediately to prevent duplicate logs
        hasLoggedTrialOutcome = true;

        // Set the trial outcome to Failure when trial fails
        PlayerPrefs.SetString("CurrentTrialOutcome", "Failure");

        // Check if LogManager exists
        if (LogManager.Instance == null)
        {
            Debug.LogError("LogMovementFailure: LogManager.Instance is null!");
            return;
        }

        // Calculate movement duration regardless of whether movement timer was started
        float movementDuration = movementTimerStarted ? Time.time - movementStartTime : 0f;

        // If movement timer wasn't started, log this fact but continue with logging
        if (!movementTimerStarted)
        {
            Debug.Log("No movement detected in this trial, but still logging decision outcome.");
        }
        else
        {
            Debug.Log($"Movement duration calculation: {Time.time} - {movementStartTime} = {movementDuration}");
        }

        // Calculate sum of step durations for verification
        float totalStepDuration = stepDurations.Count > 0 ? stepDurations.Sum() : 0f;
        Debug.Log($"Failure - Total step durations: {totalStepDuration}s, Movement duration: {movementDuration}s");

        // Determine if this is a practice trial
        bool isPracticeTrial = PlayerPrefs.GetInt("IsPracticeTrial", 0) == 1;

        // Get trial index based on trial type
        int trialIndex;
        int blockNumber;
        float decisionTime;
        int effortLevel;
        int requiredPresses;

    // Safely get trial data
    try
    {
        if (isPracticeTrial)
        {
            // CRITICAL FIX: Use the stored trial index from decision time to avoid race condition
            trialIndex = PlayerPrefs.GetInt("CurrentDecisionTrialIndex", -1);
            
            // Log which method we're using to determine the trial index
            if (trialIndex != -1)
            {
                Debug.Log($"Using stored trial index: {trialIndex} from PlayerPrefs");
            }
            else
            {
                Debug.LogWarning("Stored trial index not found in PlayerPrefs, falling back to PracticeManager");
                
                if (PracticeManager.Instance == null)
                {
                    Debug.LogError("PracticeManager.Instance is null!");
                    trialIndex = -1;
                }
                else
                {
                    trialIndex = PracticeManager.Instance.GetCurrentPracticeTrialIndex();
                    Debug.Log($"Fallback trial index from PracticeManager: {trialIndex}");
                }
            }
            
            blockNumber = -1; // Use -1 for practice blocks
            
            if (PracticeManager.Instance == null)
            {
                Debug.LogError("PracticeManager.Instance is null when getting effort level!");
                effortLevel = 1;
                requiredPresses = 1;
            }
            else
            {
                effortLevel = PracticeManager.Instance.GetCurrentTrialEffortLevel();
                requiredPresses = PracticeManager.Instance.GetCurrentTrialPressesRequired();
            }
            
            decisionTime = PlayerPrefs.GetFloat("PracticeDecisionTime", 0f);
        }
        else
        {
            if (ExperimentManager.Instance == null)
            {
                Debug.LogError("ExperimentManager.Instance is null!");
                trialIndex = -1;
                blockNumber = -1;
                effortLevel = 1;
                requiredPresses = 1;
                decisionTime = 0f;
            }
            else
            {
                trialIndex = ExperimentManager.Instance.GetCurrentTrialIndex();
                blockNumber = ExperimentManager.Instance.GetCurrentBlockNumber();
                effortLevel = ExperimentManager.Instance.GetCurrentTrialEffortLevel();
                requiredPresses = ExperimentManager.Instance.GetCurrentTrialEV();
                decisionTime = PlayerPrefs.GetFloat("DecisionTime", 0f);
            }
        }
    }
        catch (Exception e)
        {
            Debug.LogError($"Error getting trial data: {e.Message}\n{e.StackTrace}");
            trialIndex = -1;
            blockNumber = -1;
            effortLevel = 1;
            requiredPresses = 1;
            decisionTime = 0f;
        }

        // Handle zero presses gracefully
        float timePerPress = totalButtonPresses > 0 ? movementDuration / totalButtonPresses : 0f;

        // Format press data before logging anything
        string buttonPressData = totalButtonPresses > 0
            ? string.Join("|", buttonPressList.Select(bp => $"{bp.Key:F3}:{bp.Value}"))
            : "-";

        // Always log movement duration as a separate event
        try
        {
            LogManager.Instance.LogEvent("MovementDuration", new Dictionary<string, string>
        {
            {"TrialNumber", (trialIndex + 1).ToString()},
            {"MovementDuration", movementDuration.ToString("F3")},
            {"TotalStepDuration", totalStepDuration.ToString("F3")},
            {"OutcomeType", "Failure"}
        });

            // Log failure outcome
            LogManager.Instance.LogEvent("OutcomeType", new Dictionary<string, string>
        {
            {"TrialNumber", (trialIndex + 1).ToString()},
            {"OutcomeType", "Failure"}
        });

            // Log reward collection (not collected)
            LogManager.Instance.LogEvent("RewardCollected", new Dictionary<string, string>
        {
            {"TrialNumber", (trialIndex + 1).ToString()},
            {"Collected", "False"}
        });

            LogManager.Instance.LogEvent("ButtonPresses", new Dictionary<string, string>
        {
            {"TrialNumber", (trialIndex + 1).ToString()},
            {"TotalPresses", totalButtonPresses.ToString()},
            {"PressData", buttonPressData} // Include press data here
        });
        }
        catch (Exception e)
        {
            Debug.LogError($"Error logging events: {e.Message}\n{e.StackTrace}");
        }

        int currentTotal = 0;
        int currentPractice = 0;

        try
        {
            currentTotal = ScoreManager.Instance?.GetTotalScore() ?? 0;
            currentPractice = ScoreManager.Instance?.GetPracticeScore() ?? 0;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error getting scores: {e.Message}");
        }

        // ENSURE we actually log the DecisionOutcome with detailed error handling
        try
        {
            // Get practice type if this is a practice trial
            string practiceType = "";
            if (isPracticeTrial && PracticeManager.Instance != null)
            {
                // Get the current practice block type from PlayerPrefs
                practiceType = PlayerPrefs.GetString("CurrentPracticeBlockType", "EqualRatio");
                Debug.Log($"Found practice type for failure logging: {practiceType}");
            }

            // Log decision outcome for failure
            if (LogManager.Instance != null)
            {
                Debug.Log($"About to log decision outcome with parameters: " +
                          $"trialIndex={trialIndex}, " +
                          $"blockNumber={blockNumber}, " +
                          $"decisionType={PlayerPrefs.GetString("DecisionType", "Work")}, " +
                          $"rewardCollected=false, " +
                          $"decisionTime={decisionTime}, " +
                          $"movementDuration={movementDuration}, " +
                          $"totalButtonPresses={totalButtonPresses}, " +
                          $"effortLevel={effortLevel}, " +
                          $"requiredPresses={requiredPresses}, " +
                          $"timePerPress={timePerPress}, " +
                          $"loggedTotalScore={currentTotal}, " +
                          $"loggedPracticeScore={currentPractice}");

                LogManager.Instance.LogDecisionOutcome(
                    trialIndex,
                    blockNumber,
                    PlayerPrefs.GetString("DecisionType", "Work"),
                    false, // rewardCollected
                    decisionTime,
                    movementDuration,
                    totalButtonPresses,
                    effortLevel,
                    requiredPresses,
                    false, // skipAdjustment
                    buttonPressData, // Use the formatted press data here
                    timePerPress,
                    0, // points for failure
                    loggedTotalScore: currentTotal,
                    loggedPracticeScore: currentPractice,
                    practiceType // Add the practice type parameter
                );

                Debug.Log($"Successfully logged decision outcome for failure - Trial: {trialIndex}, Block: {blockNumber}");

                // Ensure button presses are logged even if other logging operations fail
                try
                {
                    // Explicitly log button presses for failed trials
                    LogButtonPressesAtTrialEnd(trialIndex);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error logging button presses on failure: {e.Message}");
                }
            }
            else
            {
                Debug.LogError("LogManager.Instance is null when trying to log decision outcome!");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error logging decision outcome: {e.Message}\n{e.StackTrace}");

            // Try one more time to log button presses if decision outcome logging failed
            try
            {
                LogButtonPressesAtTrialEnd(trialIndex);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Final attempt to log button presses failed: {ex.Message}");
            }
        }

        Debug.Log($"Failed Trial | Presses: {totalButtonPresses} | MovementTime: {movementDuration:F3} | TimePerPress: {timePerPress:F3}");

        // Log movement end
        try
        {
            LogMovementEnd(currentPosition, false, true);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error logging movement end: {e.Message}");
        }

        // Reset movement tracking
        movementTimerStarted = false;

        // Notify GameController about failure
        try
        {
            GameController gameController = GameController.Instance;
            if (gameController != null)
            {
                gameController.RewardCollected(false);
            }
            else
            {
                Debug.LogWarning("GameController.Instance is null when notifying about failure");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error notifying GameController: {e.Message}");
        }

        DisableMovement();
    }
    #endregion

    // Then log the entire list at the end of the trial
    private void LogButtonPressesAtTrialEnd(int trialIndex)
    {
        // string buttonPressData = string.Join("|", buttonPressList.Select(bp => $"{bp.Key:F3}:{bp.Value}"));

        // Format the press data in the same way as in LogDecisionOutcome
        string buttonPressData = totalButtonPresses > 0
            ? string.Join("|", buttonPressList.Select(bp => $"{bp.Key:F3}:{bp.Value}"))
            : "-";

        LogManager.Instance.LogEvent("ButtonPresses", new Dictionary<string, string>
        {
            {"TrialNumber", (trialIndex + 1).ToString()},
            {"TotalPresses", totalButtonPresses.ToString()},
            {"PressData", buttonPressData} // Format: "0.123:right|0.456:left|..."
        });

        // Clear the list for next trial
        buttonPressList.Clear();
    }
}