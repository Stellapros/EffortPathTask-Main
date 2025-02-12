using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class BlockPreInstructions : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private TextMeshProUGUI continueText;
    [SerializeField] private float minimumDisplayTime = 5f;  // Added to match BlockInstructions

    private float startTime;
    private bool canContinue = false;
    private const string SPACE_INSTRUCTION = "\n\n<size=90%>Press 'Space' to continue</size>";
    private const string NEXT_SCENE = "Block_Instructions";

    private void Start()
    {
        // Clean up all practice-related PlayerPrefs before starting formal trials
        CleanupPracticePrefs();

        startTime = Time.time;
        InitializeUI();
        DisplayInstructions();
    }

    private void CleanupPracticePrefs()
    {
        // Clear all practice-related PlayerPrefs
        PlayerPrefs.DeleteKey("IsPracticeTrial");
        PlayerPrefs.DeleteKey("CurrentPracticeEffortLevel");
        PlayerPrefs.DeleteKey("CurrentPracticeTrial");
        PlayerPrefs.DeleteKey("PracticeTrialCount");
        PlayerPrefs.DeleteKey("LastPracticeScore");
        PlayerPrefs.DeleteKey("PracticeModeActive");

        // Set a flag to indicate we're in formal trials
        PlayerPrefs.SetInt("IsFormalTrial", 1);

        // Save the changes
        PlayerPrefs.Save();

        Debug.Log("Cleaned up practice-related PlayerPrefs and set formal trial mode");
    }


    private void Update()
    {
        if (!canContinue && Time.time - startTime >= minimumDisplayTime)
        {
            canContinue = true;
            ShowContinueInstruction();
        }

        if (canContinue && Input.GetKeyDown(KeyCode.Space))
        {
            OnContinueButtonClicked();
        }
    }

    private void InitializeUI()
    {
        if (instructionText == null || continueText == null)
        {
            Debug.LogError("Missing UI text components for pre-block instructions!");
        }

        // Initialize continue text
        if (continueText != null)
        {
            continueText.text = "";
            continueText.alignment = TextAlignmentOptions.Center;
        }
    }

    private void DisplayInstructions()
    {
        if (instructionText != null)
        {
            instructionText.text = "Practice complete! Now, the real challenge begins.\n\n" +
                       "Collect as many fruits as possible within the available time. Choose wisely—each island has a unique fruit distribution. Some fruits are rarer than others, and the more you collect, the higher your score!";

            // instructionText.text = "Practice complete! Now, it's time for the real challenge.\n\n" +
            //              "Your goal is to collect as many fruits as possible. Choose when to work for fruit " +
            //              "within the available time, but plan wisely—each island has a different fruit distribution. " +
            //              "Some fruits are rarer than others, and the more you collect, the higher your score.";
        }
    }

    private void ShowContinueInstruction()
    {
        if (continueText != null)
        {
            continueText.text = SPACE_INSTRUCTION;
        }
    }

    private void OnContinueButtonClicked()
    {
        if (canContinue)
        {
            // Double-check that we're in formal trial mode before proceeding
            if (PlayerPrefs.GetInt("IsFormalTrial", 0) != 1)
            {
                CleanupPracticePrefs();
            }
            SceneManager.LoadScene(NEXT_SCENE);
        }
    }
}