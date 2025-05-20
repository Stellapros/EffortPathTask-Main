using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

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
    [SerializeField] private float sliderArrowSpeed = 1f; // Speed for arrow key movement

    private void Awake()
    {
        // Force show cursor for this form scene
        ForceShowCursor();
    }

    private void OnEnable()
    {
        // Additional attempt to ensure cursor is visible
        ForceShowCursor();
    }

    private void OnDestroy()
    {
        // Hide cursor when leaving this scene
        HideCursor();
    }

    private void ForceShowCursor()
    {
        // Force cursor to be visible
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Debug info
        Debug.Log("Cursor should be visible. Current state: " +
                 (Cursor.visible ? "Visible" : "Not Visible") +
                 ", Lock state: " + Cursor.lockState);
    }

    private void HideCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Start()
    {
        // Set up UI elements
        promptText.text = "<color=#DED9AB>On a scale of 1 to 10, how tired do you feel right now?</color> \n ← 1 = not at all   10 = very much →";

        // Configure spaceInstructionText to prevent interaction
        if (spaceInstructionText != null)
        {
            // This prevents the text from receiving pointer events
            spaceInstructionText.raycastTarget = false;

            // Update instructions to include mouse
            spaceInstructionText.text = "<size=90%>Use LEFT/RIGHT arrows to move the slider \n\n Press 'Space' to continue</size>";
            
            // Enable rich text support for the TextMeshPro component
            spaceInstructionText.richText = true;
        }

        // Configure slider
        tirednessSlider.minValue = 1;
        tirednessSlider.maxValue = 10;
        tirednessSlider.value = 5; // Default value
        tirednessSlider.wholeNumbers = true;

        // Set up event listeners
        tirednessSlider.onValueChanged.AddListener(OnSliderValueChanged);
        continueButton.onClick.AddListener(OnContinueButtonClicked);

        // Ensure the slider handle can be interacted with
        if (tirednessSlider.GetComponentInChildren<Image>() != null)
        {
            // Make sure handle has a graphic raycaster
            if (!tirednessSlider.gameObject.GetComponent<GraphicRaycaster>())
            {
                tirednessSlider.gameObject.AddComponent<GraphicRaycaster>();
            }

            // Ensure the slider handle has an EventTrigger component for dragging
            GameObject handle = tirednessSlider.handleRect.gameObject;
            if (!handle.GetComponent<EventTrigger>())
            {
                // This is optional but can help with debugging
                Debug.Log("Adding EventTrigger to slider handle");
            }
        }

        // Initialize value text
        UpdateValueText(tirednessSlider.value);

        // Final cursor check
        ForceShowCursor();
    }

    private void Update()
    {
        // Handle arrow key controls for the slider
        HandleArrowKeyInput();

        // Check for Space key press to continue
        if (Input.GetKeyDown(KeyCode.Space))
        {
            OnContinueButtonClicked();
        }

        // Periodically ensure cursor is visible (can help with stubborn cursor issues)
        if (Time.frameCount % 30 == 0) // Check every 30 frames
        {
            if (!Cursor.visible)
            {
                Debug.LogWarning("Cursor visibility lost, forcing visibility again");
                ForceShowCursor();
            }
        }
    }

    private void HandleArrowKeyInput()
    {
        // Left arrow decreases the slider value
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            tirednessSlider.value -= sliderArrowSpeed * Time.deltaTime;
        }
        // Right arrow increases the slider value
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            tirednessSlider.value += sliderArrowSpeed * Time.deltaTime;
        }

        // For quick incremental movement, handle key down events
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            tirednessSlider.value = Mathf.Max(tirednessSlider.minValue, Mathf.Floor(tirednessSlider.value - 1));
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            tirednessSlider.value = Mathf.Min(tirednessSlider.maxValue, Mathf.Ceil(tirednessSlider.value + 1));
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