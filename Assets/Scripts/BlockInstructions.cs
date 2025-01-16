using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BlockInstructions : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private Button continueButton;
    [SerializeField] private float minimumDisplayTime = 5f;

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
        if (ExperimentManager.Instance == null || instructionText == null)
        {
            Debug.LogError("Missing required components for block instructions!");
            return;
        }

        int currentBlock = ExperimentManager.Instance.GetCurrentBlockNumber();
        string instructions = GetInstructionsForBlock(currentBlock);
        instructionText.text = instructions;
    }

    private string GetInstructionsForBlock(int blockNumber)
    {
        if (blockNumber == 0)
        {
            return "Welcome to the first island!\n\n" +
            "A sun-kissed island packed with orange trees. You'll see some banana plants along the way and maybe a rare cherry bush or two.";
        }
        else
        {
            return "Welcome to the second island!\n\n" +
            "Sweet cherries fill every corner of this island! Banana plants dot the landscape, while orange trees make rare appearances.";
        }
    }

    private void OnContinueButtonClicked()
{
    if (canContinue && ExperimentManager.Instance != null)
    {
        ExperimentManager.Instance.ContinueAfterInstructions();
    }
}
}