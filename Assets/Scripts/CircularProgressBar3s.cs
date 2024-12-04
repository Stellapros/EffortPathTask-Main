using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;
using TMPro;

public class CircularProgressBar3s : MonoBehaviour
{
    public Image loadingBar;
    public TextMeshProUGUI countdownText;
    
    private const float DURATION = 3f;
    public string nextSceneName = "DecisionPhase";

    private void Start()
    {
        // Ensure the progress bar starts at 0
        loadingBar.fillAmount = 0f;
        StartCoroutine(FillLoadingBar());
    }

    private IEnumerator FillLoadingBar()
    {
        Debug.Log("Starting 3-second countdown");
        float elapsedTime = 0f;

        while (elapsedTime < DURATION)
        {
            elapsedTime += Time.deltaTime;
            loadingBar.fillAmount = elapsedTime / DURATION;

            int remainingSeconds = Mathf.CeilToInt(DURATION - elapsedTime);
            countdownText.text = Mathf.Max(0, remainingSeconds).ToString();

            yield return null;
        }

        Debug.Log("Countdown finished");
        loadingBar.fillAmount = 1f;
        countdownText.text = "0";

        yield return new WaitForSeconds(0.5f);

        Debug.Log("Loading next scene");
        SceneManager.LoadScene(nextSceneName);
    }
}