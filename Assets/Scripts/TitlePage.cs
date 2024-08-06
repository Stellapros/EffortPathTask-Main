using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class TitlePage : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private Button startButton;
    [SerializeField] private Image[] brainImages;

    private void Start()
    {
        // Set up the title
        titleText.text = "Neuronauts";
        StartCoroutine(PulseText(titleText));

        // Set up the subtitle
        subtitleText.text = "The Motivation Expedition";
        subtitleText.gameObject.SetActive(false);
        StartCoroutine(FadeInSubtitle());

        // Set up the start button
        startButton.GetComponentInChildren<TextMeshProUGUI>().text = "Start Journey";
        startButton.onClick.AddListener(StartJourney);
        StartCoroutine(BounceButton(startButton.gameObject));

        // Set up brain icons
        foreach (var brainImage in brainImages)
        {
            //StartCoroutine(RotateBrain(brainImage.gameObject));
        }
    }

    private IEnumerator PulseText(TextMeshProUGUI text)
    {
        while (true)
        {
            yield return StartCoroutine(ScaleText(text, 1f, 1.1f, 0.5f));
            yield return StartCoroutine(ScaleText(text, 1.1f, 1f, 0.5f));
        }
    }

    private IEnumerator ScaleText(TextMeshProUGUI text, float startScale, float endScale, float duration)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            text.transform.localScale = Vector3.Lerp(Vector3.one * startScale, Vector3.one * endScale, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        text.transform.localScale = Vector3.one * endScale;
    }

    private IEnumerator FadeInSubtitle()
    {
        yield return new WaitForSeconds(0.5f);
        subtitleText.gameObject.SetActive(true);
        subtitleText.alpha = 0f;
        float duration = 1f;
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            subtitleText.alpha = Mathf.Lerp(0f, 1f, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        subtitleText.alpha = 1f;
    }

    private IEnumerator BounceButton(GameObject button)
    {
        while (true)
        {
            yield return StartCoroutine(MoveButton(button, button.transform.position, button.transform.position + Vector3.up * 10f, 0.5f));
            yield return StartCoroutine(MoveButton(button, button.transform.position, button.transform.position - Vector3.up * 10f, 0.5f));
        }
    }

    private IEnumerator MoveButton(GameObject button, Vector3 startPos, Vector3 endPos, float duration)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            button.transform.position = Vector3.Lerp(startPos, endPos, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        button.transform.position = endPos;
    }

    // private IEnumerator RotateBrain(GameObject brain)
    // {
    //     while (true)
    //     {
    //         brain.transform.Rotate(Vector3.forward, 120f * Time.deltaTime);
    //         yield return null;
    //     }
    // }

    private void StartJourney()
    {
        Debug.Log("Journey Started!");
        // Add your game start logic here
    }
}