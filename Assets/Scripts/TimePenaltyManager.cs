using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class TimePenaltyManager : MonoBehaviour
{
    [SerializeField] private float penaltyDuration = 5f;
    [SerializeField] private TextMeshProUGUI penaltyText;
    private float currentPenaltyTime;
    
    private void Start()
    {
        currentPenaltyTime = penaltyDuration;
        UpdatePenaltyText();
    }
    
    private void Update()
    {
        if (currentPenaltyTime > 0)
        {
            currentPenaltyTime -= Time.deltaTime;
            UpdatePenaltyText();
            
            if (currentPenaltyTime <= 0)
            {
                PenaltyComplete();
            }
        }
    }
    
    private void UpdatePenaltyText()
    {
        if (penaltyText != null)
        {
            penaltyText.text = $"Oh, no decision made! Wait 5 seconds for the next chance... Stay focused!";
            // penaltyText.text = $"Oh, no decision made! Wait 5 seconds for the next chance... Stay focused! Time Penalty: {currentPenaltyTime:F0}s";
        }
    }

    private void PenaltyComplete()
    {
        // Return to decision phase for next trial
        SceneManager.LoadScene("DecisionPhase");
    }
}