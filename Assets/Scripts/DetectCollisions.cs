using UnityEngine;

public class DetectCollisions : MonoBehaviour
{
    [SerializeField] private GameController gameController;

    private void Start()
    {
        // If GameController is not assigned in the inspector, try to find it in the scene
        if (gameController == null)
            gameController = FindObjectOfType<GameController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the collided object is a reward
        if (other.CompareTag("Reward") && other.TryGetComponent<Reward>(out var reward))
        {
            // Notify the GameController that a reward was collected
            gameController.RewardCollected();
            
            // Destroy the reward object
            Destroy(other.gameObject);
        }
    }
}