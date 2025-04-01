using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class TirednessRatingScreen : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider tirednessSlider;
    [SerializeField] private TextMeshProUGUI sliderValueText;
    [SerializeField] private Button continueButton;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private TextMeshProUGUI spaceInstructionText;

    [Header("Settings")]
    [SerializeField] private string endExperimentSceneName = "EndExperiment";

    private void Awake()
    {
        // Show cursor for this form scene
        ShowCursor();
    }

    private void OnDestroy()
    {
        // Hide cursor when leaving this scene
        HideCursor();
    }

    private void ShowCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void HideCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }


    private void Start()
    {
        // Set up UI elements
        promptText.text = "On a scale of 1 to 10, how tired do you feel right now?";

        // Set up space instruction text
        if (spaceInstructionText != null)
        {
            spaceInstructionText.text = "Use your mouse to move the slider. \n Press 'Space' to continue";
        }
        else
        {
            Debug.LogWarning("Space instruction text reference is missing. Please assign it in the inspector.");
        }

        // Configure slider
        tirednessSlider.minValue = 1;
        tirednessSlider.maxValue = 10;
        tirednessSlider.value = 5; // Default value
        tirednessSlider.wholeNumbers = true;

        // Set up event listeners
        tirednessSlider.onValueChanged.AddListener(OnSliderValueChanged);
        continueButton.onClick.AddListener(OnContinueButtonClicked);

        // Initialize value text
        UpdateValueText(tirednessSlider.value);
    }

    private void Update()
    {
        // Check for Space key press
        if (Input.GetKeyDown(KeyCode.Space))
        {
            OnContinueButtonClicked();
        }
    }

    private void OnSliderValueChanged(float value)
    {
        UpdateValueText(value);
    }

    private void UpdateValueText(float value)
    {
        sliderValueText.text = $"{value:0}/10";
    }

    private void OnContinueButtonClicked()
    {
        // Get the rating value
        int tirednessRating = Mathf.RoundToInt(tirednessSlider.value);

        // Log the tiredness rating using the existing LogManager
        LogTirednessRating(tirednessRating);

        // Proceed to the EndExperiment scene
        SceneManager.LoadScene(endExperimentSceneName);
    }

    private void LogTirednessRating(int tirednessRating)
    {
        // Use the existing LogManager to log the tiredness rating
        if (LogManager.Instance != null)
        {
            LogManager.Instance.LogEvent("TirednessRating", new System.Collections.Generic.Dictionary<string, string>
        {
            {"Rating", tirednessRating.ToString()},
            {"Scale", "1-10"},
            {"ScaleDescription", "1=Not tired at all, 10=Extremely tired"},
            {"AdditionalInfo", $"Tiredness rating collected before EndExperiment scene"},
            {"TirednessRating", tirednessRating.ToString()} // Add this line to log the tiredness rating
        });

            Debug.Log($"Tiredness rating logged: {tirednessRating}/10");
        }
        else
        {
            Debug.LogError("LogManager instance not found. Tiredness rating could not be logged.");

            // Fallback to PlayerPrefs if LogManager is not available
            PlayerPrefs.SetInt("TirednessRating", tirednessRating);
            PlayerPrefs.Save();

            Debug.Log($"Tiredness rating saved to PlayerPrefs as fallback: {tirednessRating}/10");
        }
    }
}