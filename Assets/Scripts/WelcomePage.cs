using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class ContinueScreen : MonoBehaviour
{
    [SerializeField] private Button continueButton;
    [SerializeField] private string nextSceneName = "StartScreen";
    [SerializeField] public AudioClip buttonClickSound;
    [SerializeField] private TextMeshProUGUI instructionText;
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

        // Add space bar instruction to the instruction text
        if (instructionText != null)
        {
            instructionText.text = "Press 'Space' to continue";
        }

        ButtonNavigationController navigationController = gameObject.AddComponent<ButtonNavigationController>();
        navigationController.AddElement(continueButton);
    }

    private void Update()
    {
        // Check for space key press to continue
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ContinueToNextScreen();
        }
    }

    private void ContinueToNextScreen()
    {
        Debug.Log("Continuing to the next screen: " + nextSceneName);
        SceneManager.LoadScene(nextSceneName);
        // ExperimentManager.Instance.StartExperiment();
    }
}