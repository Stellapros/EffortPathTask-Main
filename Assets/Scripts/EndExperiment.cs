using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.SceneManagement;

public class EndExperiment : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI totalTimeText;
    [SerializeField] private TMP_InputField feedbackInputField;
    [SerializeField] private Button submitButton;

    [Header("Input Field Settings")]
    [SerializeField] private Vector2 inputFieldSize = new Vector2(800, 400); // Width, Height
    [SerializeField] private int fontSize = 16;
    [SerializeField] private int characterLimit = 500;

    private float startTime;

    private void Start()
    {
        SetupInputField();

        // Assume we've stored the start time when the experiment began
        startTime = PlayerPrefs.GetFloat("ExperimentStartTime", Time.time);

        // Calculate and display total time
        float totalTime = Time.time - startTime;
        TimeSpan timeSpan = TimeSpan.FromSeconds(totalTime);
        totalTimeText.text = $"Total Time: {timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";

        // Add listener to the submit button
        submitButton.onClick.AddListener(SubmitFeedback);
    }

    private void SetupInputField()
    {
        // Set the size of the input field
        RectTransform rectTransform = feedbackInputField.GetComponent<RectTransform>();
        rectTransform.sizeDelta = inputFieldSize;

        // Set the font size
        feedbackInputField.pointSize = fontSize;

        // Set character limit
        feedbackInputField.characterLimit = characterLimit;

        // Make it multi-line
        feedbackInputField.lineType = TMP_InputField.LineType.MultiLineNewline;

        // Optionally, adjust text area size within the input field
        feedbackInputField.textViewport.sizeDelta = new Vector2(inputFieldSize.x - 10, inputFieldSize.y - 10);
    }

    private void SubmitFeedback()
    {
        string feedback = feedbackInputField.text;
        
        // Here you would typically send this feedback to a server or save it locally
        Debug.Log($"Feedback submitted: {feedback}");

        // For demonstration, we'll save it to PlayerPrefs
        PlayerPrefs.SetString("ExperimentFeedback", feedback);
        PlayerPrefs.Save();

        // You might want to show a thank you message or load a different scene here
        // SceneManager.LoadScene("ThankYouScene");
    }
}