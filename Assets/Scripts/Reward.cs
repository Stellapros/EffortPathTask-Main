using UnityEngine;

public class Reward : MonoBehaviour
{
    public float detectionRadius = 0.5f;
    public int scoreValue = 10;
    private ScoreManager scoreManager; // refer to tHe GameManager Script

    private void OnDrawGizmosSelected()
    {
        // This will draw a wire sphere in the Scene view to help visualize the detection radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }

    public bool IsPlayerInRange(Vector3 playerPosition)
    {
        return Vector3.Distance(transform.position, playerPosition) <= detectionRadius;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Add score
            ScoreManager.instance.AddScore(scoreValue);

            // Destroy the reward object
            Destroy(gameObject);
        }
    }
}