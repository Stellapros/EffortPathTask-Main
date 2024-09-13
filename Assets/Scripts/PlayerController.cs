using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    public event System.Action OnRewardCollected;

    [SerializeField] private float moveStepSize = 1.0f;
    [SerializeField] private int pressesPerStep = 1;
    [SerializeField] private bool trialRunning = false;

    private int upCounter = 0;
    private int downCounter = 0;
    private int leftCounter = 0;
    private int rightCounter = 0;
    private int totalButtonPresses = 0;

    private Rigidbody playerRigidbody;
    private AudioSource audioSource;

    private Vector2 lastMoveDirection;

    [SerializeField] private AudioClip errorSound;
    [SerializeField] private GridManager gridManager;

    private void Awake()
    {
        Debug.Log("PlayerController Awake called");

        playerRigidbody = GetComponent<Rigidbody>();
        ConfigureRigidbody();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (gridManager == null)
        {
            gridManager = FindObjectOfType<GridManager>();
            if (gridManager == null)
            {
                Debug.LogError("GridManager not found in the scene!");
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
        if (!trialRunning)
        {
            return;
        }
        HandleInput();
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            IncrementCounter(ref upCounter, Vector2.up);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            IncrementCounter(ref downCounter, Vector2.down);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            IncrementCounter(ref leftCounter, Vector2.left);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            IncrementCounter(ref rightCounter, Vector2.right);
        }
    }

    private void IncrementCounter(ref int counter, Vector2 direction)
    {
        counter++;
        totalButtonPresses++;
        lastMoveDirection = direction;

        Debug.Log($"Counter incremented. Total presses: {totalButtonPresses}, Presses needed: {pressesPerStep}");

        if (counter >= pressesPerStep)
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
        }
        else
        {
            Debug.Log("Invalid move attempted. Playing error sound.");
            PlayErrorSound();
        }
    }

    private void MoveCharacter(Vector2 newPosition)
    {
        Vector3 targetPosition = new Vector3(newPosition.x, newPosition.y, transform.position.z);
        Debug.Log($"Moving from {transform.position} to {targetPosition}");

        if (playerRigidbody != null)
        {
            playerRigidbody.MovePosition(targetPosition);
        }
        else
        {
            transform.position = targetPosition;
        }

        Debug.Log($"Player moved. New position: {transform.position}");
    }

    private void PlayErrorSound()
    {
        if (errorSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(errorSound);
            audioSource.volume = 0.5f;
            Debug.Log("Played error sound");
        }
        else
        {
            Debug.LogWarning("Unable to play error sound. Sound clip or AudioSource is missing.");
        }
    }

    private void ResetCounters()
    {
        upCounter = downCounter = leftCounter = rightCounter = 0;
    }

    public void ResetPosition(Vector2 position)
    {
        transform.position = new Vector3(position.x, position.y, transform.position.z);
        ResetCounters();
        totalButtonPresses = 0;
        if (playerRigidbody != null) playerRigidbody.velocity = Vector2.zero;
        Debug.Log($"Player position reset to: {position}, Total button presses reset to 0");
    }

    private void UpdateFacingDirection(Vector2 movement)
    {
        if (movement.x != 0)
        {
            // Flip the character sprite based on movement direction
            transform.localScale = new Vector3(Mathf.Sign(movement.x), 1, 1);
        }
    }

    public void SetPressesPerStep(int presses)
    {
        pressesPerStep = presses;
        ResetCounters();
        Debug.Log($"Presses per step set to: {pressesPerStep}");
    }

    public void EnableMovement()
    {
        trialRunning = true;
        Debug.Log("Player movement enabled");
    }

    public void DisableMovement()
    {
        trialRunning = false;
        Debug.Log("Player movement disabled");
    }

    public int GetButtonPressCount() => totalButtonPresses;

    // This method will be called by DetectCollisions
    public void HandleRewardCollection()
    {
        OnRewardCollected?.Invoke();
        totalButtonPresses = 0;
        Debug.Log("Reward collected! Button press count reset.");
    }
}