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
    public TextMeshProUGUI trialIndexText;
    public TextMeshProUGUI buttonInstructionText;
    [SerializeField] private Color normalColor = new Color(0.67f, 0.87f, 0.86f);
    [SerializeField] private Color selectedColor = new Color(0.87f, 0.86f, 0.67f);
    [SerializeField] private Color disabledColor = new Color(0.7f, 0.7f, 0.7f);

    [Header("Check Questions Settings")]
    private int numberOfCheckQuestions = 6;
    private List<GameObject> fruitPool;
    private GameObject leftFruit;
    private GameObject rightFruit;
    private int currentTrialNumber = 0;
    private int checkQuestionsCompleted = 0;
    private bool isProcessing = false;
    private int currentSelection = -1;

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

    void Update()
    {
        if (!isProcessing)
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                currentSelection = 0;
                UpdateButtonHighlight();
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                currentSelection = 1;
                UpdateButtonHighlight();
            }

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                if (currentSelection == 0)
                {
                    HandleButtonChoice("Left");
                }
                else if (currentSelection == 1)
                {
                    HandleButtonChoice("Right");
                }
            }
        }
    }

    void SetupUI()
    {
        leftChoiceButton.onClick.AddListener(() => HandleButtonChoice("Left"));
        rightChoiceButton.onClick.AddListener(() => HandleButtonChoice("Right"));

        ButtonNavigationController navigationController = gameObject.AddComponent<ButtonNavigationController>();
        navigationController.AddElement(leftChoiceButton);
        navigationController.AddElement(rightChoiceButton);
    }

    private void DisableAllButtons()
    {
        leftChoiceButton.interactable = false;
        rightChoiceButton.interactable = false;

        // Keep the selected color for the chosen button, set disabled for the other
        if (currentSelection == 0)
        {
            rightChoiceButton.GetComponent<Image>().color = disabledColor;
        }
        else if (currentSelection == 1)
        {
            leftChoiceButton.GetComponent<Image>().color = disabledColor;
        }

        var navigationController = GetComponent<ButtonNavigationController>();
        if (navigationController != null)
        {
            navigationController.isProcessing = true;
            navigationController.DisableAllButtons(currentSelection);
        }
    }

    private void EnableAllButtons()
    {
        leftChoiceButton.interactable = true;
        rightChoiceButton.interactable = true;

        leftChoiceButton.GetComponent<Image>().color = normalColor;
        rightChoiceButton.GetComponent<Image>().color = normalColor;

        var navigationController = GetComponent<ButtonNavigationController>();
        if (navigationController != null)
        {
            navigationController.EnableAllButtons();
            navigationController.isProcessing = false;
            navigationController.ResetSelection();
        }

        currentSelection = -1;
        UpdateButtonHighlight();
    }

    private void UpdateButtonHighlight()
    {
        // Reset both buttons to normal color
        leftChoiceButton.GetComponent<Image>().color = normalColor;
        rightChoiceButton.GetComponent<Image>().color = normalColor;

        // Highlight the selected button
        if (currentSelection == 0)
        {
            leftChoiceButton.GetComponent<Image>().color = selectedColor;
        }
        else if (currentSelection == 1)
        {
            rightChoiceButton.GetComponent<Image>().color = selectedColor;
        }
    }

    private void HandleButtonChoice(string choice)
    {
        if (isProcessing) return;
        isProcessing = true;

        // Update button colors based on choice
        if (choice == "Left")
        {
            leftChoiceButton.GetComponent<Image>().color = selectedColor;
            rightChoiceButton.GetComponent<Image>().color = normalColor;
        }
        else
        {
            rightChoiceButton.GetComponent<Image>().color = selectedColor;
            leftChoiceButton.GetComponent<Image>().color = normalColor;
        }

        // Disable buttons immediately after selection
        DisableAllButtons();

        ProcessChoice(choice);
    }

    void ProcessChoice(string choice)
    {
        Debug.Log($"Processing Choice: Current Trial {currentTrialNumber}, Total Pairs {fruitPairs.Count}");

        if (currentTrialNumber >= fruitPairs.Count)
        {
            TransitionToNextScene();
            return;
        }

        var currentPair = fruitPairs[currentTrialNumber];
        Debug.Log($"Current Pair: {currentPair.Item1.name} vs {currentPair.Item2.name}");

        bool correctChoice = false;

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

        currentTrialNumber++;
        if (correctChoice)
        {
            correctChoiceScore++;
        }

        checkQuestionsCompleted++;

        UpdateProgressText();
        debugText.text += $"\nCorrect choice: {correctChoice} (Total: {correctChoiceScore})";

        LogManager.Instance.LogCheckQuestionResponse(
            currentTrialNumber - 1,
            checkQuestionsCompleted,
            leftFruit ? leftFruit.name : "Unknown",
            rightFruit ? rightFruit.name : "Unknown",
            choice,
            correctChoice
        );

        PlayerPrefs.SetInt("Check1Score", Mathf.Min(correctChoiceScore, 6));
        PlayerPrefs.Save();

        Invoke("SetupNewTrial", 1.5f);
    }

    void SetupNewTrial()
    {
        if (checkQuestionsCompleted >= numberOfCheckQuestions)
        {
            TransitionToNextScene();
            return;
        }

        isProcessing = false;
        SetupCheckTrial();
        EnableAllButtons();
    }

    void SetupCheckTrial()
    {
        if (currentTrialNumber >= fruitPairs.Count)
        {
            TransitionToNextScene();
            return;
        }

        var currentPair = fruitPairs[currentTrialNumber];

        if (leftFruit != null) Destroy(leftFruit);
        if (rightFruit != null) Destroy(rightFruit);

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

        instructionText.text = "In order to save energy, which fruit would you perfer to choose?";
        // buttonInstructionText.text = "Use ← → to choose; Press Space or Enter to confirm";
        buttonInstructionText.text = "Use ← → to choose";

        debugText.text = $"Trial {currentTrialNumber + 1} of {fruitPairs.Count}: {leftFruit.name} vs {rightFruit.name}";
        UpdateProgressText();
    }

    // Rest of the methods remain unchanged...

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
        var shuffledPairs = new List<(GameObject, GameObject)>();
        var tempPairs = new List<(GameObject, GameObject)>(fruitPairs);

        // Start with a random pair
        int randomIndex = Random.Range(0, tempPairs.Count);
        shuffledPairs.Add(tempPairs[randomIndex]);
        tempPairs.RemoveAt(randomIndex);

        // For each subsequent position
        while (tempPairs.Count > 0)
        {
            // Get the last added pair for comparison
            var lastPair = shuffledPairs[shuffledPairs.Count - 1];

            // Find valid pairs that don't share fruits with the last pair
            var validPairs = tempPairs.Where(p =>
                !SharesFruits(p, lastPair)).ToList();

            // If no valid pairs exist, just take any remaining pair
            if (validPairs.Count == 0)
            {
                validPairs = tempPairs;
            }

            // Select a random pair from valid options
            randomIndex = Random.Range(0, validPairs.Count);
            shuffledPairs.Add(validPairs[randomIndex]);
            tempPairs.Remove(validPairs[randomIndex]);
        }

        fruitPairs = shuffledPairs;
    }

    private bool SharesFruits(
        (GameObject, GameObject) pair1,
        (GameObject, GameObject) pair2)
    {
        return pair1.Item1.name == pair2.Item1.name ||
               pair1.Item1.name == pair2.Item2.name ||
               pair1.Item2.name == pair2.Item1.name ||
               pair1.Item2.name == pair2.Item2.name;
    }

    void StartCheckQuestions()
    {
        checkQuestionsCompleted = 0;
        currentTrialNumber = 0;
        correctChoiceScore = 0;

        phaseText.text = "Check Phase";
        SetupNewTrial();
    }

    void TransitionToNextScene()
    {
        LogManager.Instance.LogExperimentEnd();
        SceneManager.LoadScene("Check2_Recognition");
    }

    void UpdateProgressText()
    {
        progressText.text = $"Check Questions Completed: {checkQuestionsCompleted}/{numberOfCheckQuestions}";
        if (trialIndexText != null)
        {
            trialIndexText.text = $"Question: {checkQuestionsCompleted}/{fruitPairs.Count}";
        }
    }
}