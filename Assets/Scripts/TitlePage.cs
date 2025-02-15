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
    [SerializeField] private Button startButton;
    [SerializeField] private string nextSceneName = "BeforeStartingScreen";

    private void Start()
    {
        // Add in Start() method
        ButtonNavigationController navigationController = gameObject.AddComponent<ButtonNavigationController>();
        navigationController.AddElement(startButton);

        // Set up the title
        titleText.text = "Neuronauts";

        // Set up the subtitle
        subtitleText.text = "The Motivation Expedition";

        // Set up the instruction text
        instructionText.text = "Press any key to begin your journey";
        StartCoroutine(PulseInstructionText());

        // // Set up the start button
        // startButton.GetComponentInChildren<TextMeshProUGUI>().text = "Start Journey";
        // startButton.onClick.AddListener(StartJourney);
        // StartCoroutine(BounceButton(startButton.gameObject));
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

    private IEnumerator BounceButton(GameObject button)
    {
        Vector3 startPosition = button.transform.position;
        while (true)
        {
            yield return StartCoroutine(MoveButton(button, startPosition, startPosition + Vector3.up * 10f, 0.5f));
            yield return StartCoroutine(MoveButton(button, startPosition + Vector3.up * 10f, startPosition, 0.5f));
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

    private void Update()
    {
        // Check for any key press
        if (Input.anyKeyDown)
        {
            StartJourney();
        }
    }

    private void StartJourney()
    {
        Debug.Log("Starting Journey! Loading next scene...");
        SceneManager.LoadScene(nextSceneName);
    }
}