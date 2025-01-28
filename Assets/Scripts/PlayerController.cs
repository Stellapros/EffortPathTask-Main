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
    [SerializeField] private PracticeManager practiceManager; // Add SerializeField to allow setting in inspector
    private Vector2 lastMoveDirection = Vector2.right; // Initialize facing right
    private Vector2 lastNonZeroMovement = Vector2.right; // Initialize facing right
    private SpriteRenderer spriteRenderer;
    private Vector2 lastHorizontalDirection = Vector2.right; // To keep track of last horizontal movement; Initialize facing right

    private bool isTrialRunning = false;
    private float moveStartTime;
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



    private void Awake()
    {
        SetupSingleton();
        SetupComponents();
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
        UpdatePressesPerStep();
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

        // if (!isMoving)
        // {
        //     isMoving = true;
        //     moveStartTime = Time.time;
        // }

        // Remove the isMoving check here as it's not necessary and could cause issues
        HandleInput();
        // HandleMovement(); // Add this line to handle movement and facing direction
    }

    private void HandleInput()
    {
        // Only process input if we're not currently moving
        if (isMoving) return;

        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            IncrementCounter(0, Vector2.up);
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            IncrementCounter(1, Vector2.down);
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            IncrementCounter(2, Vector2.left);
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            IncrementCounter(3, Vector2.right);
    }

    private void IncrementCounter(int index, Vector2 direction)
    {
        Debug.Log($"IncrementCounter called - isTrialRunning: {isTrialRunning}, isMoving: {isMoving}, pressesPerStep: {pressesPerStep}");

        if (!isTrialRunning) return;  // Additional safety check

        directionCounters[index]++;
        totalButtonPresses++;

        Debug.Log($"Counter incremented. Total presses: {totalButtonPresses}, Presses needed: {pressesPerStep}");

        if (directionCounters[index] >= pressesPerStep && !isMoving)
        {
            isMoving = true;  // Set moving state before attempting move
            AttemptMove(direction);
            ResetCounters();
            isMoving = false;  // Reset moving state after move is complete
        }
            else
    {
        // If the number of key presses is insufficient, only update the direction without moving
        UpdateFacingDirection(direction);
    }
    }

    private void AttemptMove(Vector2 direction)
    {
        Vector2 startPosition = transform.position;
        Vector2 newPosition = (Vector2)transform.position + (direction * moveStepSize);
        float moveStartTime = Time.time;

        Debug.Log($"Attempting move from {startPosition} to {newPosition}");

        if (gridManager != null && gridManager.IsValidPosition(newPosition))
        {
            // Record movement before executing it
            OnMovementRecorded?.Invoke(startPosition, newPosition);

            // Log the movement
            if (LogManager.Instance != null)
            {
                float movementTime = Time.time - moveStartTime;
                // Get the current trial number from GameController or ExperimentManager
                int currentTrial = ExperimentManager.Instance?.GetCurrentTrialIndex() ?? 0;
                LogManager.Instance.LogPlayerMovement(currentTrial, startPosition, newPosition, movementTime);
            }

            MoveCharacter(newPosition);
            UpdateFacingDirection(direction);
            PlayStepSound();

            currentPosition = newPosition;
            Debug.Log($"Player moved. New position: {currentPosition}");
        }
        else
        {
            Debug.Log("Invalid move attempted. Playing error sound.");
            PlaySound(errorSound);
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

    private void UpdateFacingDirection(Vector2 direction)
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

    // private void UpdateSpriteDirection(Vector2 direction)
    // {
    //     // This method can be used to update sprite animations or states based on direction
    //     // For example:
    //     // if (direction.y > 0) SetAnimationState("FacingUp");
    //     // else if (direction.y < 0) SetAnimationState("FacingDown");
    //     // else if (direction.x != 0) SetAnimationState("FacingSide");

    //     // If you're using a sprite renderer, you might flip the sprite here
    //     SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
    //     if (spriteRenderer != null)
    //     {
    //         spriteRenderer.flipX = direction.x < 0;
    //     }
    // }
    public Vector2 GetInitialPosition() => initialPosition;
    public Vector2 GetCurrentPosition() => currentPosition;
    public int GetButtonPressCount() => totalButtonPresses;

    public void SetPressesPerStep(int presses)
    {
        pressesPerStep = presses;
        ResetCounters();
        // Debug.Log($"Presses per step set to: {pressesPerStep}");
        Debug.Log($"PlayerController: Presses per step set to {pressesPerStep}");
    }

    // public void UpdatePressesPerStep()
    // {
    //     if (ExperimentManager.Instance != null)
    //     {
    //         pressesPerStep = ExperimentManager.Instance.GetCurrentTrialEV();
    //         Debug.Log($"PlayerController: Updated presses per step to {pressesPerStep}");
    //     }
    //     else
    //     {
    //         Debug.LogError("ExperimentManager.Instance is null in PlayerController.UpdatePressesPerStep");
    //     }
    // }
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
        Debug.Log($"EnableMovement - pressesPerStep: {pressesPerStep}, isMoving: {isMoving}");
        
        isMoving = false; // Ensure we start in a non-moving state
        totalButtonPresses = 0;

        UpdatePressesPerStep();
        ResetCounters();  // Reset counters when enabling movement
        isTrialRunning = true;
        
        // moveStartTime = 0f;
        moveStartTime = Time.time;  // Initialize moveStartTime when movement is enabled 

        if (playerRigidbody != null)
        {
            playerRigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
        }
        Debug.Log("Player movement enabled");
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
            float collisionTime = Time.time;
            Debug.Log($"Player collided with reward at time: {Time.time}");
            float movementDuration = isMoving ? collisionTime - moveStartTime : 0f;

            GameController gameController = GameController.Instance;
            if (gameController != null)
            {
                gameController.LogRewardCollectionTiming(collisionTime, movementDuration);
                gameController.RewardCollected(true);
            }
            else
            {
                Debug.LogError("GameController not found in the scene!");
            }

            // Play reward sound
            PlaySound(rewardSound);

            // Disable the reward object
            other.gameObject.SetActive(false);

            // IMPORTANT: Invoke the reward collected event BEFORE disabling movement
            OnRewardCollected?.Invoke();

            // Disable player movement
            DisableMovement();

            Debug.Log("Reward collected!");
        }
    }
}