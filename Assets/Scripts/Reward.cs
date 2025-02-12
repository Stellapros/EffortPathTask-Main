using UnityEngine;

/// <summary>
/// Represents a reward in the GridWorld scene.
/// </summary>
public class Reward : MonoBehaviour
{
    [SerializeField] private int scoreValue;
    [SerializeField] private int pressesRequired;
    private int blockIndex;
    private int trialIndex;

    public void SetRewardParameters(int block, int trial, int pressesRequired, int value)
    {
        blockIndex = block;
        trialIndex = trial;
        this.pressesRequired = pressesRequired;
        scoreValue = value;
        // Debug.Log($"Reward parameters set: Block {block}, Trial {trial}, Presses required {pressesRequired}, Value {value}");
    }
}