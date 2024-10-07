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

    /// <summary>
    /// Sets the parameters for the reward.
    /// </summary>
    /// <param name="block">The block index.</param>
    /// <param name="trial">The trial index.</param>
    /// <param name="presses">The number of presses required.</param>
    /// <param name="value">The score value of the reward.</param>
    public void SetRewardParameters(int block, int trial, int pressesRequired, int value)
    {
        blockIndex = block;
        trialIndex = trial;
        this.pressesRequired = pressesRequired;
        scoreValue = value;
        // Debug.Log($"Reward parameters set: Block {block}, Trial {trial}, Presses required {pressesRequired}, Value {value}");
    }

    // Getter methods
    // public int GetScoreValue() => scoreValue;
    // public int GetPressesRequired() => pressesRequired;
    // public (int, int) GetBlockAndTrialIndex() => (blockIndex, trialIndex);
}