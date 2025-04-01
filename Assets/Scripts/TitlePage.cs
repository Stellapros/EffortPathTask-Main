using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class TitlePage : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private TextMeshProUGUI instructionText;
    // [SerializeField] private Button startButton;
    // [SerializeField] private Button fullscreenButton;
    [SerializeField] private string nextSceneName = "BeforeStartingScreen";
    private bool _hasStarted = false; // Prevents duplicate triggers


    private void Update()
    {
        if (!_hasStarted && (Input.anyKeyDown || Input.GetMouseButtonDown(0)))
        {
            _hasStarted = true;
            StartCoroutine(EnterFullscreenAndStart());
        }
    }


    private void Start()
    {
        // WebGL-specific setup
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLInput.captureAllKeyboardInput = true;
#endif

        // Set the initial resolution (optional)
        Screen.SetResolution(1980, 1080, false);

        // Handle WebGL fullscreen properly
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            // WebGL specific fullscreen handling
            if (Screen.fullScreen)
            {
                ToggleFullscreen();
            }
        }

        // Set up text
        titleText.text = "Neuronauts";
        subtitleText.text = "The Motivation Expedition";
        instructionText.text = "Maximize the screen for an immersive experience.\n\n Press any key or click to begin your journey.";

        // // Set up buttons
        // startButton.onClick.AddListener(StartJourney);
        // fullscreenButton.onClick.AddListener(OnFullscreenButtonClick);
        // fullscreenButton.GetComponentInChildren<TextMeshProUGUI>().text = "Go Fullscreen";

        StartCoroutine(PulseInstructionText());
        // StartCoroutine(BounceButton(startButton.gameObject));
    }

    private void ToggleFullscreen()
    {
        Screen.fullScreen = !Screen.fullScreen;
    }

    private IEnumerator EnterFullscreenAndStart()
    {
        // Enter fullscreen first
        if (!Screen.fullScreen)
        {
            Screen.fullScreen = true;
            yield return new WaitForSeconds(0.1f); // Small delay for fullscreen to engage
        }

        // Then start the game
        yield return StartCoroutine(FadeText(1f, 0f, 0.3f)); // Fade to black
        SceneManager.LoadScene(nextSceneName);
    }

    private IEnumerator PulseInstructionText()
    {
        while (true)
        {
            // Fade out
            yield return StartCoroutine(FadeText(1f, 0.3f, 1f));
            // Fade in
            yield return StartCoroutine(FadeText(0.3f, 1f, 1f));
        }
    }

    private IEnumerator FadeText(float startAlpha, float endAlpha, float duration)
    {
        float elapsedTime = 0f;
        Color startColor = instructionText.color;
        Color endColor = startColor;
        startColor.a = startAlpha;
        endColor.a = endAlpha;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            instructionText.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }
    }
}