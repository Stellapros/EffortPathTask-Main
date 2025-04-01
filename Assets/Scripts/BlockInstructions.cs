using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BlockInstructions : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI headingText;
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private TextMeshProUGUI continueText;
    [SerializeField] private Button continueButton;
    [SerializeField] private float minimumDisplayTime = 5f;

    [Header("Block Duration Display")]
    [SerializeField] private bool showBlockDuration = true;
    [SerializeField] private string durationFormat = "\n\nYour fruit-hunting expedition on this island will last {0} minutes! Make every moment count!";

    private float startTime;
    private bool canContinue = false;
    private const string SPACE_INSTRUCTION = "\n\n<size=90%>Press 'Space' to continue</size>";


    private void Start()
    {
        Debug.Log($"BlockInstructions: Starting instructions for block {ExperimentManager.Instance.GetCurrentBlockNumber()}");
        Debug.Log($"Current block type is: {ExperimentManager.Instance.GetCurrentBlockType()}");

        if (ExperimentManager.Instance == null)
        {
            Debug.LogError("[BlockInstructions] ExperimentManager instance is null!");
            return;
        }

        int currentBlock = ExperimentManager.Instance.GetCurrentBlockNumber();
        ExperimentManager.BlockType blockType = ExperimentManager.Instance.GetCurrentBlockType();

        Debug.Log($"[BlockInstructions] Displaying instructions for Block {currentBlock} of type {blockType}");


        startTime = Time.time;
        CleanupPracticeData();
        InitializeUI();
        DisplayCurrentBlockInstructions();
    }

    private void CleanupPracticeData()
    {
        // Clear all practice-related PlayerPrefs
        PlayerPrefs.DeleteKey("PracticeAttempts");
        PlayerPrefs.DeleteKey("NeedsPracticeRetry");
        PlayerPrefs.DeleteKey("InPracticeMode");

        // Set formal trial flag
        PlayerPrefs.SetInt("IsFormalTrial", 1);

        // Clear any temporary practice scores
        PlayerPrefs.DeleteKey("PracticeScore");
        PlayerPrefs.DeleteKey("CurrentPracticeBlock");

        // Save changes
        PlayerPrefs.Save();

        Debug.Log("Cleaned up practice data and marked as formal trial");
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
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueButtonClicked);
            continueButton.gameObject.SetActive(false);
        }

        if (headingText == null || instructionText == null || continueText == null)
        {
            Debug.LogError("Missing UI text components for block instructions!");
        }

        if (continueText != null)
        {
            continueText.text = "";
            continueText.alignment = TextAlignmentOptions.Center;
        }
    }

    private void ShowContinueInstruction()
    {
        if (continueText != null)
        {
            continueText.text = SPACE_INSTRUCTION;
        }
    }

    private void DisplayCurrentBlockInstructions()
    {
        if (ExperimentManager.Instance == null || instructionText == null || headingText == null)
        {
            Debug.LogError("Missing required components for block instructions!");
            return;
        }

        ExperimentManager.BlockType currentBlockType = ExperimentManager.Instance.GetCurrentBlockType();
        Debug.Log($"Displaying instructions for current block - GetCurrentBlockType: {currentBlockType}");

        (string heading, string instructions) = GetInstructionsForBlockType(currentBlockType);
        headingText.text = heading;
        instructionText.text = instructions;

        if (continueText != null)
        {
            continueText.text = "";
        }

        Debug.Log($"Displaying instructions for block type: {currentBlockType}");
    }

    private (string heading, string instructions) GetInstructionsForBlockType(ExperimentManager.BlockType blockType)
    {
        string heading, instructions;

        switch (blockType)
        {
            case ExperimentManager.BlockType.HighLowRatio:
                heading = "Green Island";
                instructions = "Welcome to Green Island! Get ready for a new challenge!\n\n" +
                               "Use your RIGHT hand (← → ↑ ↓) to move the character and your LEFT hand (A / D) to make decisions.";
                break;

            case ExperimentManager.BlockType.LowHighRatio:
                heading = "Blue Island";
                instructions = "Welcome to Blue Island! A different adventure awaits!\n\n" +
                               "Use your RIGHT hand (← → ↑ ↓) to move the character and your LEFT hand (A / D) to make decisions.";
                break;

            // case ExperimentManager.BlockType.HighLowRatio: // 3:2:1 ratio
            //     heading = "Green Island";
            //     instructions = "Welcome to Green Island!\n\n" +
            //                    "Get ready for a unique challenge ahead. Stay focused and dive in!\n\n" +
            //                    "Controls: Use your RIGHT hand (↑ ↓ ← →) to move the character and your LEFT hand (A / D) to make decisions.";
            //     break;

            // case ExperimentManager.BlockType.LowHighRatio: // 1:2:3 ratio
            //     heading = "Blue Island";
            //     instructions = "Welcome to Blue Island!\n\n" +
            //                    "This adventure will test you in new ways. Stay sharp and take on the challenge!\n\n" +
            //                    "Controls: Use your RIGHT hand (↑ ↓ ← →) to move the character and your LEFT hand (A / D) to make decisions.";
            //     break;

            // case ExperimentManager.BlockType.HighLowRatio: // 3:2:1 ratio
            //     heading = "Green island";
            //     instructions = "Welcome to this island!\n\n" +
            //                  "Get ready!";
            //     break;

            // case ExperimentManager.BlockType.LowHighRatio: // 1:2:3 ratio
            //     heading = "Blue island";
            //     instructions = "Welcome to this island!\n\n" +
            //                  "Get ready!";
            //     break;

            default:
                Debug.LogError($"Unknown block type: {blockType}");
                return ("Error", "Unknown block type");
        }

        if (showBlockDuration)
        {
            instructions += string.Format(durationFormat, ExperimentManager.Instance.GetBlockDuration() / 60f);
        }

        return (heading, instructions);
    }

    private void OnContinueButtonClicked()
    {
        if (canContinue && ExperimentManager.Instance != null)
        {
            Debug.Log("Continuing after instructions...");
            ExperimentManager.Instance.ContinueAfterInstructions();
        }
        else
        {
            Debug.LogError("Cannot continue: ExperimentManager.Instance is null or not ready!");
        }
    }
}