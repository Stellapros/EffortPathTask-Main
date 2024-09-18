using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ContinueScreen : MonoBehaviour
{
    [SerializeField] private Button continueButton;
    [SerializeField] private string nextSceneName = "StartScreen";

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
    }

    private void ContinueToNextScreen()
    {
        Debug.Log("Continuing to the next screen: " + nextSceneName);
        SceneManager.LoadScene(nextSceneName);
        ExperimentManager.Instance.StartExperiment();
    }

    // public void ContinueToNextScreen()
    // {
    //     ExperimentManager experimentManager = ExperimentManager.Instance;
    //     if (experimentManager != null)
    //     {
    //         experimentManager.StartExperiment();
    //     }
    //     else
    //     {
    //         Debug.LogError("ExperimentManager not found in ContinueToNextScreen!");
    //     }
    // }
}