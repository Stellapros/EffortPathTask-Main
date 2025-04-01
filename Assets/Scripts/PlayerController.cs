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
    [SerializeField] private int gridWidth = 18;
    [SerializeField] private int gridHeight = 10;
    [SerializeField] private float maxTrialDuration = 5.0f; // Configurable timeout
                                                            // private float trialStartTime;
    private bool hasLoggedTrialOutcome = false;
    public event System.Action OnRewardCollected;
    [SerializeField] private float moveStepSize = 1.0f;
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
    private float movementStartTime;
    private bool movementTimerStarted = false;

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

        // Start movement timer on first button press if not already started
        if (!movementTimerStarted)
        {
            StartMovementTimer();
        }

        directionCounters[index]++;

        Debug.Log($"Button press {totalButtonPresses} in direction {direction}, " +
                  $"Current count: {directionCounters[index]}, Required: {pressesPerStep}");

        // Check if we've reached the required number of presses
        if (directionCounters[index] >= pressesPerStep)
        {
            isMoving = true;
            AttemptMove(direction);
            ResetCounters();
            isMoving = false;
        }
        else
        {
            // Update facing direction even when not moving
            UpdateFacingDirection(direction);
        }
    }

    private void HandleInput()
    {
        if (!isTrialRunning) return;

        // Only log button press if a relevant key is actually pressed
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow) ||
            Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow))
        {
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
            Debug.Log($"Elapsed time: {elapsedTime}");
            yield return null; // Wait for the next frame
        }

        // Ensure the player reaches the exact end position
        transform.position = endPos;
        Debug.Log($"SmoothMove completed. Final position: {endPos}");

        // Calculate the step duration after the movement is complete
        float stepDuration = Time.time - stepStartTime;
        Debug.Log($"Step duration: {stepDuration}s");

        // Log individual steps with the correct step duration
        LogMovementStep(startPos, endPos, stepDuration);
    }

    private void AttemptMove(Vector2 direction)
    {
        Vector2 startPosition = transform.position;
        Vector2 newPosition = (Vector2)transform.position + (direction * moveStepSize);
        float stepStartTime = Time.time; // Record the start time of the step

        // Convert to grid position first for more accurate checking
        Vector2Int gridPos = gridManager.WorldToGridPosition(newPosition);

        // Snap the new position to the grid
        // newPosition.x = Mathf.Round(newPosition.x * 100) / 100; // Round to 2 decimal places
        // newPosition.y = Mathf.Round(newPosition.y * 100) / 100; // Round to 2 decimal places
        Debug.Log($"Attempting move from {startPosition} to {newPosition}");
        Debug.Log($"Attempting move to: World={newPosition}, Grid={gridPos}");
        Debug.Log($"World={newPosition} → Grid={gridPos} | Valid? {gridManager.IsValidFloorPosition(gridPos)}");


        // Log the start of movement
        LogMovementStart(startPosition);

        // if (gridManager != null && gridManager.IsValidPosition(newPosition))
        if (gridManager != null && gridManager.IsValidFloorPosition(gridPos))
        {
            // Record movement before executing it
            OnMovementRecorded?.Invoke(startPosition, newPosition);

            // Start the smooth movement coroutine and pass the stepStartTime
            StartCoroutine(SmoothMove(startPosition, newPosition, 0.1f, stepStartTime)); // Adjust the duration as needed

            UpdateFacingDirection(direction);
            PlayStepSound();

            currentPosition = newPosition;
        }
        else
        {
            Debug.Log("Invalid move attempted. Playing error sound.");
            Debug.Log("BLOCKED: Trying to move into a wall!");
            PlaySound(errorSound);
        }
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

    public void ResetCounters()
    {
        for (int i = 0; i < directionCounters.Length; i++)
            directionCounters[i] = 0;
    }

    public void ResetPosition(Vector2 position)
    {
        // If you want to ensure absolute grid center
        Vector2 gridCenterPosition = gridManager.GridToWorldPosition(
            new Vector2Int(gridWidth / 2, gridHeight / 2)
        );
        transform.position = new Vector3(position.x, position.y, transform.position.z);
        initialPosition = position;
        currentPosition = position;

        ResetCounters();
        totalButtonPresses = 0;
        if (playerRigidbody != null) playerRigidbody.linearVelocity = Vector2.zero;
        ApplyFacingDirection(); // Apply the last known facing direction
        Debug.Log($"Player position reset to: {position}, Total button presses reset to 0");
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
        Debug.Log($"Current Practice Trial Index: {PlayerPrefs.GetInt("CurrentPracticeTrialIndex", -1)}");

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

        // Ensure clean initial state
        isMoving = false;

        trialStartTime = Time.time;
        hasLoggedTrialOutcome = false;
        isTrialRunning = false;
        totalButtonPresses = 0;
        ResetCounters();

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

            // Determine if this is a practice trial
            bool isPracticeTrial = PlayerPrefs.GetInt("IsPracticeTrial", 0) == 1;

            // Get trial index based on trial type
            int trialIndex = isPracticeTrial
                ? PracticeManager.Instance.GetCurrentPracticeTrialIndex()
                : ExperimentManager.Instance.GetCurrentTrialIndex();

            // Get block number based on trial type
            int blockNumber = isPracticeTrial ? -1 : ExperimentManager.Instance.GetCurrentBlockNumber();

            Debug.Log($"Logging decision outcome - Trial: {trialIndex}, Block: {blockNumber}, isPractice: {isPracticeTrial}");

            // Get decision time, effort level, and required presses based on trial type
            float decisionTime = isPracticeTrial
                ? PlayerPrefs.GetFloat("PracticeDecisionTime", 0f) // Retrieve decision time for practice trials
                : PlayerPrefs.GetFloat("DecisionTime", 0f); // Retrieve decision time for formal trials

            int effortLevel = isPracticeTrial
                ? PracticeManager.Instance.GetCurrentTrialEffortLevel() // Retrieve effort level for practice trials
                : ExperimentManager.Instance.GetCurrentTrialEffortLevel(); // Retrieve effort level for formal trials

            int requiredPresses = isPracticeTrial
                ? PracticeManager.Instance.GetCurrentTrialPressesRequired() // Retrieve required presses for practice trials
                : ExperimentManager.Instance.GetCurrentTrialEV(); // Retrieve required presses for formal trials

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

            // Pass the movement duration (collision timing) to LogDecisionOutcome
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
                string.Join("|", buttonPressList.Select(bp => $"{bp.Key:F3}:{bp.Value}")), // pressData
                movementDuration / Mathf.Max(1, totalButtonPresses), // timePerPress
                10 // points
            );

            Debug.Log($"Logging trial outcome - Trial: {trialIndex}, Block: {blockNumber}");

            // Log final movement details
            LogMovementEnd(currentPosition, true, true); // This is the final move

            // Log movement duration
            LogManager.Instance.LogEvent("MovementDuration", new Dictionary<string, string>
        {
            {"TrialNumber", (trialIndex + 1).ToString()},
            {"MovementDuration", movementDuration.ToString("F3")},
            {"OutcomeType", "Success"}
        });

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

            // Handle reward collection
            GameController gameController = GameController.Instance;
            if (gameController != null)
            {
                // gameController.LogRewardCollectionTiming(collisionTime, movementDuration);
                gameController.RewardCollected(true);
            }

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
        float startX = Mathf.Round(startPosition.x * 100) / 100; // Round to 2 decimal places
        float startY = Mathf.Round(startPosition.y * 100) / 100; // Round to 2 decimal places

        // Get trial index based on trial type
        int trialIndex = PlayerPrefs.GetInt("IsPracticeTrial", 0) == 1
            ? PracticeManager.Instance.GetCurrentPracticeTrialIndex()
            : ExperimentManager.Instance.GetCurrentTrialIndex();

        if (LogManager.Instance != null)
        {
            LogManager.Instance.LogEvent("MovementStart", new Dictionary<string, string>
        {
            {"TrialNumber", (trialIndex + 1).ToString()},
            {"StartX", startX.ToString("F2")}, // Use rounded value
            {"StartY", startY.ToString("F2")}, // Use rounded value
            {"Timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}
        });
        }
    }

    // Add a new method for logging individual steps
    private void LogMovementStep(Vector2 startPos, Vector2 endPos, float stepDuration)
    {
        // Get trial index based on trial type
        int trialIndex = PlayerPrefs.GetInt("IsPracticeTrial", 0) == 1
            ? PracticeManager.Instance.GetCurrentPracticeTrialIndex()
            : ExperimentManager.Instance.GetCurrentTrialIndex();

        // Round the coordinates to 2 decimal places
        float startX = Mathf.Round(startPos.x * 100) / 100; // Round to 2 decimal places
        float startY = Mathf.Round(startPos.y * 100) / 100; // Round to 2 decimal places
        float endX = Mathf.Round(endPos.x * 100) / 100;     // Round to 2 decimal places
        float endY = Mathf.Round(endPos.y * 100) / 100;     // Round to 2 decimal places

        Debug.Log($"Original Start: ({startPos.x}, {startPos.y}), Rounded Start: ({startX}, {startY})");
        Debug.Log($"Original End: ({endPos.x}, {endPos.y}), Rounded End: ({endX}, {endY})");

        if (LogManager.Instance != null)
        {
            LogManager.Instance.LogEvent("MovementStep", new Dictionary<string, string>
        {
            {"TrialNumber", (trialIndex + 1).ToString()},
            {"StartX", startX.ToString("F2")}, // Use rounded value
            {"StartY", startY.ToString("F2")}, // Use rounded value
            {"EndX", endX.ToString("F2")},     // Use rounded value
            {"EndY", endY.ToString("F2")},     // Use rounded value
            {"StepDuration", stepDuration.ToString("F3")}, // Log the actual step duration
            {"Timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}
        });
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

        if (movementTimerStarted)
        {
            float movementDuration = Time.time - movementStartTime;

            // Determine if this is a practice trial
            bool isPracticeTrial = PlayerPrefs.GetInt("IsPracticeTrial", 0) == 1;

            // Get trial index based on trial type
            int trialIndex = isPracticeTrial
                ? PracticeManager.Instance.GetCurrentPracticeTrialIndex()
                : ExperimentManager.Instance.GetCurrentTrialIndex();

            // Get block number based on trial type - FIXED: use -1 for practice trials
            int blockNumber = isPracticeTrial
                ? -1 // Use -1 for practice blocks (consistent with PracticeDecisionManager)
                : ExperimentManager.Instance.GetCurrentBlockNumber();

            // Log movement duration for failure
            LogManager.Instance.LogEvent("MovementDuration", new Dictionary<string, string>
        {
            {"TrialNumber", (trialIndex + 1).ToString()},
            {"MovementDuration", movementDuration.ToString("F3")},
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

            // Log button presses
            LogButtonPressesAtTrialEnd(trialIndex);

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

            // Log decision outcome for failure - use the SAME method as success
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
                string.Join("|", buttonPressList.Select(bp => $"{bp.Key:F3}:{bp.Value}")), // pressData
                movementDuration / Mathf.Max(1, totalButtonPresses), // timePerPress
                0 // points for failure
            );

            // Mark as logged to prevent duplicate logs
            hasLoggedTrialOutcome = true;

            // Log movement end
            LogMovementEnd(currentPosition, false, true);

            // Reset movement tracking
            movementTimerStarted = false;

            // Notify GameController about failure
            GameController gameController = GameController.Instance;
            if (gameController != null)
            {
                // gameController.LogRewardCollectionTiming(Time.time, movementDuration);
                gameController.RewardCollected(false);
            }

            DisableMovement();
        }
    }
    #endregion

    // Then log the entire list at the end of the trial
    private void LogButtonPressesAtTrialEnd(int trialIndex)
    {
        string buttonPressData = string.Join("|", buttonPressList.Select(bp => $"{bp.Key:F3}:{bp.Value}"));

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