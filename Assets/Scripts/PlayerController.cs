using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    public event System.Action OnRewardCollected;

    [SerializeField] private float moveStepSize = 1.0f;
    [SerializeField] public int pressesPerStep = 5; // determines how many presses are needed before movement occurs
    [SerializeField] private bool trialRunning = false;


    public int upCounter = 0;
    public int downCounter = 0;
    public int leftCounter = 0;
    public int rightCounter = 0;
    private Vector2 moveDirection;
    private bool canMove = false;
    // public Rigidbody rb;
    public Rigidbody playerRigidbody;

    private void Awake()
    {
        Debug.Log("PlayerController Awake called");
        playerRigidbody = GetComponent<Rigidbody>();
        if (playerRigidbody == null)
        {
            Debug.LogError("Rigidbody component not found on the player object!");
        }
    }

    private void Start()
    {
        Debug.Log("PlayerController Start called");
    }


    public void ResetPosition(Vector2 position)
    {
        transform.position = position;
        ResetCounters();
        if (playerRigidbody != null) playerRigidbody.velocity = Vector2.zero;
        Debug.Log($"Player position reset to: {position}");
    }

    // Renamed moveStepTreshold to pressesPerStep for clarity
    // Replaced SetMovementTreshold method with SetPressesPerStep
    // public void SetMovementTreshold(int treshold)
    // {
    //     moveStepTreshold = treshold;
    //     ResetCounters();
    // }
    // Updated the IncrementCounter method to use pressesPerStep instead of moveStepTreshold.

    public void SetPressesPerStep(int presses)
    {
        pressesPerStep = presses;
        ResetCounters();
        Debug.Log($"Presses per step set to: {pressesPerStep}");
    }



    // control the trialRunning boolean
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

    private void Update()
    {
        if (!trialRunning)
        {
            Debug.Log("Trial not running, movement disabled");
            return;
        }

        bool keyPressed = true;
        Vector2 movement = Vector2.zero;

        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            Debug.Log("Up key pressed");
            IncrementCounter(ref upCounter, Vector2.up);
            keyPressed = true;
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            IncrementCounter(ref downCounter, Vector2.down);
            keyPressed = true;
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            IncrementCounter(ref leftCounter, Vector2.left);
            keyPressed = true;
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            IncrementCounter(ref rightCounter, Vector2.right);
            keyPressed = true;
        }

        if (keyPressed)
        {
            Debug.Log($"Key pressed. Counters - Up: {upCounter}, Down: {downCounter}, Left: {leftCounter}, Right: {rightCounter}");
        }
        if (movement != Vector2.zero)
        {
            MoveCharacter(movement.normalized);
        }
    }

    private void IncrementCounter(ref int counter, Vector2 direction)
    {
        counter++;
        if (counter >= pressesPerStep)
        {
            MoveCharacter(direction);
            ResetCounters();
            counter++; // Keeping the original logic, incrementing the counter for this direction after moving
        }
    }

    // updates the transform position
    private void MoveCharacter(Vector2 direction)
    {
        Vector2 oldPosition = transform.position;
        transform.position += (Vector3)(direction * moveStepSize);
        Debug.Log($"Player moved from {oldPosition} to {transform.position}");
    }

    private void ResetCounters()
    {
        upCounter = 0;
        downCounter = 0;
        leftCounter = 0;
        rightCounter = 0;
    }

    public void StartTrial() => trialRunning = true;
    public void EndTrial() => trialRunning = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Reward"))
        {
            OnRewardCollected?.Invoke();
            Debug.Log("Reward collected!");
        }
    }
}