using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class ParticipantInfo : MonoBehaviour
{
    /// <summary>
    /// This class handles the participant information form, including ID,
    /// </summary>
    
    [SerializeField] private TMP_InputField idInput;
    [SerializeField] private TMP_InputField ageInput;
    [SerializeField] private TMP_Dropdown genderDropdown;
    [SerializeField] private Button submitButton;
    [SerializeField] public AudioClip buttonClickSound;
    [SerializeField] private TextMeshProUGUI instructionsText;
    private AudioSource audioSource;
    private ButtonNavigationController navigationController;

    private void Awake()
    {
        // Show cursor for this form scene
        ShowCursor();
    }

    private void OnDestroy()
    {
        // Hide cursor when leaving this scene
        HideCursor();
    }

    private void Start()
    {
        SetupGenderDropdown();
        SetupInstructions();
        submitButton.onClick.AddListener(SaveParticipantInfo);

        navigationController = gameObject.AddComponent<ButtonNavigationController>();

        // Add navigation elements
        navigationController.AddElement(idInput);
        navigationController.AddElement(ageInput);
        navigationController.AddElement(genderDropdown);
        navigationController.AddElement(submitButton);

        // Make sure input fields are interactable
        idInput.interactable = true;
        ageInput.interactable = true;
    }

    private void ShowCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void HideCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void SetupInstructions()
    {
        if (instructionsText != null)
        {
            instructionsText.text = "Click or use arrow keys to navigate.\nClick or press Space/Enter to select.";
        }
    }

    private void SetupGenderDropdown()
    {
        genderDropdown.ClearOptions();
        List<string> options = new List<string> { "Prefer not to say", "Male", "Female", "Other" };
        genderDropdown.AddOptions(options);
        genderDropdown.interactable = true;
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

        // Get the selected gender option - allow any option including "Prefer not to say"
        string gender = genderDropdown.options[genderDropdown.value].text;

        PlayerPrefs.SetString("ParticipantID", id);
        PlayerPrefs.SetInt("ParticipantAge", age);
        PlayerPrefs.SetString("ParticipantGender", gender);
        PlayerPrefs.Save();

        Debug.Log($"Participant info saved: ID={id}, Age={age}, Gender={gender}");
        // Add this to ensure PlayerPrefs are saved
        PlayerPrefs.Save();
        System.Threading.Thread.Sleep(100); // Small delay to ensure save completes

        // Set the info directly in LogManager if it exists
        LogManager logManager = FindAnyObjectByType<LogManager>();
        if (logManager != null)
        {
            logManager.SetParticipantInfoDirectly(id, age, gender);
        }
        else
        {
            PlayerPrefs.SetString("ParticipantID", id);
            PlayerPrefs.SetInt("ParticipantAge", age);
            PlayerPrefs.SetString("ParticipantGender", gender);
            PlayerPrefs.Save();
        }

        // Hide cursor before loading next scene
        HideCursor();

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