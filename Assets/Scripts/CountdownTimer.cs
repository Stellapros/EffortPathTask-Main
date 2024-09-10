using UnityEngine;
using TMPro;

public class CountdownTimer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private float totalTime = 10.0f; // Default time set to 10 seconds
    private float timeLeft;
    private bool isRunning = false;

    // Event that can be subscribed to for when the timer reaches zero
    public event System.Action OnTimerExpired;

    // Public property to access remaining time
    public float TimeLeft => timeLeft;

    private void Start()
    {
        ResetTimer();
    }


    private void Update()
    {
        if (isRunning)
        {
            // Decrease time left
            timeLeft -= Time.deltaTime;
            UpdateTimerUI();
            
            // Check if time has run out
            if (timeLeft <= 0)
            {
                timeLeft = 0;
                isRunning = false;
                OnTimerExpired?.Invoke(); // Trigger the event
            }
        }
    }

    // Starts the timer with a specified duration
    public void StartTimer(float duration)
    {
        totalTime = duration;
        ResetTimer();
        isRunning = true;
    }

    // Stops the timer
    public void StopTimer()
    {
        isRunning = false;
    }

    // Resets the timer to the total time
    public void ResetTimer()
    {
        timeLeft = totalTime;
        UpdateTimerUI();
    }

    // Updates the UI text with the current time left
    private void UpdateTimerUI()
    {
        if (timerText != null)
        {
            timerText.text = $"Time: {Mathf.CeilToInt(timeLeft)}";
        }
    }
}