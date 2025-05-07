using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class PracticeBlockInstruction : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI headingText;
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private TextMeshProUGUI continueText;

    [Header("Settings")]
    [SerializeField] private float minimumDisplayTime = 3f;
    [SerializeField] private string decisionPhaseScene = "PracticeDecisionPhase";

    private const string SPACE_INSTRUCTION = "\n\n<size=90%>Press 'Space' to continue</size>";
    private float startTime;
    private bool canContinue = false;

    private void Start()
    {
        // Record start time
        startTime = Time.time;

        // Get block type from PlayerPrefs
        string blockType = PlayerPrefs.GetString("CurrentPracticeBlockType", "EqualRatio");
        Debug.Log($"PracticeBlockInstruction: Loaded block type from PlayerPrefs: {blockType}");

        // Set instruction texts based on block type
        DisplayBlockInstructions(blockType);

        // Initialize continue text to empty
        if (continueText != null)
        {
            continueText.text = "";
        }
    }

    private void Update()
    {
        // Check if minimum display time has elapsed
        if (!canContinue && Time.time - startTime >= minimumDisplayTime)
        {
            canContinue = true;
            ShowContinueInstruction();
        }

        // Allow user to skip by pressing space if minimum time has passed
        if (canContinue && Input.GetKeyDown(KeyCode.Space))
        {
            ContinueToNextScene();
        }
    }

    private void ShowContinueInstruction()
    {
        if (continueText != null)
        {
            continueText.text = SPACE_INSTRUCTION;
        }
    }

    private void DisplayBlockInstructions(string blockType)
    {
        Debug.Log($"Displaying instructions for block type: {blockType}");
        (string heading, string instructions) = GetBlockInstructionText(blockType);

        if (headingText != null)
        {
            headingText.text = heading;
            Debug.Log($"Set heading text: {heading}");
        }
        else
        {
            Debug.LogError("headingText is null!");
        }

        if (instructionText != null)
        {
            instructionText.text = instructions;
            Debug.Log($"Set instruction text: {instructions}");
        }
        else
        {
            Debug.LogError("instructionText is null!");
        }
    }

    private (string heading, string instructions) GetBlockInstructionText(string blockType)
    {
        switch (blockType)
        {
            case "EqualRatio":
                return ("Yellow Island",
                        "Welcome to Yellow Island!\n\nHere, you'll try out all the fruit types you'll see later.\n\nUse your RIGHT hand (← → ↑ ↓) to move the character and your LEFT hand (A / D) to make decisions.");
            case "HighLowRatio":
                return ("Green Island",
                        "Welcome to Green Island!\n\nFruit is distributed in a special way here—keep your eyes open!\n\nUse your RIGHT hand (← ↑ ↓ →) to move your character, and your LEFT hand (A / D) to make decisions.");
            case "LowHighRatio":
                return ("Blue Island",
                        "Welcome to Blue Island!\n\nThis island has its own fruit distribution pattern. Pay close attention!\n\nUse your RIGHT hand (← ↑ ↓ →) to move your character, and your LEFT hand (A / D) to make decisions.");
            default:
                Debug.LogWarning($"Unknown block type: {blockType}, defaulting to EqualRatio");
                return ("Practice Island",
                        "Starting the Practice Island\n\nRemember to press A to work, D to skip.");
        }
    }

    private void ContinueToNextScene()
    {
        Debug.Log($"Continuing to next scene: {decisionPhaseScene}");
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        HideCursorConsistently();

        // OFFICIALLY start block timing here
        float blockStartTime = Time.realtimeSinceStartup;

        // Make sure we clear any existing block time expired flags
        PlayerPrefs.SetInt("BlockTimeExpired", 0);

        // Set block started flags and time
        PlayerPrefs.SetInt("BlockOfficiallyStarted", 1);
        PlayerPrefs.SetFloat("BlockStartTime", blockStartTime);

        // Force save before scene transition
        PlayerPrefs.Save();

        Debug.Log($"PracticeBlockInstruction: Block officially started at {blockStartTime}");

        // Double check that values were saved correctly
        bool blockStarted = PlayerPrefs.GetInt("BlockOfficiallyStarted", 0) == 1;
        float storedTime = PlayerPrefs.GetFloat("BlockStartTime", 0f);
        Debug.Log($"VERIFICATION: BlockOfficiallyStarted={blockStarted}, BlockStartTime={storedTime}");

        SceneManager.LoadScene(decisionPhaseScene);
    }


    private void HideCursorConsistently()
    {
        // Hide and lock the cursor
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // Add a delayed check to ensure it stays hidden
        StartCoroutine(EnsureCursorHidden());
    }

    private IEnumerator EnsureCursorHidden()
    {
        // Check a few times over a short period to make sure cursor stays hidden
        for (int i = 0; i < 5; i++)
        {
            yield return new WaitForSeconds(0.1f);
            if (Cursor.visible || Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
    }
}