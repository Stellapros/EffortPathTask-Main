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
    private AudioSource audioSource;
    
    private void Start()
    {
        SetupGenderDropdown();
        submitButton.onClick.AddListener(SaveParticipantInfo);
    }

    private void SetupGenderDropdown()
    {
        genderDropdown.ClearOptions();
        List<string> options = new List<string> { "Male", "Female", "Other", "Prefer not to say" };
        genderDropdown.AddOptions(options);
    }

    private void SaveParticipantInfo()
    {
        string id = idInput.text;
        int age;
        if (!int.TryParse(ageInput.text, out age))
        {
            Debug.LogError("Invalid age input");
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