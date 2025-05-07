using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;
using TMPro;

public class CircularProgressBar5sBeforeCheck : MonoBehaviour
{
    public Image loadingBar;
    public TextMeshProUGUI countdownText;

    private const float DURATION = 5f;
    public string nextSceneName = "Check1_Preference";
    
    // Add this to ensure no other DontDestroyOnLoad objects interfere
    private bool isCountdownActive = false;

    private void Awake()
    {
        Debug.Log("CircularProgressBar5s Awake() called");
        // Reset any persisting DontDestroyOnLoad objects that might interfere
        var practiceManager = FindAnyObjectByType<PracticeManager>();
        if (practiceManager != null)
        {
            Debug.Log("Found PracticeManager - temporarily pausing its updates");
            // Tell PracticeManager to pause updates during countdown (see below for this method)
            practiceManager.PauseUpdates(true);
        }
    }

    private void Start()
    {
        Debug.Log("CircularProgressBar5s Start() called");
        
        // Ensure the progress bar starts at 0
        if (loadingBar != null)
        {
            loadingBar.fillAmount = 0f;
            Debug.Log("Loading bar reset to 0");
        }
        else
        {
            Debug.LogError("Loading bar reference is null!");
        }
        
        if (countdownText != null)
        {
            countdownText.text = DURATION.ToString();
            Debug.Log("Countdown text initialized");
        }
        else
        {
            Debug.LogError("Countdown text reference is null!");
        }
        
        // Start the countdown with a slight delay to ensure everything is ready
        Invoke("BeginCountdown", 0.1f);
    }
    
    private void BeginCountdown()
    {
        if (!isCountdownActive)
        {
            isCountdownActive = true;
            StartCoroutine(FillLoadingBar());
        }
    }

private IEnumerator FillLoadingBar()
{
    Debug.Log("Starting 5-second countdown");
    float startTime = Time.realtimeSinceStartup;
    float elapsedTime = 0f;

    while (elapsedTime < DURATION)
    {
        // Calculate elapsed time using realtime clock
        elapsedTime = Time.realtimeSinceStartup - startTime;
        
        // Update UI
        if (loadingBar != null)
        {
            loadingBar.fillAmount = elapsedTime / DURATION;
        }

        int remainingSeconds = Mathf.CeilToInt(DURATION - elapsedTime);
        remainingSeconds = Mathf.Max(0, remainingSeconds);

        if (countdownText != null)
        {
            countdownText.text = remainingSeconds.ToString();
        }

        yield return null;
    }

    Debug.Log("Countdown finished");
    
    if (loadingBar != null)
    {
        loadingBar.fillAmount = 1f;
    }
    
    if (countdownText != null)
    {
        countdownText.text = "0";
    }

    // Short delay before scene transition
    yield return new WaitForSeconds(0.5f);

    // Make sure we have the correct next scene name based on the current state
    string sceneToLoad = nextSceneName;
    
    // Debug current state
    Debug.Log($"Before scene load - ResumeFromCheck: {PlayerPrefs.GetInt("ResumeFromCheck", 0)}");
    Debug.Log($"Before scene load - PracticeBlocksCompleted: {PlayerPrefs.GetInt("PracticeBlocksCompleted", 0)}");
    
    // Re-enable PracticeManager updates before scene change
    var practiceManager = FindAnyObjectByType<PracticeManager>();
    if (practiceManager != null)
    {
        Debug.Log("Re-enabling PracticeManager updates");
        practiceManager.PauseUpdates(false);
    }

    Debug.Log($"Loading next scene: {sceneToLoad}");
    SceneManager.LoadScene(sceneToLoad);
}

    private void OnDestroy()
    {
        // Ensure PracticeManager is unpaused if this object is destroyed
        var practiceManager = FindAnyObjectByType<PracticeManager>();
        if (practiceManager != null)
        {
            practiceManager.PauseUpdates(false);
        }
    }
}