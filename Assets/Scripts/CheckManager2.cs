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
    [SerializeField] private TextMeshProUGUI debugText; // New debug text component

    private GameObject currentFruit;
    private readonly Dictionary<string, Dictionary<int, int>> choiceRecords = new Dictionary<string, Dictionary<int, int>>();
    private readonly List<GameObject> fruitPrefabs = new List<GameObject>();
    private int trialCount = 0;
    private int totalTrials = 3;
    private int currentTrialIndex = 0;
    private bool isProcessing = false;
    private int correctChoiceScore = 0; // Track correct choices

    private void Start()
    {
        InitializeChoiceRecords();
        SetupButtons();
        fruitPrefabs.AddRange(new[] { applePrefab, grapesPrefab, watermelonPrefab });

        // Start the first trial
        SpawnRandomFruit();
    }

    private void SetupButtons()
    {
        choice50Button.onClick.AddListener(() => RecordChoice(50));
        choice70Button.onClick.AddListener(() => RecordChoice(70));
        choice90Button.onClick.AddListener(() => RecordChoice(90));

        ButtonNavigationController navigationController = gameObject.AddComponent<ButtonNavigationController>();
        navigationController.AddElement(choice50Button);
        navigationController.AddElement(choice70Button);
        navigationController.AddElement(choice90Button);
    }

    private void SpawnRandomFruit()
    {
        // Destroy previous fruit if it exists
        if (currentFruit != null)
        {
            Destroy(currentFruit.gameObject);
        }

        // Randomly select and spawn a fruit
        int randomIndex = Random.Range(0, fruitPrefabs.Count);
        GameObject selectedFruit = fruitPrefabs[randomIndex];
        currentFruit = Instantiate(selectedFruit, Vector3.zero, Quaternion.identity);

        // Set the fruit image
        if (fruitImage != null)
        {
            fruitImage.sprite = currentFruit.GetComponent<SpriteRenderer>().sprite;
            fruitImage.rectTransform.sizeDelta = new Vector2(100, 100);
        }

        // Update question text
        string fruitName = selectedFruit.name.Replace("(Clone)", "").Replace("Reward_", "");
        questionText.text = $"Which threshold did you see most frequently with this {fruitName}?";

        // Remove the used fruit prefab from the list
        fruitPrefabs.RemoveAt(randomIndex);

        // Increment the trial count
        trialCount++;

        // Start the new trial
        if (LogManager.Instance != null)
        {
            LogManager.Instance.LogTrialStart(currentTrialIndex + 1, 1, 0, 0, false);
        }

        // Update debug text
        debugText.text = $"Trial {currentTrialIndex + 1}: {fruitName}";
    }

    private void RecordChoice(int threshold)
    {
        // Immediate guard against multiple processing
        if (currentFruit == null || isProcessing) return;

        // Synchronization flag
        isProcessing = true;

        string currentFruitName = currentFruit.name.Replace("(Clone)", "").Replace("Reward_", "");

        if (choiceRecords.ContainsKey("Reward_" + currentFruitName))
        {
            choiceRecords["Reward_" + currentFruitName][threshold]++;
            LogManager.Instance.LogCheckQuestionResponse(currentTrialIndex + 1, 1, "TODO", "TODO", threshold.ToString(), true);

            // Disable buttons during processing
            choice50Button.interactable = false;
            choice70Button.interactable = false;
            choice90Button.interactable = false;

            // Determine correct choice based on fruit type
            bool correctChoice = false;
            if (currentFruitName == "Apple" && threshold == 90)
                correctChoice = true;
            else if (currentFruitName == "Grapes" && threshold == 70)
                correctChoice = true;
            else if (currentFruitName == "Watermelon" && threshold == 50)
                correctChoice = true;

            // Update score if correct
            if (correctChoice)
            {
                correctChoiceScore++;
                debugText.text += $"\nCorrect choice: +1 (Total: {correctChoiceScore})";
            }
            else
            {
                debugText.text += $"\nIncorrect choice (Total: {correctChoiceScore})";
            }

            // Save the score to PlayerPrefs with a max of 3
            PlayerPrefs.SetInt("Check2Score", Mathf.Min(correctChoiceScore, 3));
            PlayerPrefs.Save();

            // Use Invoke to manage progression and reset
            Invoke("ProcessNextTrial", 0.5f);
        }
    }

    private void ProcessNextTrial()
    {
        currentTrialIndex++;
        isProcessing = false;

        // Re-enable buttons
        choice50Button.interactable = true;
        choice70Button.interactable = true;
        choice90Button.interactable = true;

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