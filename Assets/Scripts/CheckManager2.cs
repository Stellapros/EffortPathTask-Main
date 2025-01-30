using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class CheckManager2 : MonoBehaviour
{
    [Header("Fruit Settings")]
    public GameObject applePrefab;
    public GameObject grapesPrefab;
    public GameObject watermelonPrefab;

    [SerializeField] private Image fruitImage;

    [Header("UI Elements")]
    [SerializeField] private Button choice50Button;
    [SerializeField] private Button choice70Button;
    [SerializeField] private Button choice90Button;
    [SerializeField] private TextMeshProUGUI questionText;
    [SerializeField] private TextMeshProUGUI debugText;
    [SerializeField] private TextMeshProUGUI trialIndexText;
    [SerializeField] private TextMeshProUGUI buttonInstructionText;

    private GameObject currentFruit;
    private readonly Dictionary<string, Dictionary<int, int>> choiceRecords = new Dictionary<string, Dictionary<int, int>>();
    private readonly List<GameObject> fruitPrefabs = new List<GameObject>();
    private int trialCount = 0;
    private int totalTrials = 3;
    private int currentTrialIndex = 0;
    private bool isProcessing = false;
    private int correctChoiceScore = 0;

    private int currentSelection = -1; // Start with no selection
    private readonly int[] thresholdOptions = { 50, 70, 90 };
    private Color defaultButtonColor;
    private Color selectedButtonColor = new Color(0.87f, 0.86f, 0.67f);
    [SerializeField] private Color normalColor = new Color(0.67f, 0.87f, 0.86f);
    [SerializeField] private Color disabledColor = new Color(0.7f, 0.7f, 0.7f);

    private void Start()
    {
        InitializeChoiceRecords();
        SetupButtons();
        fruitPrefabs.AddRange(new[] { applePrefab, grapesPrefab, watermelonPrefab });
        defaultButtonColor = choice50Button.GetComponent<Image>().color;
        SpawnRandomFruit();
        UpdateButtonHighlight();
    }

    private void Update()
    {
        if (!isProcessing)
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                currentSelection = (currentSelection + 2) % 3;
                UpdateButtonHighlight();
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                currentSelection = (currentSelection + 1) % 3;
                UpdateButtonHighlight();
            }

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                if (currentSelection >= 0)
                {
                    RecordChoice(thresholdOptions[currentSelection]);
                }
            }
        }
    }

    private void SetupButtons()
    {
        choice50Button.onClick.AddListener(() => RecordChoice(50));
        choice70Button.onClick.AddListener(() => RecordChoice(70));
        choice90Button.onClick.AddListener(() => RecordChoice(90));
    }

    private void DisableAllButtons()
    {
        choice50Button.interactable = false;
        choice70Button.interactable = false;
        choice90Button.interactable = false;

        // Set all buttons to disabled color
        choice50Button.GetComponent<Image>().color = disabledColor;
        choice70Button.GetComponent<Image>().color = disabledColor;
        choice90Button.GetComponent<Image>().color = disabledColor;
    }

    private void EnableAllButtons()
    {
        choice50Button.interactable = true;
        choice70Button.interactable = true;
        choice90Button.interactable = true;

        // Reset all buttons to normal color
        choice50Button.GetComponent<Image>().color = normalColor;
        choice70Button.GetComponent<Image>().color = normalColor;
        choice90Button.GetComponent<Image>().color = normalColor;
    }

    private void SpawnRandomFruit()
    {
        if (currentFruit != null)
        {
            Destroy(currentFruit.gameObject);
        }

        int randomIndex = Random.Range(0, fruitPrefabs.Count);
        GameObject selectedFruit = fruitPrefabs[randomIndex];
        currentFruit = Instantiate(selectedFruit, Vector3.zero, Quaternion.identity);

        if (fruitImage != null)
        {
            fruitImage.sprite = currentFruit.GetComponent<SpriteRenderer>().sprite;
            fruitImage.rectTransform.sizeDelta = new Vector2(100, 100);
        }

        string fruitName = selectedFruit.name.Replace("(Clone)", "").Replace("Reward_", "");
        questionText.text = $"How often did you see this {fruitName}?";
        buttonInstructionText.text = "Use ← → to choose; Press Space or Enter to confirm";
        
        fruitPrefabs.RemoveAt(randomIndex);
        trialCount++;

        if (LogManager.Instance != null)
        {
            LogManager.Instance.LogTrialStart(currentTrialIndex + 1, 1, 0, 0, false);
        }

        if (trialIndexText != null)
        {
            trialIndexText.text = $"Question: {currentTrialIndex + 1} of {totalTrials}";
        }

        debugText.text = $"Trial {currentTrialIndex + 1} of {totalTrials}: {fruitName}";

        // Reset selection and enable buttons
        currentSelection = -1;
        EnableAllButtons();
        UpdateButtonHighlight();
    }

    private void RecordChoice(int threshold)
    {
        if (currentFruit == null || isProcessing) return;
        isProcessing = true;

        // Disable all buttons and update their visual state
        DisableAllButtons();

        // Disable navigation controller
        var navigationController = GetComponent<ButtonNavigationController>();
        if (navigationController != null)
        {
            navigationController.isProcessing = true;
            navigationController.DisableAllButtons();
        }

        string currentFruitName = currentFruit.name.Replace("(Clone)", "").Replace("Reward_", "");

        if (choiceRecords.ContainsKey("Reward_" + currentFruitName))
        {
            choiceRecords["Reward_" + currentFruitName][threshold]++;
            LogManager.Instance.LogCheckQuestionResponse(currentTrialIndex + 1, 1, "TODO", "TODO", threshold.ToString(), true);

            bool correctChoice = false;
            if (currentFruitName == "Apple" && threshold == 40)
                correctChoice = true;
            else if (currentFruitName == "Grapes" && threshold == 30)
                correctChoice = true;
            else if (currentFruitName == "Watermelon" && threshold == 25)
                correctChoice = true;

            if (correctChoice)
            {
                correctChoiceScore++;
                debugText.text += $"\nCorrect choice: +1 (Total: {correctChoiceScore})";
            }
            else
            {
                debugText.text += $"\nIncorrect choice (Total: {correctChoiceScore})";
            }

            PlayerPrefs.SetInt("Check2Score", Mathf.Min(correctChoiceScore, 3));
            PlayerPrefs.Save();

            Invoke("ProcessNextTrial", 0.5f);
        }
    }

    private void ProcessNextTrial()
    {
        currentTrialIndex++;
        isProcessing = false;

        var navigationController = GetComponent<ButtonNavigationController>();
        if (navigationController != null)
        {
            navigationController.isProcessing = false;
            navigationController.EnableAllButtons();
            navigationController.ResetSelection();
        }

        if (currentTrialIndex < totalTrials)
        {
            SpawnRandomFruit();
        }
        else
        {
            LogManager.Instance.LogExperimentEnd();
            LoadNextScene();
        }
    }

    private void UpdateButtonHighlight()
    {
        // Reset all buttons to normal color first
        choice50Button.GetComponent<Image>().color = normalColor;
        choice70Button.GetComponent<Image>().color = normalColor;
        choice90Button.GetComponent<Image>().color = normalColor;

        // If there's a selection, highlight the selected button
        if (currentSelection >= 0 && currentSelection <= 2)
        {
            Button selectedButton = currentSelection switch
            {
                0 => choice50Button,
                1 => choice70Button,
                2 => choice90Button,
                _ => null
            };

            if (selectedButton != null)
            {
                selectedButton.GetComponent<Image>().color = selectedButtonColor;
            }
        }
    }

    private void LoadNextScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Check3_ComprehensionQuiz");
    }

    private void InitializeChoiceRecords()
    {
        choiceRecords["Reward_Apple"] = new Dictionary<int, int> { { 50, 0 }, { 70, 0 }, { 90, 0 } };
        choiceRecords["Reward_Grapes"] = new Dictionary<int, int> { { 50, 0 }, { 70, 0 }, { 90, 0 } };
        choiceRecords["Reward_Watermelon"] = new Dictionary<int, int> { { 50, 0 }, { 70, 0 }, { 90, 0 } };
    }

    public Dictionary<string, Dictionary<int, int>> GetChoiceRecords()
    {
        return new Dictionary<string, Dictionary<int, int>>(choiceRecords);
    }
}