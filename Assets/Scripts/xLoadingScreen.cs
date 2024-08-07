using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoadingScreen : MonoBehaviour
{
    [SerializeField] private Image progressBar;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private GameObject pressAnyKeyPrompt;

    private void Start()
    {
        pressAnyKeyPrompt.SetActive(false);
    }

    public void UpdateProgressBar(float progress)
    {
        progressBar.fillAmount = progress;
        progressText.text = $"Loading... {Mathf.Round(progress * 100)}%";
    }

    public void ShowPressAnyKeyPrompt()
    {
        pressAnyKeyPrompt.SetActive(true);
    }
}