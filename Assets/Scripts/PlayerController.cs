using UnityEngine;
using UnityEngine.SceneManagement; // For restarting the scene

public class PlayerController : MonoBehaviour
{
    public float stepDistance = 1f;
    public int pressesPerStep = 5;
    public int totalTrials = 20;

    private Vector3 movement;
    private int pressCounter = 0;
    private int currentTrial = 1;
    private int totalMoves = 0;

    void Start()
    {
        Debug.Log($"Trial {currentTrial} of {totalTrials} started");
    }

    void Update()
    {
        if (currentTrial > totalTrials)
        {
            Debug.Log("All trials completed!");
            return; // Exit the update method if all trials are done
        }

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            IncrementCounter(Vector3.forward);
        }
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            IncrementCounter(Vector3.back);
        }
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            IncrementCounter(Vector3.left);
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            IncrementCounter(Vector3.right);
        }

        // Check for 'R' key to restart the current trial
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartTrial();
        }

        // Check for 'N' key to move to the next trial
        if (Input.GetKeyDown(KeyCode.N))
        {
            NextTrial();
        }
    }

    void IncrementCounter(Vector3 direction)
    {
        pressCounter++;
        movement = direction;
        Debug.Log($"Trial {currentTrial}: Button pressed. Counter: {pressCounter}, Direction: {direction}");

        if (pressCounter >= pressesPerStep)
        {
            Move();
            pressCounter = 0;
        }
    }

    void Move()
    {
        Vector3 oldPosition = transform.position;
        transform.Translate(movement * stepDistance);
        Vector3 newPosition = transform.position;
        totalMoves++;
        Debug.Log($"Trial {currentTrial}: Moved from {oldPosition} to {newPosition}. Total moves: {totalMoves}");

        // You can add your own condition here to determine when a trial ends
        // For example, if the player reaches a certain position or after a certain number of moves
        if (totalMoves >= 10) // Example: End trial after 10 moves
        {
            NextTrial();
        }
    }

    void NextTrial()
    {
        currentTrial++;
        if (currentTrial <= totalTrials)
        {
            Debug.Log($"Trial {currentTrial} of {totalTrials} started");
            ResetPlayerPosition();
            totalMoves = 0;
            pressCounter = 0;
        }
        else
        {
            Debug.Log("All trials completed!");
        }
    }

    void RestartTrial()
    {
        Debug.Log($"Restarting Trial {currentTrial}");
        ResetPlayerPosition();
        totalMoves = 0;
        pressCounter = 0;
    }

    void ResetPlayerPosition()
    {
        // Reset the player to the starting position
        transform.position = Vector3.zero; // Or whatever your starting position is
    }
}