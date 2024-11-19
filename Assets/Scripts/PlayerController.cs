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

        if (!isMoving)
        {
            isMoving = true;
            moveStartTime = Time.time;
        }
        HandleInput();
        // HandleMovement(); // Add this line to handle movement and facing direction
    }

    private void HandleInput()
    {
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
        directionCounters[index]++;
        totalButtonPresses++;

        Debug.Log($"Counter incremented. Total presses: {totalButtonPresses}, Presses needed: {pressesPerStep}");

        if (directionCounters[index] >= pressesPerStep)
        {
            AttemptMove(direction);
            ResetCounters();
        }
    }

    private void AttemptMove(Vector2 direction)
    {
        Vector2 newPosition = (Vector2)transform.position + (direction * moveStepSize);
        Debug.Log($"Attempting move to {newPosition}");

        if (gridManager != null && gridManager.IsValidPosition(newPosition))
        {
            MoveCharacter(newPosition);
            UpdateFacingDirection(direction); // This line ensures the facing direction is updated after each move
            PlayStepSound();

            // Update current position after successful move
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

    private void ResetCounters()
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

    // private void ApplyFacingDirection()
    // {
    //     // Apply the rotation based on lastHorizontalDirection
    //     transform.rotation = Quaternion.Euler(0, lastHorizontalDirection.x < 0 ? 180 : 0, 0);
    // }

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

    private void UpdateSpriteDirection(Vector2 direction)
    {
        // This method can be used to update sprite animations or states based on direction
        // For example:
        // if (direction.y > 0) SetAnimationState("FacingUp");
        // else if (direction.y < 0) SetAnimationState("FacingDown");
        // else if (direction.x != 0) SetAnimationState("FacingSide");

        // If you're using a sprite renderer, you might flip the sprite here
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = direction.x < 0;
        }
    }


    public Vector2 GetInitialPosition()
    {
        return initialPosition;
    }

    public Vector2 GetCurrentPosition()
    {
        return currentPosition;
    }

    public void SetPressesPerStep(int presses)
    {
        pressesPerStep = presses;
        ResetCounters();
        // Debug.Log($"Presses per step set to: {pressesPerStep}");
        Debug.Log($"PlayerController: Presses per step set to {pressesPerStep}");
    }

    public void UpdatePressesPerStep()
    {
        if (ExperimentManager.Instance != null)
        {
            pressesPerStep = ExperimentManager.Instance.GetCurrentTrialEV();
            Debug.Log($"PlayerController: Updated presses per step to {pressesPerStep}");
        }
        else
        {
            Debug.LogError("ExperimentManager.Instance is null in PlayerController.UpdatePressesPerStep");
        }
    }

    public void EnableMovement()
    {
        UpdatePressesPerStep(); // Add this line
        isTrialRunning = true;
        isMoving = false;
        moveStartTime = 0f;
        if (playerRigidbody != null)
        {
            playerRigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
        }
        Debug.Log("Player movement enabled");
    }

    public void DisableMovement()
    {
        isTrialRunning = false;
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

    public int GetButtonPressCount() => totalButtonPresses;
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

            // Disable player movement
            DisableMovement();

            // Invoke the reward collected event
            OnRewardCollected?.Invoke();

            Debug.Log("Reward collected!");
        }
    }
}