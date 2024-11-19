using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class CheckManager2 : MonoBehaviour
{
    [Header("Fruit Settings")]
    public GameObject cherryPrefab;
    public GameObject bananaPrefab;
    public GameObject orangePrefab;

    [SerializeField] private Image fruitImage;

    [Header("UI Elements")]
    [SerializeField] private Button choice50Button;
    [SerializeField] private Button choice70Button;
    [SerializeField] private Button choice90Button;
    [SerializeField] private TextMeshProUGUI questionText;

    private GameObject currentFruit;
    private readonly Dictionary<string, Dictionary<int, int>> choiceRecords = new Dictionary<string, Dictionary<int, int>>();
    private readonly List<GameObject> fruitPrefabs = new List<GameObject>();
    private int trialCount = 0;
    private int totalTrials = 3; // Set the desired number of trials
    private int currentTrialIndex = 0;

    private void Start()
    {
        InitializeChoiceRecords();
        SetupButtons();
        fruitPrefabs.AddRange(new[] { cherryPrefab, bananaPrefab, orangePrefab });

        // Start the first trial
        SpawnRandomFruit();
    }

    private void SetupButtons()
    {
        choice50Button.onClick.AddListener(() => RecordChoice(50));
        choice70Button.onClick.AddListener(() => RecordChoice(70));
        choice90Button.onClick.AddListener(() => RecordChoice(90));

        // Add in SetupUI() method
// Add in SetupButtons() method
ButtonNavigationController navigationController = gameObject.AddComponent<ButtonNavigationController>();
navigationController.AddButton(choice50Button);
navigationController.AddButton(choice70Button);
navigationController.AddButton(choice90Button);
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
        LogManager.Instance.LogTrialStart(currentTrialIndex + 1, 1, 0, 0, 0, false); // Adjust the block number and other parameters as needed
    }
}
    private void RecordChoice(int threshold)
    {
            if (currentFruit == null)
    {
        Debug.LogError("currentFruit is null, unable to record choice.");
        return;
    }
    string currentFruitName = currentFruit.name.Replace("(Clone)", "").Replace("Reward_", "");
        if (choiceRecords.ContainsKey("Reward_" + currentFruitName))
        {
            choiceRecords["Reward_" + currentFruitName][threshold]++;

            // Log the choice
            LogManager.Instance.LogCheckQuestionResponse(currentTrialIndex + 1, 1, "TODO", "TODO", threshold.ToString(), true); // Adjust the check question number and fruit names as needed

            // Log debugging information
            Debug.Log($"Recorded choice for {currentFruitName}: {threshold}%");
            LogCurrentStats("Reward_" + currentFruitName);

            // Move to the next trial
            currentTrialIndex++;
            if (currentTrialIndex < totalTrials)
            {
                SpawnRandomFruit();
            }
            else
            {
                // All trials completed, end the experiment and transition to the next scene
                LogManager.Instance.LogExperimentEnd();
                LoadNextScene();
            }
        }
        else
        {
            Debug.LogError($"Fruit name 'Reward_{currentFruitName}' not found in choiceRecords dictionary.");
        }
    }

    private void LogCurrentStats(string fruitName)
    {
        string stats = $"\nCurrent stats for {fruitName}:\n";
        foreach (var choice in choiceRecords[fruitName])
        {
            stats += $"{choice.Key}%: {choice.Value} times\n";
        }
        Debug.Log(stats);
    }

    public Dictionary<string, Dictionary<int, int>> GetChoiceRecords()
    {
        return new Dictionary<string, Dictionary<int, int>>(choiceRecords);
    }

private void InitializeChoiceRecords()
{
    choiceRecords["Reward_Cherries"] = new Dictionary<int, int> { { 50, 0 }, { 70, 0 }, { 90, 0 } };
    choiceRecords["Reward_Banana"] = new Dictionary<int, int> { { 50, 0 }, { 70, 0 }, { 90, 0 } };
    choiceRecords["Reward_Orange"] = new Dictionary<int, int> { { 50, 0 }, { 70, 0 }, { 90, 0 } };
}

    private void LoadNextScene()
    {
        // Replace "Check3_ComprehensionQuiz" with the actual name of the next scene
        UnityEngine.SceneManagement.SceneManager.LoadScene("Check3_ComprehensionQuiz");
    }
}