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
    private Vector2 lastHorizontalDirection = Vector2.right; // To keep track of last horizontal movement

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

        audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            Debug.Log("AudioSource component added to PlayerController.");
        }

        gridManager = gridManager ?? FindObjectOfType<GridManager>();
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

    // private int GetDirectionIndex(Vector2 direction)
    // {
    //     if (direction == Vector2.up) return 0;
    //     if (direction == Vector2.down) return 1;
    //     if (direction == Vector2.left) return 2;
    //     if (direction == Vector2.right) return 3;
    //     return -1;
    // }

    // private void HandleMovement()
    // {
    //     // Check if we should move based on the counter
    //     int directionIndex = System.Array.IndexOf(directionCounters, pressesPerStep);
    //     if (directionIndex != -1)
    //     {
    //         Vector2 moveDirection = GetDirectionFromIndex(directionIndex);
    //         AttemptMove(moveDirection);
    //         ResetCounters();
    //     }

    //     // Always update facing direction, even if not moving
    //     UpdateFacingDirection(currentMovementVector);
    // }


    // private Vector2 GetDirectionFromIndex(int index)
    // {
    //     switch (index)
    //     {
    //         case 0: return Vector2.up;
    //         case 1: return Vector2.down;
    //         case 2: return Vector2.left;
    //         case 3: return Vector2.right;
    //         default: return Vector2.zero;
    //     }
    // }

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
            UpdateFacingDirection(direction);
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
        if (playerRigidbody != null) playerRigidbody.velocity = Vector2.zero;
        ApplyFacingDirection(); // Apply the last known facing direction
        Debug.Log($"Player position reset to: {position}, Total button presses reset to 0");
    }

    // private void UpdateFacingDirection(Vector2 movement)
    // {
    //     if (movement.x != 0)
    //         transform.localScale = new Vector3(Mathf.Sign(movement.x), 1, 1);
    // }

    // negative scale or size on the BoxCollider is likely due to the player's scale being set to a negative value. 
    // This can happen when you're flipping the player sprite to face left or right

    // private void UpdateFacingDirection(Vector2 direction)
    // {
    //     if (direction.x != 0)
    //     {
    //         lastHorizontalDirection = direction;
    //         // Only update rotation for horizontal movement
    //         transform.rotation = Quaternion.Euler(0, direction.x < 0 ? 180 : 0, 0);
    //     }

    //     // Only update rotation for horizontal movement
    //     if (lastMoveDirection.x != 0)
    //     {
    //         transform.rotation = Quaternion.Euler(0, lastMoveDirection.x < 0 ? 180 : 0, 0);
    //     }
    //     // For vertical movement, we don't change the facing direction
    // }
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
        // Apply the rotation based on lastHorizontalDirection
        transform.rotation = Quaternion.Euler(0, lastHorizontalDirection.x < 0 ? 180 : 0, 0);
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
        Debug.Log($"Presses per step set to: {pressesPerStep}");
    }

    public void EnableMovement()
    {
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
            playerRigidbody.velocity = Vector3.zero;
            playerRigidbody.constraints = RigidbodyConstraints.FreezeAll;
        }
        // Reset all input counters
        ResetCounters();
        totalButtonPresses = 0;
        Debug.Log("Player movement disabled from PlayerController");
    }

    public int GetButtonPressCount() => totalButtonPresses;

    // public void HandleRewardCollection(GameObject rewardObject)
    // {
    //     if (!isTrialRunning) return;

    //     isTrialRunning = false;
    //     if (playerRigidbody != null)
    //         playerRigidbody.constraints = RigidbodyConstraints.FreezeAll;

    //     rewardObject.SetActive(false);
    //     PlaySound(rewardSound);

    //     GameController gameController = FindObjectOfType<GameController>();
    //     if (gameController != null)
    //         gameController.RewardCollected(true);
    //     else
    //         Debug.LogError("GameController not found in the scene!");

    //     OnRewardCollected?.Invoke();
    //     Debug.Log("Reward collected! Player frozen.");
    // }

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

    // private void OnTriggerEnter(Collider other)
    // {
    //     if (other.CompareTag("Reward") && isTrialRunning )
    //     {
    //         float collisionTime = Time.time;
    //         float movementDuration = isMoving ? collisionTime - moveStartTime : 0f;

    //         GameController gameController = GameController.Instance;
    //         if (gameController != null)
    //         {
    //             gameController.LogRewardCollectionTiming(collisionTime, movementDuration);
    //             gameController.RewardCollected(true);
    //         }
    //         else
    //         {
    //             Debug.LogError("GameController not found in the scene!");
    //         }

    //         // Disable the reward object
    //         // other.gameObject.SetActive(false);

    //         // Disable player movement
    //         // DisableMovement();

    //         // Play reward sound
    //         PlaySound(rewardSound);

    //         // Invoke the reward collected event
    //         OnRewardCollected?.Invoke();

    //         Debug.Log("Reward collected! Player frozen.");
    //     }
    // }
}