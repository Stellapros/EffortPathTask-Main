using UnityEngine;

public class Reward : MonoBehaviour
{
    public int scoreValue = 10;
    private int blockIndex;
    private int trialIndex;
    private int pressesRequired;

    public void SetRewardParameters(int block, int trial, int presses)
    {
        blockIndex = block;
        trialIndex = trial;
        pressesRequired = presses;
        // You can use these parameters to adjust the reward if needed
    }

    public void SetValue(float value)
    {
        scoreValue = Mathf.RoundToInt(value);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ScoreManager.Instance.AddScore(scoreValue);
            Destroy(gameObject);
        }
    }
}