using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class EndExperiment : MonoBehaviour
{
    [SerializeField] private ExperimentConfig config;
    [SerializeField] private TextMeshProUGUI totalTimeText;
    [SerializeField] private TextMeshProUGUI totalScoreText;
    [SerializeField] private TMP_InputField feedbackInputField;
    [SerializeField] private Button submitButton;
    // [SerializeField] private string serverUrl = "https://your-server-url.com/api/submit-data";
    [SerializeField] public AudioClip buttonClickSound;
    private AudioSource audioSource;

    
    [Header("Input Field Settings")]
    [SerializeField] public Vector2 inputFieldSize = new Vector2(300, 50);
    [SerializeField] private int fontSize = 16;
    [SerializeField] private int characterLimit = 500;

    private float totalTime;
    private int totalScore;

    private void Start()
    {
        // Add null checks for serialized fields
        if (totalTimeText == null) Debug.LogError("totalTimeText is not assigned in the inspector!");
        if (totalScoreText == null) Debug.LogError("totalScoreText is not assigned in the inspector!");
        if (feedbackInputField == null) Debug.LogError("feedbackInputField is not assigned in the inspector!");
        if (submitButton == null) Debug.LogError("submitButton is not assigned in the inspector!");
        if (config == null)
        {
            Debug.LogError("ExperimentConfig is not assigned!");
        }

        SetupInputField();

        // Retrieve the experiment start time and calculate total time
        float startTime = PlayerPrefs.GetFloat("ExperimentStartTime", Time.time);
        totalTime = Time.time - startTime;

        DisplayTotalTime();
        DisplayTotalScore();

        if (submitButton != null)
        {
            submitButton.onClick.AddListener(SubmitFeedbackAndData);
        }
    }

    private void DisplayTotalTime()
    {
        if (totalTimeText != null)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(totalTime);
            totalTimeText.text = $"Total Time: {timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
        }
    }

    private void DisplayTotalScore()
    {
        // Get the total score from ScoreManager
        if (ScoreManager.Instance != null)
        {
            totalScore = ScoreManager.Instance.GetTotalScore();
        }
        else
        {
            Debug.LogError("ScoreManager instance not found!");
        }

        if (totalScoreText != null)
        {
            totalScoreText.text = $"Total Score: {totalScore}";
        }
    }

    private void SetupInputField()
    {
        if (feedbackInputField != null)
        {
            RectTransform rectTransform = feedbackInputField.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = inputFieldSize;
            }
            feedbackInputField.pointSize = fontSize;
            feedbackInputField.characterLimit = characterLimit;
            feedbackInputField.lineType = TMP_InputField.LineType.MultiLineNewline;
            if (feedbackInputField.textViewport != null)
            {
                feedbackInputField.textViewport.sizeDelta = new Vector2(inputFieldSize.x - 10, inputFieldSize.y - 10);
            }
        }
    }

    private void SubmitFeedbackAndData()
    {
        string feedback = feedbackInputField != null ? feedbackInputField.text : "";
        StartCoroutine(SubmitDataToServer(feedback));
    }

    private IEnumerator SubmitDataToServer(string feedback)
    {
        if (config == null)
        {
            Debug.LogError("ExperimentConfig is not assigned. Cannot submit data.");
            yield break;
        }

        WWWForm form = new WWWForm();
        form.AddField("totalTime", totalTime.ToString());
        form.AddField("totalScore", totalScore.ToString());
        form.AddField("feedback", feedback);

        using (UnityWebRequest www = UnityWebRequest.Post(config.ServerUrl, form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Data submitted successfully");
                ShowMessage("Thank you for your participation!");
            }
            else
            {
                Debug.LogError($"Error submitting data: {www.error}");
                ShowMessage("There was an error submitting your data. Please try again.");
            }
        }
    }

    private void ShowMessage(string message)
    {
        Debug.Log(message);
        // Implement UI feedback here
    }
}