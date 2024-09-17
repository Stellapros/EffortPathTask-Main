using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;
using TMPro;

public class CircularProgressBar : MonoBehaviour
{
    public Image loadingBar;
    public TextMeshProUGUI countdownText;
    public float duration = 5f;
    public string nextSceneName = "DecisionPhase";

    private void Start()
    {
        StartCoroutine(FillLoadingBar());
    }

    private IEnumerator FillLoadingBar()
    {
        Debug.Log("Starting countdown");
        float elapsedTime = 0f;
        loadingBar.fillAmount = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            loadingBar.fillAmount = elapsedTime / duration;

            int remainingSeconds = Mathf.CeilToInt(duration - elapsedTime);
            countdownText.text = remainingSeconds.ToString();
            Debug.Log($"Countdown: {remainingSeconds}");

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