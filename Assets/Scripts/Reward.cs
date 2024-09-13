using UnityEngine;

/// <summary>
/// Represents a reward in the GridWorld scene.
/// </summary>
public class Reward : MonoBehaviour
{
    [SerializeField] private int scoreValue = 10;
     [SerializeField] private int pressesRequired;
    private int blockIndex;
    private int trialIndex;

    /// <summary>
    /// Sets the number of presses required to collect the reward.
    /// </summary>
       /// <param name="block">The block index.</param>
    /// <param name="trial">The trial index.</param>
    /// <param name="presses">The number of presses required.</param>
    public void SetRewardParameters(int block, int trial, int presses)
    {
        blockIndex = block;
        trialIndex = trial;
        pressesRequired = presses;
        Debug.Log($"Reward parameters set: Block {block}, Trial {trial}, Presses required {presses}");
    }

    /// <summary>
    /// Sets the score value of the reward.
    /// </summary>
    /// <param name="value">The score value to set.</param>
    public void SetValue(float value)
    {
        scoreValue = Mathf.RoundToInt(value);
        Debug.Log($"Reward value set to: {scoreValue}");
    }

    // Getter methods
    public int GetScoreValue() => scoreValue;
    public int GetPressesRequired() => pressesRequired;
    public (int, int) GetBlockAndTrialIndex() => (blockIndex, trialIndex);
}