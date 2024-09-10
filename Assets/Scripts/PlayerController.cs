using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))] // This attribute ensures that a Rigidbody component is always added to the GameObject
public class PlayerController : MonoBehaviour
{
    // Event triggered when the player collects a reward
    public event System.Action OnRewardCollected;

    [SerializeField] private float moveStepSize = 1.0f;
    [SerializeField] private int pressesPerStep = 1; // determines how many presses are needed before movement occurs
    [SerializeField] private bool trialRunning = false;


    private int upCounter = 0;
    private int downCounter = 0;
    private int leftCounter = 0;
    private int rightCounter = 0;
    private Vector2 moveDirection;
    private bool canMove = false;

    // Reference to the player's Rigidbody component
    private Rigidbody playerRigidbody;

    // Reference to the AudioSource component
    private AudioSource audioSource;

    // Reference to the error sound clip
    [SerializeField] private AudioClip errorSound;

    // Reference to the GridManager
    [SerializeField] private GridManager gridManager;

    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// </summary>
    private void Awake()
    {
        Debug.Log("PlayerController Awake called");

        // Get the Rigidbody2D component
        playerRigidbody = GetComponent<Rigidbody>();

        // Configure the Rigidbody for 2D-style movement in a 3D space
        if (playerRigidbody != null)
        {
            playerRigidbody.useGravity = false; // Disable gravity since we're controlling movement manually
            playerRigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ; // Freeze rotation and Z-axis movement
            playerRigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous; // Use continuous collision detection for better accuracy
            playerRigidbody.interpolation = RigidbodyInterpolation.Interpolate; // Smooth out movement between physics updates
        }
        else
        {
            Debug.LogError("Rigidbody component not found on the player object! This shouldn't happen due to RequireComponent attribute.");
        }

        // Get or add the AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Find the GridManager in the scene if not already assigned
        if (gridManager == null)
        {
            gridManager = FindObjectOfType<GridManager>();
            if (gridManager == null)
            {
                Debug.LogError("GridManager not found in the scene!");
            }
        }
    }

    /// <summary>
    /// Start is called before the first frame update
    /// </summary>
    private void Start()
    {
        // Check if the error sound is assigned
        if (errorSound == null)
        {
            Debug.LogWarning("Error sound clip is not assigned to the PlayerController!");
        }
    }


    /// <summary>
    /// Update is called once per frame. Handles player input and movement.
    /// </summary>
    private void Update()
    {
        if (!trialRunning)
        {
            Debug.Log("Trial not running, movement disabled");
            return;
        }

        bool keyPressed = false;
        Vector2 movementDirection = Vector2.zero;

        // Check for input and set movement direction
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            Debug.Log("Up key pressed");
            IncrementCounter(ref upCounter, Vector2.up);
            keyPressed = true;
            movementDirection = Vector2.up;
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            Debug.Log("Down key pressed");
            IncrementCounter(ref downCounter, Vector2.down);
            keyPressed = true;
            movementDirection = Vector2.down;
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            Debug.Log("Left key pressed");
            IncrementCounter(ref leftCounter, Vector2.left);
            keyPressed = true;
            movementDirection = Vector2.left;
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            Debug.Log("Right key pressed");
            IncrementCounter(ref rightCounter, Vector2.right);
            keyPressed = true;
            movementDirection = Vector2.right;
        }

        if (keyPressed)
        {
            Debug.Log($"Key pressed. Counters - Up: {upCounter}, Down: {downCounter}, Left: {leftCounter}, Right: {rightCounter}");
        }

        // If a movement key was pressed, attempt to move
        if (movementDirection != Vector2.zero)
        {
            AttemptMove(movementDirection);
        }
    }


    /// <summary>
    /// Attempts to move the player in the specified direction.
    /// </summary>
    /// <param name="direction">Direction of movement</param>
    private void AttemptMove(Vector2 direction)
    {
        Vector2 newPosition = (Vector2)transform.position + (direction * moveStepSize);

        // Check if the new position is valid using the GridManager
        if (gridManager != null && gridManager.IsValidPosition(newPosition))
        {
            MoveCharacter(newPosition);
        }
        else
        {
            Debug.Log("Invalid move attempted. Playing error sound.");
            PlayErrorSound();
        }
    }

    /// <summary>
    /// Resets the player's position and counters.
    /// </summary>
    /// <param name="position">The new position for the player</param>
    public void ResetPosition(Vector2 position)
    {
        transform.position = position;
        ResetCounters();
        if (playerRigidbody != null) playerRigidbody.velocity = Vector2.zero;
        Debug.Log($"Player position reset to: {position}");
    }


    /// <summary>
    /// Sets the number of presses required for a single movement.
    /// </summary>
    /// <param name="presses">Number of presses required</param>
    public void SetRequiredPresses(int presses)
    {
        SetPressesPerStep(presses);
    }

    /// <summary>
    /// Sets the number of presses required for a single movement.
    /// </summary>
    /// <param name="presses">Number of presses required</param>
    public void SetPressesPerStep(int presses)
    {
        pressesPerStep = presses;
        ResetCounters();
        Debug.Log($"Presses per step set to: {pressesPerStep}");
    }


    /// <summary>
    /// Enables player movement by setting trialRunning to true.
    /// </summary>
    // control the trialRunning boolean
    public void EnableMovement()
    {
        trialRunning = true;
        Debug.Log("Player movement enabled");
    }

    /// <summary>
    /// Disables player movement by setting trialRunning to false.
    /// </summary>
    public void DisableMovement()
    {
        trialRunning = false;
        Debug.Log("Player movement disabled");
    }

    /// <summary>
    /// Increments the counter for a direction and moves the character if the threshold is reached.
    /// </summary>
    /// <param name="counter">Reference to the direction counter</param>
    /// <param name="direction">Direction of movement</param>
    private void IncrementCounter(ref int counter, Vector2 direction)
    {
        counter++;
        if (counter >= pressesPerStep)
        {
            AttemptMove(direction);
            ResetCounters();
            //counter++; // Keeping the original logic, incrementing the counter for this direction after moving
        }
    }


    /// <summary>
    /// Moves the character in the specified direction.
    /// </summary>
    /// <param name="direction">Direction of movement</param>
    // updates the transform position
    private void MoveCharacter(Vector2 newPosition)
    {
        Vector2 oldPosition = transform.position;
        if (playerRigidbody != null)
        {
            // Use MovePosition for physics-based movement
            playerRigidbody.MovePosition(new Vector3(newPosition.x, newPosition.y, transform.position.z));
        }
        else
        {
            // Fallback to transform-based movement if Rigidbody is somehow missing
            transform.position = new Vector3(newPosition.x, newPosition.y, transform.position.z);
        }
        Debug.Log($"Player moved from {oldPosition} to {newPosition}");
    }


    /// <summary>
    /// Plays an error sound when an invalid move is attempted.
    /// </summary>
    private void PlayErrorSound()
    {
        if (errorSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(errorSound);
            audioSource.volume = 0.5f; // Adjust this value as needed (0.0f to 1.0f)
            Debug.Log("Played error sound");
        }
        else
        {
            Debug.LogWarning("Unable to play error sound. Sound clip or AudioSource is missing.");
        }
    }


    /// <summary>
    /// Resets all direction counters to zero.
    /// </summary>
    private void ResetCounters()
    {
        upCounter = 0;
        downCounter = 0;
        leftCounter = 0;
        rightCounter = 0;
    }



    /// <summary>
    /// Starts the trial, allowing player movement.
    /// </summary>
    public void StartTrial() => trialRunning = true;

    /// <summary>
    /// Ends the trial, disabling player movement.
    /// </summary>
    public void EndTrial() => trialRunning = false;

    /// <summary>
    /// Called when the player's collider enters a trigger collider.
    /// Used to detect when the player collects a reward.
    /// </summary>
    /// <param name="other">The other collider involved in the collision</param>
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Reward"))
        {
            OnRewardCollected?.Invoke();
            Debug.Log("Reward collected!");
        }
    }
}

