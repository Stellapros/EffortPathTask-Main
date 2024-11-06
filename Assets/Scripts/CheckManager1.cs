using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Linq;
using TMPro;

public class CheckManager1 : MonoBehaviour
{
    [Header("Fruit Prefabs")]
    public GameObject cherryPrefab;
    public GameObject bananaPrefab;
    public GameObject orangePrefab;

    [Header("Spawn Positions")]
    // public Transform leftSpawnPoint;
    // public Transform rightSpawnPoint;

    [Header("Fruit Image Frames")]
    public Image leftFruitImage;
    public Image rightFruitImage;

    [Header("UI Elements")]
    public Button leftChoiceButton;
    public Button rightChoiceButton;
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI phaseText;

    [Header("Check Questions Settings")]
    public int numberOfCheckQuestions = 3;
    public float checkPassThreshold = 2;

    [Header("Data Recording")]
    public string participantID = "P001";

    [Header("Transition to Next Scene")]
    public string nextSceneName = "Check2_Recognition";

    private List<GameObject> fruitPool;
    private GameObject leftFruit;
    private GameObject rightFruit;
    private int currentTrialNumber = 0;
    private bool isInCheckPhase = true;
    private int checkQuestionsCompleted = 0;
    private int correctCheckResponses = 0;

    private List<(GameObject, GameObject)> fruitPairs = new List<(GameObject, GameObject)>();
    private List<string> choiceResults = new List<string>();

    void Start()
    {
        fruitPool = new List<GameObject> { cherryPrefab, bananaPrefab, orangePrefab };
        SetupUI();
        GenerateFruitPairs();
        ShufflePairs();
        StartCheckQuestions();
    }

    void SetupUI()
    {
        // Remove existing button listeners and add new ones for fruit images
        leftChoiceButton.onClick.RemoveAllListeners();
        rightChoiceButton.onClick.RemoveAllListeners();

        leftChoiceButton.onClick.AddListener(() => ProcessChoice("Left"));
        rightChoiceButton.onClick.AddListener(() => ProcessChoice("Right"));
    }

    void GenerateFruitPairs()
    {
        // Generate all possible combinations of fruits
        for (int i = 0; i < fruitPool.Count; i++)
        {
            for (int j = i + 1; j < fruitPool.Count; j++)
            {
                fruitPairs.Add((fruitPool[i], fruitPool[j]));
            }
        }
    }

    void ShufflePairs()
    {
        // Shuffle the fruit pairs
        fruitPairs = fruitPairs.OrderBy(x => Random.value).ToList();
    }

    void StartCheckQuestions()
    {
        isInCheckPhase = true;
        checkQuestionsCompleted = 0;
        correctCheckResponses = 0;
        currentTrialNumber = 0;

        phaseText.text = "Practice Phase";
        SetupNewTrial();
    }

    void ProcessChoice(string choice)
    {
        if (isInCheckPhase)
        {
            ProcessCheckResponse(choice);
        }
        else
        {
            LogDecisionMade(currentTrialNumber, choice);
            SetupNewTrial();
        }
    }

    void ProcessCheckResponse(string choice)
    {
        bool isCorrect = EvaluateCheckResponse(choice);

        if (isCorrect)
        {
            correctCheckResponses++;
            instructionText.text = "Correct!";
        }
        else
        {
            instructionText.text = "Incorrect. Please try to be more careful.";
        }

        checkQuestionsCompleted++;
        UpdateProgressText();

        // Disable buttons temporarily
        leftChoiceButton.interactable = false;
        rightChoiceButton.interactable = false;

        // Log the check question response
        LogManager.Instance.LogCheckQuestionResponse(currentTrialNumber, checkQuestionsCompleted, leftFruit.name, rightFruit.name, choice, isCorrect);

        Invoke("SetupNewTrial", 1.5f);
    }

    void LogDecisionMade(int trialNumber, string choice)
    {
        LogManager.Instance.LogDecisionMade(trialNumber, choice);
    }

    bool EvaluateCheckResponse(string choice)
    {
        // Implement your check response evaluation logic here
        string leftFruitName = leftFruit.name.Replace("(Clone)", "");
        string rightFruitName = rightFruit.name.Replace("(Clone)", "");

        // Example logic - modify based on your requirements
        bool bananaPresentOnLeft = leftFruitName.Contains("Banana");
        bool bananaPresentOnRight = rightFruitName.Contains("Banana");

        if (bananaPresentOnLeft && choice == "Left") return true;
        if (bananaPresentOnRight && choice == "Right") return true;

        return false;
    }

    void EvaluateCheckPhase()
    {
        if (correctCheckResponses >= checkPassThreshold)
        {
            // Passed check questions, proceed to main experiment
            isInCheckPhase = false;
            currentTrialNumber = 0;
            phaseText.text = "Main Experiment";
            instructionText.text = "Great! Now let's go to the next stage.";
            Invoke("SetupNewTrial", 2.0f);
        }
        else
        {
            // Failed check questions, restart check phase
            instructionText.text = "Let's try the practice questions again.";
            Invoke("StartCheckQuestions", 2.0f);
        }
    }

    void SetupNewTrial()
    {
        // Clear previous fruits
        if (leftFruit != null) Destroy(leftFruit);
        if (rightFruit != null) Destroy(rightFruit);

        if (isInCheckPhase)
        {
            if (checkQuestionsCompleted >= numberOfCheckQuestions)
            {
                EvaluateCheckPhase();
                return;
            }
            SetupCheckTrial();
        }
        else
        {
            if (currentTrialNumber >= fruitPairs.Count)
            {
                TransitionToNextScene();
                return;
            }
            SetupMainTrial();
        }
    }
    void SetupCheckTrial()
    {
        // Randomly select a fruit pair from the list
        var currentPair = fruitPairs[Random.Range(0, fruitPairs.Count)];

        // Randomly decide left/right position
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

        // Display the fruit images in the separate image frames
        leftFruitImage.sprite = leftFruit.GetComponent<SpriteRenderer>().sprite;
        rightFruitImage.sprite = rightFruit.GetComponent<SpriteRenderer>().sprite;

        // Enable choice buttons
        leftChoiceButton.interactable = true;
        rightChoiceButton.interactable = true;

        instructionText.text = "Which fruit would you choose?";
    }
    void SetupMainTrial()
    {
        var currentPair = fruitPairs[currentTrialNumber];

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

        // Display the fruit images in the separate image frames
        leftFruitImage.sprite = leftFruit.GetComponent<SpriteRenderer>().sprite;
        rightFruitImage.sprite = rightFruit.GetComponent<SpriteRenderer>().sprite;

        leftChoiceButton.interactable = true;
        rightChoiceButton.interactable = true;

        currentTrialNumber++;
        instructionText.text = "Which fruit would you choose?";
    }

    void TransitionToNextScene()
    {
        // Save results and then load the next scene
        LogManager.Instance.LogExperimentEnd();
        SceneManager.LoadScene(nextSceneName);
    }

    void UpdateProgressText()
    {
        progressText.text = $"Check Questions Completed: {checkQuestionsCompleted}/{numberOfCheckQuestions}";
    }
}