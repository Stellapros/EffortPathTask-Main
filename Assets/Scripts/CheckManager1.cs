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

    [Header("UI Elements")]
    public Image leftFruitImage;
    public Image rightFruitImage;
    public Button leftChoiceButton;
    public Button rightChoiceButton;
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI phaseText;

    [Header("Check Questions Settings")]
    private int numberOfCheckQuestions = 6;

    private List<GameObject> fruitPool;
    private GameObject leftFruit;
    private GameObject rightFruit;
    private int currentTrialNumber = 0;
    private bool isInCheckPhase = true;
    private int checkQuestionsCompleted = 0;

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
        leftChoiceButton.onClick.AddListener(() => ProcessChoice("Left"));
        rightChoiceButton.onClick.AddListener(() => ProcessChoice("Right"));

        // Add in SetupUI() method
ButtonNavigationController navigationController = gameObject.AddComponent<ButtonNavigationController>();
navigationController.AddElement(leftChoiceButton);
navigationController.AddElement(rightChoiceButton);
    }

    void GenerateFruitPairs()
    {
        fruitPairs.Add((cherryPrefab, bananaPrefab));
        fruitPairs.Add((cherryPrefab, orangePrefab));
        fruitPairs.Add((bananaPrefab, orangePrefab));
        fruitPairs.Add((bananaPrefab, cherryPrefab));
        fruitPairs.Add((orangePrefab, cherryPrefab));
        fruitPairs.Add((orangePrefab, bananaPrefab));
    }

    void ShufflePairs()
    {
        fruitPairs = fruitPairs.OrderBy(x => Random.value).ToList();
    }

    void StartCheckQuestions()
    {
        isInCheckPhase = true;
        checkQuestionsCompleted = 0;
        currentTrialNumber = 0;

        phaseText.text = "Check Phase";
        SetupNewTrial();
    }

    void ProcessChoice(string choice)
    {
        LogManager.Instance.LogCheckQuestionResponse(currentTrialNumber, checkQuestionsCompleted, leftFruit.name, rightFruit.name, choice, false);

        checkQuestionsCompleted++;
        UpdateProgressText();

        leftChoiceButton.interactable = false;
        rightChoiceButton.interactable = false;

        Invoke("SetupNewTrial", 1.5f);
    }

    void SetupNewTrial()
    {
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
        var currentPair = fruitPairs[currentTrialNumber];

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

        currentTrialNumber++;
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