using System.Diagnostics;
using UnityEngine;
using TMPro;
using Debug = UnityEngine.Debug;

public class CountdownTimer : MonoBehaviour
{
    public bool IsInitialized { get; private set; }

    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private float totalTime = 10.0f;
    // [SerializeField] private Color warningColor = new Color(0.6f, 0.2f, 0.2f); // dard red

    // ToRGB: 0.4 × 255 = 102; 1.0 × 255 = 255; 0.4 × 255 = 102
    [SerializeField] private Color warningColor = new Color(0.694f, 0.925f, 0.180f); // #B1EC2E
    [SerializeField] private Color normalColor = new Color(0.584f, 0.761f, 0.749f); // #95C2BF

    private float timeLeft;
    private bool isRunning = false;
    private bool isInWarningPhase = false;
    public event System.Action OnTimerExpired;
    public float TimeLeft => timeLeft;
    private Stopwatch stopwatch;

    private void Start()
    {
        stopwatch = new Stopwatch();
    }

    public void Initialize()
    {
        IsInitialized = true;

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

        isRunning = false;
        timeLeft = 0f;
        isInWarningPhase = false;
        timerText.color = normalColor;
        UpdateTimerUI();
    }

    private void Update()
    {
        if (isRunning)
        {
            float elapsedTime = stopwatch.ElapsedMilliseconds / 1000f;
            timeLeft = Mathf.Max(totalTime - elapsedTime, 0);

            if (timeLeft <= 2f && !isInWarningPhase)
            {
                isInWarningPhase = true;
                timerText.color = warningColor;
            }

            UpdateTimerUI();

            if (timeLeft <= 0)
            {
                StopTimer();
                OnTimerExpired?.Invoke();
            }
        }

        if (Time.frameCount % 60 == 0)
        {
            // Debug.Log($"Timer Update: timeLeft = {timeLeft:F2}s, isRunning = {isRunning}, totalTime = {totalTime}");
        }
    }

    public void StartTimer(float duration)
    {
        Debug.Log("StartTimer called"); // Debug log
        totalTime = duration;
        timeLeft = totalTime;
        isRunning = true;
        isInWarningPhase = false;
        timerText.color = normalColor;
        stopwatch.Restart();
        UpdateTimerUI();
        Debug.Log($"Timer started with duration: {duration}s");

        PlayerController.Instance?.EnableMovement();

        // Added debug logs for PlayModeIndicator
        if (PlayModeIndicator.Instance != null)
        {
            Debug.Log("Found PlayModeIndicator instance, showing play mode");
            PlayModeIndicator.Instance.ShowPlayMode();
        }
        else
        {
            Debug.LogError("PlayModeIndicator.Instance is null!");
        }
    }

    public void StopTimer()
    {
        isRunning = false;
        stopwatch.Stop();
        isInWarningPhase = false;
        timerText.color = normalColor;
        UpdateTimerUI();
        Debug.Log($"Timer stopped at {Time.realtimeSinceStartup}. Elapsed time: {stopwatch.ElapsedMilliseconds / 1000f}s");

        // Hide play mode indicator when timer stops
        if (PlayModeIndicator.Instance != null)
        {
            PlayModeIndicator.Instance.HidePlayMode();
        }
    }

    public void ResetTimer()
    {
        isRunning = false;
        timeLeft = totalTime;
        stopwatch.Reset();
        isInWarningPhase = false;
        timerText.color = normalColor;
        UpdateTimerUI();
        Debug.Log($"Timer reset at {Time.time}");
    }

    public void SetDuration(float duration)
    {
        Debug.Log($"SetDuration called with duration: {duration}");
        totalTime = Mathf.Max(0, duration);

        if (isRunning)
        {
            StopTimer();
            StartTimer(totalTime);
        }
        else
        {
            timeLeft = totalTime;
            UpdateTimerUI();
        }

        Debug.Log($"Timer duration set to {totalTime}s. Current state - timeLeft: {timeLeft}, isRunning: {isRunning}");
    }

    private void UpdateTimerUI()
    {
        if (timerText != null)
        {
            timerText.text = $"Time: {timeLeft:F0}";
        }
    }

    public void CheckTimerAccuracy()
    {
        if (isRunning)
        {
            float actualTime = stopwatch.ElapsedMilliseconds / 1000f;
            float timerTime = totalTime - timeLeft;
            // Debug.Log($"Actual time passed: {actualTime:F2}s, Timer time: {timerTime:F2}s, Difference: {(actualTime - timerTime):F2}s");
        }
        else
        {
            Debug.Log("Timer is not running.");
        }
    }
}