using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetectCollisions : MonoBehaviour
{
    private ScoreManager scoreManager;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Reward"))
        {
            Reward goal = other.gameObject.GetComponent<Reward>();

            // Ensure the Goal component is not null before proceeding
            if (goal != null)
            {
                // Access or manipulate the Goal component as needed
                Debug.Log("Score value of the goal: " + goal.scoreValue);

                // Example actions: Increase score, destroy the goal object, etc.
            }
            else
            {
                Debug.LogError("Goal component not found on collided GameObject: " + other.gameObject.name);
            }
        }
        else
        {
            Debug.LogWarning("Collision detected with non-goal GameObject: " + other.gameObject.name);
        }

    }
}
