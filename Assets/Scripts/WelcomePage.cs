using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ContinueScreen : MonoBehaviour
{
    [SerializeField] private Button continueButton;
    [SerializeField] private string nextSceneName = "StartScreen";
    [SerializeField] public AudioClip buttonClickSound;
    private AudioSource audioSource;
    private void Start()
    {
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(ContinueToNextScreen);
        }
        else
        {
            Debug.LogError("Continue button not assigned in WelcomePage!");
        }

        ButtonNavigationController navigationController = gameObject.AddComponent<ButtonNavigationController>();
        navigationController.AddElement(continueButton);
    }

    private void ContinueToNextScreen()
    {
        Debug.Log("Continuing to the next screen: " + nextSceneName);
        SceneManager.LoadScene(nextSceneName);
        // ExperimentManager.Instance.StartExperiment();
    }

}