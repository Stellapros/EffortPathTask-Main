using UnityEngine;
using TMPro;

public class CountdownTimer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private float totalTime = 10.0f;
    private float timeLeft;
    private bool isRunning = false;

    public event System.Action OnTimerExpired;

    public float TimeLeft => timeLeft;

    private void Start()
    {
        ResetTimer();
    }

    private void Update()
    {
        if (isRunning)
        {
            timeLeft -= Time.deltaTime;
            UpdateTimerUI();
            if (timeLeft <= 0)
            {
                timeLeft = 0;
                isRunning = false;
                OnTimerExpired?.Invoke();
            }
        }
    }

    public void StartTimer()
    {
        isRunning = true;
        if (timeLeft <= 0)
        {
            ResetTimer();
        }
    }

    public void ResetTimer()
    {
        timeLeft = totalTime;
        UpdateTimerUI();
    }

    private void UpdateTimerUI()
    {
        if (timerText != null)
        {
            timerText.text = $"Time: {Mathf.CeilToInt(timeLeft)}";
        }
    }
}
