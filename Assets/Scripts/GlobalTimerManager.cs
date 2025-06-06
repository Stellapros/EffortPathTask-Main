using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class UniversalTimerOverlay : MonoBehaviour
{
    public static UniversalTimerOverlay Instance { get; private set; }

    // Timer UI elements
    [Header("Timer UI Components")]
    public TextMeshProUGUI timerText;
    public Canvas timerCanvas;
    public RectTransform timerRectTransform;

    [Header("Timer Styling")]
    public Color timerColor = new Color(0.67f, 0.87f, 0.86f);
    public int fontSize = 30;
    public FontStyles fontStyle = FontStyles.Bold;
    
    // Add font asset reference
    [Header("Font Settings")]
    public TMP_FontAsset electronicHighwayFont;

    // Tracking time
    private float startTime;
    private bool isTimerRunning = true;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        SetupTimerOverlay();
    }

    private void SetupTimerOverlay()
    {
        // Create canvas if not already set
        if (timerCanvas == null)
        {
            GameObject canvasObject = new GameObject("TimerCanvas");
            timerCanvas = canvasObject.AddComponent<Canvas>();
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            // Ensure canvas is on top of everything
            timerCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            timerCanvas.sortingOrder = 999; // Highest possible sorting order
        }

        // Create TextMeshProUGUI if not already set
        if (timerText == null)
        {
            GameObject textObject = new GameObject("TimerText");
            textObject.transform.SetParent(timerCanvas.transform, false);

            timerText = textObject.AddComponent<TextMeshProUGUI>();

            // Default styling
            timerText.color = timerColor;
            timerText.fontSize = fontSize;
            timerText.fontStyle = fontStyle;

            // Make text left-aligned for better readability with two lines
            timerText.alignment = TextAlignmentOptions.Left;

            // Load and set the Electronic Highway Sign font
            if (electronicHighwayFont == null)
            {
                // Try to load the font from TextMeshPro resources
                electronicHighwayFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/Electronic Highway Sign SDF");
            }
            
            // Apply the font if found
            if (electronicHighwayFont != null)
            {
                timerText.font = electronicHighwayFont;
                Debug.Log("Electronic Highway Sign font applied successfully");
            }
            else
            {
                Debug.LogWarning("Could not load Electronic Highway Sign font");
            }
        }

        // Ensure RectTransform is set up
        timerRectTransform = timerText.rectTransform;

        // Position in bottom-right corner with padding
        timerRectTransform.anchorMin = new Vector2(1, 0);
        timerRectTransform.anchorMax = new Vector2(1, 0);
        timerRectTransform.pivot = new Vector2(1, 0);
        timerRectTransform.anchoredPosition = new Vector2(30, 10); // 30 pixel padding from bottom-right

        // Set a wider width for the timer text
        timerRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 500f);
        timerRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 100f);

        // Optional: Add outline or shadow
        timerText.enableAutoSizing = false;
        timerText.outlineWidth = 0.2f;
        timerText.outlineColor = Color.black;

        // Ensure it persists across scenes
        DontDestroyOnLoad(timerCanvas.gameObject);

        // Start timer
        startTime = Time.time;
    }

    private void Update()
    {
        // Update timer display if running
        if (isTimerRunning && timerText != null)
        {
            float currentTime = Time.time - startTime;
            UpdateTimerDisplay(currentTime);
        }
    }

    private void UpdateTimerDisplay(float time)
    {
        // Format time as hours:minutes:seconds with milliseconds
        TimeSpan timePlayed = TimeSpan.FromSeconds(time);

        // Get current system time
        DateTime currentTime = DateTime.Now;

        // Combine elapsed game time and current system time
        timerText.text = string.Format(
            "Game Time: {0:D2}:{1:D2}:{2:D2}.<size=50%>{3:D3}</size>\n" +
            "Current Time: {4:HH}:{4:mm}:{4:ss}",
            timePlayed.Hours,
            timePlayed.Minutes,
            timePlayed.Seconds,
            timePlayed.Milliseconds,
            currentTime);
    }

    // Additional utility methods
    public void PauseTimer()
    {
        isTimerRunning = false;
    }

    public void ResumeTimer()
    {
        isTimerRunning = true;
    }

    public float GetTotalTime()
    {
        return Time.time - startTime;
    }

    // Method to customize timer appearance
    public void CustomizeTimerAppearance(
        int? fontSize = null,
        Color? textColor = null,
        FontStyles? fontStyle = null,
        TMP_FontAsset customFont = null)
    {
        if (timerText != null)
        {
            if (fontSize.HasValue)
                timerText.fontSize = fontSize.Value;

            if (textColor.HasValue)
                timerText.color = textColor.Value;

            if (fontStyle.HasValue)
                timerText.fontStyle = fontStyle.Value;
                
            if (customFont != null)
                timerText.font = customFont;
        }
    }
    
    // New method to set font by name
    public void SetTimerFont(string fontName)
    {
        // Try to load the font by name
        TMP_FontAsset fontAsset = Resources.Load<TMP_FontAsset>($"Fonts & Materials/{fontName}");
        
        if (fontAsset != null && timerText != null)
        {
            timerText.font = fontAsset;
            Debug.Log($"Font '{fontName}' applied successfully");
        }
        else
        {
            Debug.LogWarning($"Could not load font '{fontName}'");
        }
    }
}