using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BlockInstructions : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI headingText;
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private Button continueButton;
    [SerializeField] private float minimumDisplayTime = 5f;

    [Header("Block Duration Display")]
    [SerializeField] private bool showBlockDuration = true;
    [SerializeField] private string durationFormat = "\n\nYour fruit-hunting expedition on this island will last {0} minutes! Make every moment count!";

    private float startTime;
    private bool canContinue = false;

    private void Start()
    {
        startTime = Time.time;
        InitializeUI();
        DisplayCurrentBlockInstructions();
    }

    private void Update()
    {
        if (!canContinue && Time.time - startTime >= minimumDisplayTime)
        {
            canContinue = true;
            EnableContinueButton();
        }
    }

    private void InitializeUI()
    {
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueButtonClicked);
            continueButton.interactable = false;
        }

        if (headingText == null || instructionText == null)
        {
            Debug.LogError("Missing UI text components for block instructions!");
        }
    }

    private void EnableContinueButton()
    {
        if (continueButton != null)
        {
            continueButton.interactable = true;
            continueButton.GetComponentInChildren<TextMeshProUGUI>().text = "Continue";
        }

        ButtonNavigationController navigationController = gameObject.AddComponent<ButtonNavigationController>();
        navigationController.AddElement(continueButton);
    }

private void DisplayCurrentBlockInstructions()
{
    if (ExperimentManager.Instance == null || instructionText == null || headingText == null)
    {
        Debug.LogError("Missing required components for block instructions!");
        return;
    }

    // ExperimentManager.BlockType currentBlockType = ExperimentManager.Instance.currentBlockType;
    ExperimentManager.BlockType currentBlockType = ExperimentManager.Instance.GetCurrentBlockType();
Debug.Log($"Displaying instructions for current block - GetCurrentBlockType: {currentBlockType}");

    (string heading, string instructions) = GetInstructionsForBlockType(currentBlockType);

    headingText.text = heading;
    instructionText.text = instructions;

    Debug.Log($"Displaying instructions for block type: {currentBlockType}");
}

    private (string heading, string instructions) GetInstructionsForBlockType(ExperimentManager.BlockType blockType)
    {
        string heading, instructions;
        
        switch (blockType)
        {
            case ExperimentManager.BlockType.HighLowRatio: // 3:2:1 ratio
                heading = "Orange Haven";
                instructions = "Welcome to this island!\n" +
                             "A sun-kissed island packed with orange trees. You'll see some banana plants " +
                             "along the way and maybe a rare cherry bush or two.";
                break;

            case ExperimentManager.BlockType.LowHighRatio: // 1:2:3 ratio
                heading = "Cherry Paradise";
                instructions = "Welcome to this island!\n" +
                             "Sweet cherries fill every corner of this island! Banana plants dot the landscape, " +
                             "while orange trees make rare appearances.";
                break;

            default:
                Debug.LogError($"Unknown block type: {blockType}");
                return ("Error", "Unknown block type");
        }

        // Add block duration if enabled
        if (showBlockDuration)
        {
            instructions += string.Format(durationFormat, ExperimentManager.Instance.GetBlockDuration()/ 60f);
        }

        return (heading, instructions);
    }

    private void OnContinueButtonClicked()
    {
        if (canContinue && ExperimentManager.Instance != null)
        {
            ExperimentManager.Instance.ContinueAfterInstructions();
        }
    }
}