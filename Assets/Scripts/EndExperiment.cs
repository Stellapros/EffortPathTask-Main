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
    [Header("Feedback UI")]
    [SerializeField] private GameObject feedbackPanel;
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private Button closeMessageButton;
    [SerializeField] private float messageDuration = 3f;
    [SerializeField] private Color successColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    [SerializeField] private Color errorColor = new Color(0.8f, 0.2f, 0.2f, 1f);


    [Header("Input Field Settings")]
    [SerializeField] public Vector2 inputFieldSize = new Vector2(300, 50);
    [SerializeField] private int fontSize = 16;
    [SerializeField] private int characterLimit = 500;

    private float totalTime;
    private int totalScore;
    private bool isSubmitting = false;
    private AudioSource audioSource;
    private Coroutine hideMessageCoroutine;

    // [Header("Data Collection")]
    // [SerializeField] private string logFileName = "gameplay_log.csv"; // The name of your CSV file
    // [SerializeField] private bool deleteLocalFileAfterUpload = true;
    
    private void Start()
    {
        // Add null checks for serialized fields
        if (totalTimeText == null) Debug.LogError("totalTimeText is not assigned in the inspector!");
        if (totalScoreText == null) Debug.LogError("totalScoreText is not assigned in the inspector!");
        if (feedbackInputField == null) Debug.LogError("feedbackInputField is not assigned in the inspector!");
        if (submitButton == null) Debug.LogError("submitButton is not assigned in the inspector!");
        // if (feedbackPanel == null) Debug.LogError("feedbackPanel is not assigned in the inspector!");
        // if (feedbackText == null) Debug.LogError("feedbackText is not assigned in the inspector!");
        if (config == null) Debug.LogError("ExperimentConfig is not assigned!");

        // Initial UI setup
        if (feedbackPanel != null)
        {
            feedbackPanel.SetActive(false);
        }

        if (closeMessageButton != null)
        {
            closeMessageButton.onClick.AddListener(() => HideMessage());
        }

        // Show cursor and make it interactable
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

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

        // Add button navigation controller
        ButtonNavigationController navigationController = gameObject.AddComponent<ButtonNavigationController>();
        navigationController.AddElement(submitButton);
        navigationController.AddElement(feedbackInputField);
        if (closeMessageButton != null)
        {
            navigationController.AddElement(closeMessageButton);
        }

        // Setup audio source if needed
        if (buttonClickSound != null && audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void Update()
    {
        // Check for Space key press when not typing in input field
        if (Input.GetKeyDown(KeyCode.Space) && !feedbackInputField.isFocused && !isSubmitting)
        {
            SubmitFeedbackAndData();
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
        if (isSubmitting) return; // Prevent multiple submissions

        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }

        isSubmitting = true;
        submitButton.interactable = false; // Disable button during submission
        string feedback = feedbackInputField != null ? feedbackInputField.text : "";
        StartCoroutine(SubmitDataToServer(feedback));
    }

    private IEnumerator SubmitDataToServer(string feedback)
    {
        if (config == null)
        {
            Debug.LogError("ExperimentConfig is not assigned. Cannot submit data.");
            isSubmitting = false;
            submitButton.interactable = true;
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
                submitButton.interactable = true; // Re-enable button on error
            }
        }
        isSubmitting = false;
    }

    private void ShowMessage(string message, bool isError = false)
    {
        if (feedbackPanel == null || feedbackText == null) return;

        // Stop any existing hide message coroutine
        if (hideMessageCoroutine != null)
        {
            StopCoroutine(hideMessageCoroutine);
        }

        // Set up the feedback UI
        feedbackText.text = message;
        feedbackText.color = isError ? errorColor : successColor;
        feedbackPanel.SetActive(true);

        // Animate using Unity's built-in animation system
        StartCoroutine(AnimatePanel(true));

        // Start coroutine to hide message after duration
        hideMessageCoroutine = StartCoroutine(HideMessageAfterDelay());
    }


    private void HideMessage()
    {
        if (feedbackPanel == null) return;
        StartCoroutine(AnimatePanel(false));
    }

    private IEnumerator AnimatePanel(bool show)
    {
        float duration = 0.3f;
        float elapsed = 0f;
        
        Vector3 startScale = show ? Vector3.zero : Vector3.one;
        Vector3 endScale = show ? Vector3.one : Vector3.zero;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            // Add some easing
            float easedProgress = show ? 
                Mathf.Sin(progress * Mathf.PI * 0.5f) : // Ease out
                1f - Mathf.Sin((1f - progress) * Mathf.PI * 0.5f); // Ease in
            
            feedbackPanel.transform.localScale = Vector3.Lerp(startScale, endScale, easedProgress);
            yield return null;
        }
        
        feedbackPanel.transform.localScale = endScale;
        
        if (!show)
        {
            feedbackPanel.SetActive(false);
        }
    }

    private IEnumerator HideMessageAfterDelay()
    {
        yield return new WaitForSeconds(messageDuration);
        HideMessage();
    }
}