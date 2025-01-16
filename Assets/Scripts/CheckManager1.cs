using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Linq;
using TMPro;

public class CheckManager1 : MonoBehaviour
{
    [Header("Fruit Prefabs")]
    public GameObject applePrefab;
    public GameObject grapesPrefab;
    public GameObject watermelonPrefab;

    [Header("UI Elements")]
    public Image leftFruitImage;
    public Image rightFruitImage;
    public Button leftChoiceButton;
    public Button rightChoiceButton;
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI phaseText;
    public TextMeshProUGUI debugText;

    [Header("Check Questions Settings")]
    private int numberOfCheckQuestions = 6;

    private List<GameObject> fruitPool;
    private GameObject leftFruit;
    private GameObject rightFruit;
    private int currentTrialNumber = 0;
    // private bool isInCheckPhase = true;
    private int checkQuestionsCompleted = 0;
    private bool isProcessing = false;

    private List<(GameObject, GameObject)> fruitPairs = new List<(GameObject, GameObject)>();
    private int correctChoiceScore = 0;

    void Start()
    {
        fruitPool = new List<GameObject> { applePrefab, grapesPrefab, watermelonPrefab };
        SetupUI();
        GenerateFruitPairs();
        ShufflePairs();
        StartCheckQuestions();
    }

    void SetupUI()
    {
        leftChoiceButton.onClick.AddListener(() => HandleButtonChoice("Left"));
        rightChoiceButton.onClick.AddListener(() => HandleButtonChoice("Right"));

        ButtonNavigationController navigationController = gameObject.AddComponent<ButtonNavigationController>();
        navigationController.AddElement(leftChoiceButton);
        navigationController.AddElement(rightChoiceButton);
    }

    void HandleButtonChoice(string choice)
    {
        if (isProcessing) return;
        isProcessing = true;

        ProcessChoice(choice);
    }

    void GenerateFruitPairs()
    {
        fruitPairs.Clear();

        // Explicitly add all unique pairs with known preferences
        fruitPairs.Add((applePrefab, grapesPrefab));     // Apple preferred
        fruitPairs.Add((applePrefab, watermelonPrefab)); // Apple preferred
        fruitPairs.Add((grapesPrefab, watermelonPrefab)); // Grapes preferred

        // Ensure reciprocal pairs for comprehensive testing
        fruitPairs.Add((grapesPrefab, applePrefab));      // Showing Apple preference again
        fruitPairs.Add((watermelonPrefab, applePrefab));  // Showing Apple preference
        fruitPairs.Add((watermelonPrefab, grapesPrefab)); // Showing Grapes preference
    }

    void ShufflePairs()
    {
        fruitPairs = fruitPairs.OrderBy(x => Random.value).ToList();
    }

    void StartCheckQuestions()
    {
        // isInCheckPhase = true;
        checkQuestionsCompleted = 0;
        currentTrialNumber = 0;
        correctChoiceScore = 0;

        phaseText.text = "Check Phase";
        SetupNewTrial();
    }

    void ProcessChoice(string choice)
    {
        Debug.Log($"Processing Choice: Current Trial {currentTrialNumber}, Total Pairs {fruitPairs.Count}");

        // Ensure we don't go out of bounds
        if (currentTrialNumber >= fruitPairs.Count)
        {
            TransitionToNextScene();
            return;
        }

        var currentPair = fruitPairs[currentTrialNumber];
        Debug.Log($"Current Pair: {currentPair.Item1.name} vs {currentPair.Item2.name}");

        bool correctChoice = false;

        // Detailed preference rules with explicit handling for all pairs
        if (currentPair.Item1.name == "Apple" && currentPair.Item2.name == "Grapes")
        {
            correctChoice = (leftFruit.name.Contains("Apple") && choice == "Left") ||
                            (rightFruit.name.Contains("Apple") && choice == "Right");
        }
        else if (currentPair.Item1.name == "Apple" && currentPair.Item2.name == "Watermelon")
        {
            correctChoice = (leftFruit.name.Contains("Apple") && choice == "Left") ||
                            (rightFruit.name.Contains("Apple") && choice == "Right");
        }
        else if (currentPair.Item1.name == "Grapes" && currentPair.Item2.name == "Watermelon")
        {
            correctChoice = (leftFruit.name.Contains("Grapes") && choice == "Left") ||
                            (rightFruit.name.Contains("Grapes") && choice == "Right");
        }
        // Reverse pair cases
        else if (currentPair.Item1.name == "Grapes" && currentPair.Item2.name == "Apple")
        {
            correctChoice = (leftFruit.name.Contains("Apple") && choice == "Left") ||
                            (rightFruit.name.Contains("Apple") && choice == "Right");
        }
        else if (currentPair.Item1.name == "Watermelon" && currentPair.Item2.name == "Apple")
        {
            correctChoice = (leftFruit.name.Contains("Apple") && choice == "Left") ||
                            (rightFruit.name.Contains("Apple") && choice == "Right");
        }
        else if (currentPair.Item1.name == "Watermelon" && currentPair.Item2.name == "Grapes")
        {
            correctChoice = (leftFruit.name.Contains("Grapes") && choice == "Left") ||
                            (rightFruit.name.Contains("Grapes") && choice == "Right");
        }
        // Uncomment and restore the increment of currentTrialNumber
        currentTrialNumber++;

        // Update score if correct
        if (correctChoice)
        {
            correctChoiceScore++;
            // UpdateProgressText();
        }

        // Increment trial and questions completed
        checkQuestionsCompleted++;

        // Update UI and logging
        UpdateProgressText();
        debugText.text += $"\nCorrect choice: {correctChoice} (Total: {correctChoiceScore})";

        // Log the response
        LogManager.Instance.LogCheckQuestionResponse(
            currentTrialNumber - 1,  // Use previous trial number for logging
            checkQuestionsCompleted,
            leftFruit ? leftFruit.name : "Unknown",
            rightFruit ? rightFruit.name : "Unknown",
            choice,
            correctChoice
        );

        // Disable buttons
        leftChoiceButton.interactable = false;
        rightChoiceButton.interactable = false;

        // Save the score to PlayerPrefs with a max of 6
        PlayerPrefs.SetInt("Check1Score", Mathf.Min(correctChoiceScore, 6));
        PlayerPrefs.Save();

        // Setup next trial
        Invoke("SetupNewTrial", 1.5f);
    }


    void SetupNewTrial()
    {
        isProcessing = false;

        if (leftFruit != null) Destroy(leftFruit);
        if (rightFruit != null) Destroy(rightFruit);

        if (checkQuestionsCompleted >= numberOfCheckQuestions)
        {
            TransitionToNextScene();
            return;
        }

        SetupCheckTrial();
    }

    void SetupCheckTrial()
    {
        // Ensure we don't go out of bounds
        if (currentTrialNumber >= fruitPairs.Count)
        {
            TransitionToNextScene();
            return;
        }

        var currentPair = fruitPairs[currentTrialNumber];

        if (leftFruit != null) Destroy(leftFruit);
        if (rightFruit != null) Destroy(rightFruit);

        // Randomly determine the left and right fruit
        if (Random.value > 0.5)
        {
            leftFruit = Instantiate(currentPair.Item1);
            rightFruit = Instantiate(currentPair.Item2);
        }
        else
        {
            leftFruit = Instantiate(currentPair.Item2);
            rightFruit = Instantiate(currentPair.Item1);
        }

        // Randomly set the left and right fruit positions
        if (Random.value > 0.5)
        {
            leftFruit.transform.localPosition = new Vector3(-3f, 0f, 0f);
            rightFruit.transform.localPosition = new Vector3(3f, 0f, 0f);
        }
        else
        {
            leftFruit.transform.localPosition = new Vector3(3f, 0f, 0f);
            rightFruit.transform.localPosition = new Vector3(-3f, 0f, 0f);
        }

        leftFruitImage.sprite = leftFruit.GetComponent<SpriteRenderer>().sprite;
        rightFruitImage.sprite = rightFruit.GetComponent<SpriteRenderer>().sprite;

        leftChoiceButton.interactable = true;
        rightChoiceButton.interactable = true;

        instructionText.text = "Which fruit would you choose?";

        // currentTrialNumber++;

        // Update debug text
        debugText.text = $"Trial {currentTrialNumber + 1}: {leftFruit.name} vs {rightFruit.name}";
    }

    void TransitionToNextScene()
    {
        LogManager.Instance.LogExperimentEnd();
        SceneManager.LoadScene("Check2_Recognition");
    }

    void UpdateProgressText()
    {
        progressText.text = $"Check Questions Completed: {checkQuestionsCompleted}/{numberOfCheckQuestions}";
    }
}