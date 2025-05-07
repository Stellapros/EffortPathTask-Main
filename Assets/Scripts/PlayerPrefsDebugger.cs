using UnityEngine;
using System.Collections.Generic;
using System.Text;
using UnityEngine.UI;
using TMPro;

public class PlayerPrefsDebugger : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI debugText;
    [SerializeField] private KeyCode toggleKey = KeyCode.F12;

    private bool showDebugOverlay = false;
    private Dictionary<string, string> previousValues = new Dictionary<string, string>();

    private string[] keysToCheck = new string[] {
        "PracticeAttempts",
        "NeedsPracticeRetry",
        "FailedCheck",
        "IsPracticeTrial",
        "CurrentPracticeTrialIndex",
        "CurrentPracticeEffortLevel",
        "BlockOfficiallyStarted",
        "BlockStartTime",
        "CurrentPracticeBlockIndex",
        "CurrentPracticeBlockType",
        "PracticeBlocksCompleted",
        "ResumeFromCheck",
        "RetryInProgress"
    };

    void Start()
    {
        if (debugText == null)
        {
            // Create canvas and text if not assigned
            CreateDebugOverlay();
        }

        // Initialize previous values
        foreach (string key in keysToCheck)
        {
            previousValues[key] = GetPlayerPrefValue(key);
        }

        debugText.gameObject.SetActive(showDebugOverlay);
        InvokeRepeating("UpdateDebugText", 0f, 0.5f);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            showDebugOverlay = !showDebugOverlay;
            debugText.gameObject.SetActive(showDebugOverlay);
        }
    }

    private void CreateDebugOverlay()
    {
        // Create canvas
        GameObject canvasObj = new GameObject("DebugCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // Create text object
        GameObject textObj = new GameObject("DebugText");
        textObj.transform.SetParent(canvas.transform, false);

        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0.6f);
        rect.anchorMax = new Vector2(0.4f, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(10, -10);
        rect.sizeDelta = new Vector2(-20, -20);

        debugText = textObj.AddComponent<TextMeshProUGUI>();
        debugText.fontSize = 14;
        debugText.color = Color.white;
        debugText.alignment = TextAlignmentOptions.TopLeft;

        // Add background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(textObj.transform, false);
        bgObj.transform.SetAsFirstSibling();

        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = new Vector2(10, 10);

        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.7f);

        DontDestroyOnLoad(canvasObj);
    }

    private void UpdateDebugText()
    {
        if (!showDebugOverlay) return;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("<b>PLAYERPREFS DEBUG (Press F12 to toggle)</b>");
        sb.AppendLine("--------------------------------");

        bool hasChanges = false;

        foreach (string key in keysToCheck)
        {
            string currentValue = GetPlayerPrefValue(key);
            bool valueChanged = currentValue != previousValues[key];

            // Format: changed values in yellow
            if (valueChanged)
            {
                sb.AppendLine($"<color=yellow>{key} = {currentValue}</color>");
                previousValues[key] = currentValue;
                hasChanges = true;
            }
            else
            {
                sb.AppendLine($"{key} = {currentValue}");
            }
        }

        // Add current block time remaining if in practice
        if (PracticeManager.Instance != null && PlayerPrefs.GetInt("IsPracticeTrial", 0) == 1)
        {
            float blockStartTime = PlayerPrefs.GetFloat("BlockStartTime", 0);
            float blockDuration = 5f; // Same as your PracticeManager
            float timeRemaining = blockDuration - (Time.time - blockStartTime);

            if (blockStartTime > 0 && PlayerPrefs.GetInt("BlockOfficiallyStarted", 0) == 1)
            {
                sb.AppendLine("--------------------------------");
                sb.AppendLine($"<color=cyan>Block Time: {timeRemaining:F1}s remaining</color>");
            }
        }

        debugText.text = sb.ToString();

        // Flash if changes detected
        if (hasChanges)
        {
            // Get the background image
            Image bg = debugText.transform.GetChild(0).GetComponent<Image>();
            if (bg != null)
            {
                bg.color = new Color(0.3f, 0.3f, 0, 0.7f);
                Invoke("ResetBackgroundColor", 0.3f);
            }
        }
    }

    private void ResetBackgroundColor()
    {
        Image bg = debugText.transform.GetChild(0).GetComponent<Image>();
        if (bg != null)
        {
            bg.color = new Color(0, 0, 0, 0.7f);
        }
    }

    private string GetPlayerPrefValue(string key)
    {
        if (!PlayerPrefs.HasKey(key))
            return "<i>not set</i>";

        if (key.Contains("Time"))
        {
            float value = PlayerPrefs.GetFloat(key, 0);
            return value.ToString("F2");
        }
        else if (key == "CurrentPracticeBlockType")
        {
            return PlayerPrefs.GetString(key, "");
        }
        else
        {
            return PlayerPrefs.GetInt(key, 0).ToString();
        }
    }

    // Static utility method that can be called from any script
    public static void LogAllPlayerPrefs()
    {
        Debug.Log("==== PLAYER PREFS DUMP ====");
        foreach (string key in new string[] {
            "PracticeAttempts", "NeedsPracticeRetry", "FailedCheck", "IsPracticeTrial",
            "CurrentPracticeTrialIndex", "CurrentPracticeEffortLevel", "BlockOfficiallyStarted",
            "BlockStartTime", "CurrentPracticeBlockIndex", "CurrentPracticeBlockType",
            "PracticeBlocksCompleted", "ResumeFromCheck", "RetryInProgress"
        })
        {
            if (PlayerPrefs.HasKey(key))
            {
                if (key.Contains("Time"))
                    Debug.Log($"{key} = {PlayerPrefs.GetFloat(key)}");
                else if (key == "CurrentPracticeBlockType")
                    Debug.Log($"{key} = {PlayerPrefs.GetString(key)}");
                else
                    Debug.Log($"{key} = {PlayerPrefs.GetInt(key)}");
            }
            else
            {
                Debug.Log($"{key} = <not set>");
            }
        }
        Debug.Log("=========================");
    }
}