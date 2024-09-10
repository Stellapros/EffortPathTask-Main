using UnityEngine;

public interface IExperimentManager
{
    /// <summary>
    /// Start a new trial
    /// </summary>
    void StartTrial();

    /// <summary>
    /// Skip the current trial
    /// </summary>
    void SkipTrial();

    /// <summary>
    /// End the current trial
    /// </summary>
    /// <param name="rewardCollected">True if the trial was completed, false otherwise</param>
    void EndTrial(bool rewardCollected);

    /// <summary>
    /// Get the sprite for the current trial's effort level
    /// </summary>
    /// <returns>The sprite for the current trial's effort level</returns>
    Sprite GetCurrentTrialSprite();

    /// <summary>
    /// Get the effort level for the current trial
    /// </summary>
    /// <returns>The effort level for the current trial</returns>
    float GetCurrentTrialEV();

    /// <summary>
    /// Get the player's starting position for the current trial
    /// </summary>
    /// <returns>The player's starting position for the current trial</returns>
    Vector2 GetCurrentTrialPlayerPosition();

    /// <summary>
    /// Get the reward position for the current trial
    /// </summary>
    /// <returns>The reward position for the current trial</returns>
    Vector2 GetCurrentTrialRewardPosition();

    /// <summary>
    /// Get the reward value for the current trial
    /// </summary>
    /// <returns>The reward value for the current trial</returns>
    float GetCurrentTrialRewardValue();

    /// <summary>
    /// Check if the current trial's time is up
    /// </summary>
    /// <returns>True if the trial time is up, false otherwise</returns>
    bool IsTrialTimeUp();

    /// <summary>
    /// Load the decision scene
    /// </summary>
    void LoadDecisionScene();
    void HandleDecision(bool workDecision);

    /// <summary>
    /// Event that is invoked when a trial ends
    /// </summary>
    event System.Action<bool> OnTrialEnded;

    /// <summary>
    /// Log trial data
    /// </summary>
    /// <param name="completed">Whether the trial was completed successfully</param>
    /// <param name="reactionTime">The reaction time for the trial</param>
    /// <param name="buttonPresses">The number of button presses during the trial</param>
    void LogTrialData(bool completed, float reactionTime, int buttonPresses);
}