using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CountdownTimer : MonoBehaviour
{
    public TextMeshProUGUI timerText;
    public float totalTime = 5.0f; // Total time in seconds
    private float timeLeft;
    
    // Start is called before the first frame update
    
    void Start()
    {
        timeLeft = totalTime;
        UpdateTimerUI();
    }

    void Update()
    {
        if (timeLeft > 0)
        {
            timeLeft -= Time.deltaTime;
            UpdateTimerUI();
        }
        else
        {
            timeLeft = 0;
            // Handle timer expiration, e.g., end game, show score screen, etc.
        }
    }

    void UpdateTimerUI()
    {
        if (timerText != null)
        {
            timerText.text = "Time: " + Mathf.CeilToInt(timeLeft);
        }
    }
}
