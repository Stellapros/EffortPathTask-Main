using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;
using TMPro;

public class CircularProgressBar1s : MonoBehaviour
{
    public Image loadingBar;
    public TextMeshProUGUI countdownText;
    
    private const float DURATION = 1f;

    private void Start()
    {
        // Ensure the progress bar starts at 0
        loadingBar.fillAmount = 0f;
        StartCoroutine(FillLoadingBarQuickly());
    }

    private IEnumerator FillLoadingBarQuickly()
    {
        float progress = 0f;
        
        // Use a faster interpolation method
        while (progress < 1f)
        {
            // Rapidly increase progress - use a more aggressive approach
            progress += Time.deltaTime * 5f; // Multiplying by 5 makes it much faster
            
            // Clamp to ensure we don't overshoot
            loadingBar.fillAmount = Mathf.Clamp01(progress);
            
            // Update countdown (will rapidly go from 1 to 0)
            countdownText.text = Mathf.CeilToInt(DURATION * (1f - progress)).ToString();

            yield return null;
        }

        // Ensure final state
        loadingBar.fillAmount = 1f;
        countdownText.text = "0";
    }
}