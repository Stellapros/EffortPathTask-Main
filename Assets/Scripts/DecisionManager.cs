using UnityEngine;

/// <summary>
/// Controls the decision phase of each trial, where the participant chooses to work or skip.
/// This script acts as a bridge between the EffortSpriteUI and the ExperimentManager.
/// </summary>
public class DecisionManager : MonoBehaviour
{
    [SerializeField] private EffortSpriteUI effortSpriteUI;
    private ExperimentManager experimentManager;

    private void Start()
    {
        experimentManager = ExperimentManager.Instance;
        if (experimentManager == null)
        {
            Debug.LogError("ExperimentManager not found!");
            return;
        }

        if (effortSpriteUI == null)
        {
            effortSpriteUI = FindObjectOfType<EffortSpriteUI>();
            if (effortSpriteUI == null)
            {
                Debug.LogError("EffortSpriteUI not found in the scene!");
                return;
            }
        }
        SetupDecisionPhase();
    }

    public void SetupDecisionPhase()
    {
        Debug.Log("DecisionManager: Setting up decision phase");
        ShowEffortSprite();
    }
    /// <summary>
    /// Displays the EffortSpriteUI to start the decision phase.
    /// </summary>
    private void ShowEffortSprite()
    {
        effortSpriteUI.Show(OnDecisionMade);
    }

    /// <summary>
    /// Callback method for when a decision is made in the EffortSpriteUI.
    /// This method passes the decision to the ExperimentManager.
    /// </summary>
    /// <param name="workDecision">True if the participant chose to work, false if they chose to skip.</param>
    private void OnDecisionMade(bool workDecision)
    {
        Debug.Log($"DecisionManager: Decision made - {(workDecision ? "Work" : "Skip")}");
        experimentManager.HandleDecision(workDecision);
    }
}