using System.Diagnostics;
using UnityEngine;
using TMPro;
using Debug = UnityEngine.Debug;


public class CountdownTimer : MonoBehaviour
{
    public bool IsInitialized { get; private set; }

    [SerializeField] private TextMeshProUGUI timerText; // Reference to the UI text component
    [SerializeField] private float totalTime = 10.0f; // Default time set to 10 seconds
    private float timeLeft;
    private bool isRunning = false;

    // Event that can be subscribed to for when the timer reaches zero
    public event System.Action OnTimerExpired;

    // Public property to access remaining time
    public float TimeLeft => timeLeft;
    private Stopwatch stopwatch;

    private void Start()
    {
        // ResetTimer();
        stopwatch = new Stopwatch();
    }

    public void Initialize()
    {

        // Initialize any necessary timer components here
        IsInitialized = true;

        // Ensure all necessary components are set up
        if (timerText == null)
        {
            timerText = GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (timerText == null)
            {
                Debug.LogWarning("Timer text component not found. Creating new one...");
                GameObject textObj = new GameObject("TimerText");
                textObj.transform.SetParent(transform);
                timerText = textObj.AddComponent<TMPro.TextMeshProUGUI>();
            }
        }

        // Reset timer state
        isRunning = false;
        timeLeft = 0f;
        // startTime = 0f;

        // Update display
        UpdateTimerUI();
    }

    private void Update()
    {
        if (isRunning)
        {
            // Calculate elapsed time using Stopwatch for more accuracy
            float elapsedTime = stopwatch.ElapsedMilliseconds / 1000f;
            timeLeft = Mathf.Max(totalTime - elapsedTime, 0);
            // timeLeft -= Time.deltaTime;

            UpdateTimerUI();

            // Check if time has run out
            if (timeLeft <= 0)
            {
                UnityEngine.Debug.Log("Timer reached zero. Stopping timer and invoking OnTimerExpired.");
                // isRunning = false;
                // timeLeft = 0;
                StopTimer();
                OnTimerExpired?.Invoke(); // Trigger the event
            }
            // UpdateTimerUI();
            UnityEngine.Debug.Log($"Timer Update: timeLeft = {timeLeft:F2}s");
        }

        if (Time.frameCount % 60 == 0) // Log every 60 frames to reduce spam
        {
            UnityEngine.Debug.Log($"Timer Update: timeLeft = {timeLeft:F2}s, isRunning = {isRunning}, totalTime = {totalTime}");
        }
    }

    // Starts the timer with a specified duration
    public void StartTimer(float duration)
    {
        UnityEngine.Debug.Log($"StartTimer called with duration: {duration}");
        totalTime = duration;
        timeLeft = totalTime;
        isRunning = true;
        // startTime = Time.time;
        stopwatch.Restart();
        UpdateTimerUI();
        UnityEngine.Debug.Log($"Timer started at {Time.realtimeSinceStartup}. totalTime: {totalTime}, timeLeft: {timeLeft}, isRunning: {isRunning}");
    }


    // Stops the timer
    public void StopTimer()
    {
        isRunning = false;
        // timeLeft = totalTime;
        stopwatch.Stop();
        UpdateTimerUI();
        UnityEngine.Debug.Log($"Timer stopped at {Time.realtimeSinceStartup}. Elapsed time: {stopwatch.ElapsedMilliseconds / 1000f}s");
    }

    // Resets the timer to the total time
    public void ResetTimer()
    {
        isRunning = false;
        timeLeft = totalTime;
        stopwatch.Reset();
        UpdateTimerUI();
        UnityEngine.Debug.Log($"Timer reset at {Time.time}");
    }

    /// <summary>
    /// Sets the total duration for the timer.
    /// If the timer is running, it will be reset with the new duration.
    /// </summary>
    /// <param name="duration">The new duration in seconds</param>
    public void SetDuration(float duration)
    {
        UnityEngine.Debug.Log($"SetDuration called with duration: {duration}");
        totalTime = Mathf.Max(0, duration); // Ensure duration is not negative

        // If timer is running, restart it with new duration
        if (isRunning)
        {
            StopTimer();
            StartTimer(totalTime);
        }
        else
        {
            // If timer is not running, just update the time left
            timeLeft = totalTime;
            UpdateTimerUI();
        }

        UnityEngine.Debug.Log($"Timer duration set to {totalTime}s. Current state - timeLeft: {timeLeft}, isRunning: {isRunning}");
    }

    // Updates the UI text with the current time left
    private void UpdateTimerUI()
    {
        if (timerText != null)
        {
            timerText.text = $"Time: {timeLeft:F1}";
        }
    }

    // Method to check timer accuracy (useful for debugging)
    public void CheckTimerAccuracy()
    {
        if (isRunning)
        {
            // float actualTime = Time.time - startTime;
            float actualTime = stopwatch.ElapsedMilliseconds / 1000f;
            float timerTime = totalTime - timeLeft;
            UnityEngine.Debug.Log($"Actual time passed: {actualTime:F2}s, Timer time: {timerTime:F2}s, Difference: {(actualTime - timerTime):F2}s");
        }
        else
        {
            UnityEngine.Debug.Log("Timer is not running.");
        }
    }
}