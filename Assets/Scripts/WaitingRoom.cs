using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class WaitingRoomManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI waitingText;
    [SerializeField] private float minWaitTime = 1f;
    [SerializeField] private float maxWaitTime = 5f;
    private LogManager logManager;

    private void Awake()
    {
        logManager = LogManager.Instance;
        if (waitingText == null)
        {
            waitingText = GetComponentInChildren<TextMeshProUGUI>();
        }
    }

    private void Start()
    {
        StartCoroutine(WaitAndTransition());
    }

    private IEnumerator WaitAndTransition()
    {
        float waitTime = Random.Range(minWaitTime, maxWaitTime);
        float remainingTime = waitTime;

        // Log wait period start
        if (logManager != null)
        {
            logManager.LogEvent("WaitingPeriod_Start", new Dictionary<string, string>
            {
                { "Duration", waitTime.ToString("F2") }
            });
        }

        while (remainingTime > 0)
        {
            if (waitingText != null)
            {
                waitingText.text = $"Please wait...\n{remainingTime:F1}s";
            }

            yield return new WaitForSeconds(0.1f);
            remainingTime -= 0.1f;
        }

        // Log wait period end
        if (logManager != null)
        {
            logManager.LogEvent("WaitingPeriod_End", new Dictionary<string, string>());
        }

        SceneManager.LoadScene("DecisionPhase");
    }
}