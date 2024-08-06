using UnityEngine;

public class DetectCollisions : MonoBehaviour
{
    [SerializeField] private GameController gameController;
    [SerializeField] private ScoreManager scoreManager;

    private void Start()
    {
        gameController ??= FindObjectOfType<GameController>();
        scoreManager ??= FindObjectOfType<ScoreManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Reward") && other.TryGetComponent<Reward>(out var reward))
        {
            scoreManager?.IncreaseScore(reward.scoreValue);
            gameController?.RewardCollected();
            Destroy(other.gameObject);
        }
    }
}