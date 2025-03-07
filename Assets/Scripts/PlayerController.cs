using System;
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
    public event System.Action OnRewardCollected;

    [SerializeField] private float moveStepSize = 1.0f;
    [SerializeField] private int pressesPerStep = 1;
    [SerializeField] private AudioClip errorSound;
    [SerializeField] private AudioClip rewardSound;
    [SerializeField] private AudioClip stepSound; //step sound
    [SerializeField] private GridManager gridManager;
    [SerializeField] private PracticeManager practiceManager;
    private Vector2 lastMoveDirection = Vector2.right; // Initialize facing right
    private Vector2 lastNonZeroMovement = Vector2.right; // Initialize facing right
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

    // Add new delegate for movement tracking
    public delegate void MovementRecordedHandler(Vector2 startPos, Vector2 endPos);
    public event MovementRecordedHandler OnMovementRecorded;

    // Create a list to store button presses for later batch logging
    private List<KeyValuePair<float, string>> buttonPressList = new List<KeyValuePair<float, string>>();

    // Movement timing variables - consolidated
    private float trialStartTime;
    private float movementStartTime;
    private bool movementTimerStarted = false;


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

        // Modified PracticeManager initialization
        if (practiceManager == null)
        {
            practiceManager = FindAnyObjectByType<PracticeManager>();
            if (practiceManager == null)
            {
                Debug.LogWarning("PracticeManager not found - defaulting to ExperimentManager values");
            }
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

    private void AttemptMove(Vector2 direction)
    {
        Vector2 startPosition = transform.position;
        Vector2 newPosition = (Vector2)transform.position + (direction * moveStepSize);
        float stepStartTime = Time.time;

        Debug.Log($"Attempting move from {startPosition} to {newPosition}");

        // Log the start of movement
        LogMovementStart(startPosition);

        if (gridManager != null && gridManager.IsValidPosition(newPosition))
        {
            // Record movement before executing it
            OnMovementRecorded?.Invoke(startPosition, newPosition);

            MoveCharacter(newPosition);
            UpdateFacingDirection(direction);
            PlayStepSound();

            currentPosition = newPosition;
            float stepDuration = Time.time - stepStartTime;
            Debug.Log($"Player moved. New position: {currentPosition}, Step duration: {stepDuration}s");

            // Only log individual steps but don't reset the main movement timer
            LogMovementStep(startPosition, newPosition, stepDuration);
        }
        else
        {
            Debug.Log("Invalid move attempted. Playing error sound.");
            PlaySound(errorSound);
        }
    }

    // Add a new method for logging individual steps
    private void LogMovementStep(Vector2 startPos, Vector2 endPos, float stepDuration)
    {
        // Get trial index based on trial type
        int trialIndex = PlayerPrefs.GetInt("IsPracticeTrial", 0) == 1
            ? PracticeManager.Instance.GetCurrentPracticeTrialIndex()
            : ExperimentManager.Instance.GetCurrentTrialIndex();

        if (LogManager.Instance != null)
        {
            LogManager.Instance.LogEvent("MovementStep", new Dictionary<string, string>
            {
                {"TrialNumber", (trialIndex + 1).ToString()},
                {"StartX", startPos.x.ToString("F2")},
                {"StartY", startPos.y.ToString("F2")},
                {"EndX", endPos.x.ToString("F2")},
                {"EndY", endPos.y.ToString("F2")},
                {"StepDuration", stepDuration.ToString("F3")},
                {"Timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}
            });
        }
    }

    private void MoveCharacter(Vector2 newPosition)
    {
        Vector3 targetPosition = new Vector3(newPosition.x, newPosition.y, transform.position.z);
        Debug.Log($"Moving from {transform.position} to {targetPosition}");

        if (playerRigidbody != null)
            playerRigidbody.MovePosition(targetPosition);
        else
            transform.position = targetPosition;

        Debug.Log($"Player moved. New position: {transform.position}");
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
            audioSource.volume = 0.5f;
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
                int effortLevel = practiceManager.GetCurrentTrialEffortLevel();
                pressesPerStep = effortLevel;
                Debug.Log($"Practice trial - using effort level: {effortLevel}");
            }
            else if (ExperimentManager.Instance != null)
            {
                // Fallback to ExperimentManager if PracticeManager is not available
                pressesPerStep = ExperimentManager.Instance.GetCurrentTrialEV();
                Debug.Log($"Practice trial but no PracticeManager - using ExperimentManager value: {pressesPerStep}");
            }
            else
            {
                // Ultimate fallback
                pressesPerStep = 1;
                Debug.LogError("No managers found - defaulting to 1 press per step");
            }
        }
        else
        {
            // Normal trial logic
            if (ExperimentManager.Instance != null)
            {
                pressesPerStep = ExperimentManager.Instance.GetCurrentTrialEV();
                Debug.Log($"Normal trial - using ExperimentManager value: {pressesPerStep}");
            }
            else
            {
                pressesPerStep = 1;
                Debug.LogError("ExperimentManager not found - defaulting to 1 press per step");
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

    // Modify OnTriggerEnter
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Reward") && isTrialRunning)
        {
            float collisionTime = Time.time;

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

            // Get trial index based on trial type
            bool isPracticeTrial = PlayerPrefs.GetInt("IsPracticeTrial", 0) == 1;
            int trialIndex = isPracticeTrial
                ? PracticeManager.Instance.GetCurrentPracticeTrialIndex()
                : ExperimentManager.Instance.GetCurrentTrialIndex();

            // Log final decision outcome with accurate movement data
            LogManager.Instance.LogDecisionOutcome(
                trialIndex,
                PlayerPrefs.GetInt("CurrentBlock", 0),
                PlayerPrefs.GetString("DecisionType", "Work"),
                true, // rewardCollected
                PlayerPrefs.GetFloat("DecisionTime", 0f),
                movementDuration,
                totalButtonPresses,
                PlayerPrefs.GetInt("EffortLevel", 1),
                PlayerPrefs.GetInt("RequiredPresses", 1)
            );

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

            // Log collision timing
            LogManager.Instance.LogEvent("CollisionTime", new Dictionary<string, string>
            {
                {"TrialNumber", (trialIndex + 1).ToString()},
                {"Time", collisionTime.ToString("F3")}
            });

            // Handle reward collection
            GameController gameController = GameController.Instance;
            if (gameController != null)
            {
                gameController.LogRewardCollectionTiming(collisionTime, movementDuration);
                gameController.RewardCollected(true);
            }

            // Play reward sound and disable reward object
            PlaySound(rewardSound);
            other.gameObject.SetActive(false);

            // Invoke reward collected event before disabling movement
            OnRewardCollected?.Invoke();
            LogButtonPressesAtTrialEnd(trialIndex);
            DisableMovement();
        }
    }

    #region Movement Logging
    private Vector2 movementStartPosition;

    private void LogMovementStart(Vector2 startPosition)
    {
        movementStartPosition = startPosition;

        // Get trial index based on trial type
        int trialIndex = PlayerPrefs.GetInt("IsPracticeTrial", 0) == 1
            ? PracticeManager.Instance.GetCurrentPracticeTrialIndex()
            : ExperimentManager.Instance.GetCurrentTrialIndex();

        if (LogManager.Instance != null)
        {
            LogManager.Instance.LogEvent("MovementStart", new Dictionary<string, string>
            {
                {"TrialNumber", (trialIndex + 1).ToString()},
                {"StartX", startPosition.x.ToString("F2")},
                {"StartY", startPosition.y.ToString("F2")},
                {"Timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}
            });
        }
    }

    public int GetTotalButtonPresses()
    {
        return totalButtonPresses;
    }

    // Replace LogMovementEnd with a simpler version that doesn't reset the timer
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
        if (movementTimerStarted)
        {
            float movementDuration = Time.time - movementStartTime;

            // Get trial index based on trial type
            int trialIndex = PlayerPrefs.GetInt("IsPracticeTrial", 0) == 1
                ? PracticeManager.Instance.GetCurrentPracticeTrialIndex()
                : ExperimentManager.Instance.GetCurrentTrialIndex();

            // Log movement duration for failure
            LogManager.Instance.LogEvent("MovementDuration", new Dictionary<string, string>
            {
                {"TrialNumber", (trialIndex + 1).ToString()},
                {"MovementDuration", movementDuration.ToString("F3")},
                {"OutcomeType", "Fail"}
            });

            // Log decision outcome for failure
            LogManager.Instance.LogDecisionOutcome(
                trialIndex,
                PlayerPrefs.GetInt("CurrentBlock", 0),
                PlayerPrefs.GetString("DecisionType", "NoDecision"),
                false, // rewardCollected
                PlayerPrefs.GetFloat("DecisionTime", 0f),
                movementDuration,
                totalButtonPresses,
                PlayerPrefs.GetInt("EffortLevel", 1),
                PlayerPrefs.GetInt("RequiredPresses", 1)
            );

            // Reset movement tracking
            movementTimerStarted = false;
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