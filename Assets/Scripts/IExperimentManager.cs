using UnityEngine;

public interface IExperimentManager
{
    void StartExperiment();
    void StartTrial();
    void SkipTrial();
    void EndTrial(bool completed);
    Sprite GetCurrentTrialSprite();
    float GetCurrentTrialEV();
    Vector2 GetCurrentTrialPlayerPosition();
    bool IsTrialTimeUp();
    void LoadDecisionScene();

    bool EndTrial();
}