using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

public class ParticipantInfo : MonoBehaviour
{
    [SerializeField] private TMP_InputField idInput;
    [SerializeField] private TMP_InputField ageInput;
    [SerializeField] private TMP_Dropdown genderDropdown;
    [SerializeField] private Button submitButton;
    [SerializeField] public AudioClip buttonClickSound;
    [SerializeField] private TextMeshProUGUI instructionsText; // New field for instructions
    private AudioSource audioSource;

    private void Start()
    {
        SetupGenderDropdown();
        SetupInstructions();
        submitButton.onClick.AddListener(SaveParticipantInfo);

        ButtonNavigationController navigationController = gameObject.AddComponent<ButtonNavigationController>();
        navigationController.AddElement(idInput);
        navigationController.AddElement(ageInput);
        navigationController.AddElement(genderDropdown);
        navigationController.AddElement(submitButton);
    }

    private void SetupInstructions()
    {
        if (instructionsText != null)
        {
            instructionsText.text = "Use ↑ ↓ to choose from dropdown; Press Space or Enter to select/confirm\n";
        }
        else
        {
            Debug.LogWarning("Instructions text component not assigned!");
        }
    }

    private void SetupGenderDropdown()
    {
        genderDropdown.ClearOptions();
        List<string> options = new List<string> { "Prefer not to say", "Male", "Female", "Other" };
        genderDropdown.AddOptions(options);
    }

    private void SaveParticipantInfo()
    {
        string id = idInput.text;

        // Validate ID input
        if (string.IsNullOrWhiteSpace(id))
        {
            Debug.LogError("ID cannot be empty");
            return;
        }

        // Validate age input
        int age;
        if (!int.TryParse(ageInput.text, out age))
        {
            Debug.LogError("Invalid age input - must be a number");
            return;
        }

        // Check age range
        if (age < 18 || age > 85)
        {
            Debug.LogError($"Age must be between 18 and 85 (got {age})");
            return;
        }

        // Validate gender selection
        if (genderDropdown.value == 0) // First option is "Prefer not to say"
        {
            // Optional: Add a more explicit warning or prevent submission
            Debug.LogWarning("Please select a gender option");
            return;
        }

        string gender = genderDropdown.options[genderDropdown.value].text;

        PlayerPrefs.SetString("ParticipantID", id);
        PlayerPrefs.SetInt("ParticipantAge", age);
        PlayerPrefs.SetString("ParticipantGender", gender);
        PlayerPrefs.Save();

        Debug.Log($"Participant info saved: ID={id}, Age={age}, Gender={gender}");

        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (SceneManager.sceneCountInBuildSettings > nextSceneIndex)
        {
            SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            Debug.LogWarning("No next scene to load. This might be the last scene.");
        }
    }
}