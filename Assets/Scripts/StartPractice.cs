using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StartPractice : MonoBehaviour
{
    [SerializeField] private Button continueButton;
    [SerializeField] private string nextSceneName = "NextScene";
    [SerializeField] public AudioClip buttonClickSound;
    private AudioSource audioSource;

    private void Start()
    {
        if (continueButton == null)
        {
            Debug.LogError("Continue button is not assigned in the inspector!");
            return;
        }

        continueButton.onClick.AddListener(ContinueToNextScreen);
    }

    private void ContinueToNextScreen()
    {
        Debug.Log("Continuing to the next screen: " + nextSceneName);
        SceneManager.LoadScene(nextSceneName);
    }
}