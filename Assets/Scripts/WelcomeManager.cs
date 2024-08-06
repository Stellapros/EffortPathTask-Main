using UnityEngine;
using UnityEngine.UI;

public class WelcomeManager : MonoBehaviour
{
    public Text welcomeText;
    public Text instructionsText;
    public Toggle consentToggle;
    public InputField idInput;
    public InputField ageInput;
    public Dropdown genderDropdown;
    public Button startButton;

    private void Start()
    {
        welcomeText.text = "Welcome to the experiment";
        instructionsText.text = "Instructions for the experiment...";
        startButton.onClick.AddListener(StartExperiment);
    }

    private void StartExperiment()
    {
        if (consentToggle.isOn && !string.IsNullOrEmpty(idInput.text) && !string.IsNullOrEmpty(ageInput.text))
        {
            PlayerPrefs.SetString("ID", idInput.text);
            PlayerPrefs.SetInt("Age", int.Parse(ageInput.text));
            PlayerPrefs.SetInt("Gender", genderDropdown.value);
            GameManager.Instance.LoadNextScene();
        }
        else
        {
            Debug.Log("Please fill all fields and give consent.");
        }
    }
}