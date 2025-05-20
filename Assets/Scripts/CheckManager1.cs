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
    [Header("Audio")]
    private AudioSource audioSource;
    public AudioClip successSound;
    public AudioClip failSound;

    [Header("Check Questions Settings")]
    private int numberOfCheckQuestions = 6;
    private List<GameObject> fruitPool;
    private GameObject leftFruit;
    private GameObject rightFruit;
    private int currentTrialNumber = 1;
    private int checkQuestionsCompleted = 0;
    private bool isProcessing = false;
    private int currentSelection = -1;

    private List<(GameObject, GameObject)> fruitPairs = new List<(GameObject, GameObject)>();
    private int correctChoiceScore = 0;
    private float questionStartTime;
    private float phaseStartTime;


    private void Awake()
    {
        Debug.Log("Check1_Preference Awake() called");
        Debug.Log($"Check1_Preference - ResumeFromCheck: {PlayerPrefs.GetInt("ResumeFromCheck", 0)}");
        Debug.Log($"Check1_Preference - PracticeBlocksCompleted: {PlayerPrefs.GetInt("PracticeBlocksCompleted", 0)}");

        // Ensure we're properly set up for this check
        PracticeManager practiceManager = FindAnyObjectByType<PracticeManager>();
        if (practiceManager != null)
        {
            practiceManager.PauseUpdates(true); // Pause updates while we're in the check
        }
    }

    void Start()
    {
        // Add this code to ensure we have an AudioSource
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        phaseStartTime = Time.time;

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
            if (Input.GetKeyDown(KeyCode.A))
            {
                currentSelection = 0;
                UpdateButtonHighlight();
            }
            else if (Input.GetKeyDown(KeyCode.D))
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
        Debug.Log($"Processing Choice: Current Trial {currentTrialNumber + 1}, Total Pairs {fruitPairs.Count}");

        if (currentTrialNumber > fruitPairs.Count)
        {
            TransitionToNextScene();
            return;
        }

        float responseTime = Time.time - questionStartTime;
        var currentPair = fruitPairs[currentTrialNumber];
        Debug.Log($"Current Pair: {currentPair.Item1.name} vs {currentPair.Item2.name}");

        bool isCorrect = false;
        string correctFruit = "";

        if (currentPair.Item1.name == "Apple" && currentPair.Item2.name == "Grapes")
        {
            correctFruit = "Apple";
            isCorrect = (leftFruit.name.Contains("Apple") && choice == "Left") ||
                            (rightFruit.name.Contains("Apple") && choice == "Right");
        }
        else if (currentPair.Item1.name == "Apple" && currentPair.Item2.name == "Watermelon")
        {
            correctFruit = "Apple";
            isCorrect = (leftFruit.name.Contains("Apple") && choice == "Left") ||
                            (rightFruit.name.Contains("Apple") && choice == "Right");
        }
        else if (currentPair.Item1.name == "Grapes" && currentPair.Item2.name == "Watermelon")
        {
            correctFruit = "Grapes";
            isCorrect = (leftFruit.name.Contains("Grapes") && choice == "Left") ||
                            (rightFruit.name.Contains("Grapes") && choice == "Right");
        }
        else if (currentPair.Item1.name == "Grapes" && currentPair.Item2.name == "Apple")
        {
            correctFruit = "Apple";
            isCorrect = (leftFruit.name.Contains("Apple") && choice == "Left") ||
                            (rightFruit.name.Contains("Apple") && choice == "Right");
        }
        else if (currentPair.Item1.name == "Watermelon" && currentPair.Item2.name == "Apple")
        {
            correctFruit = "Apple";
            isCorrect = (leftFruit.name.Contains("Apple") && choice == "Left") ||
                            (rightFruit.name.Contains("Apple") && choice == "Right");
        }
        else if (currentPair.Item1.name == "Watermelon" && currentPair.Item2.name == "Grapes")
        {
            correctFruit = "Grapes";
            isCorrect = (leftFruit.name.Contains("Grapes") && choice == "Left") ||
                            (rightFruit.name.Contains("Grapes") && choice == "Right");
        }

        // Play appropriate sound effect based on correctness
        if (isCorrect)
        {
            correctChoiceScore++;
            debugText.text += $"\nCorrect! You selected the {correctFruit}.";
            if (successSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(successSound);
            }
        }
        else
        {
            // Find which side the correct fruit was on
            string correctPosition = leftFruit.name.Contains(correctFruit) ? "Left" : "Right";
            debugText.text += $"\nIncorrect. The correct choice was the {correctFruit} on the {correctPosition}.";
            if (failSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(failSound);
            }
        }

        // Enhanced logging
        LogManager.Instance.LogCheckQuestionResponse(
            trialNumber: currentTrialNumber,
            checkPhase: 1,
            questionNumber: currentTrialNumber,
            questionType: "FruitPreference",
            questionText: $"\"Choose preferred fruit between {leftFruit.name} and {rightFruit.name}\"",
            selectedAnswer: isCorrect ? "T" : "F",
            correctAnswer: "T",
            isCorrect: isCorrect,
            responseTime: responseTime,
            new Dictionary<string, string>
            {
        {"LeftFruit", leftFruit.name},
        {"RightFruit", rightFruit.name}
            }
        );

        checkQuestionsCompleted++;
        UpdateProgressText();

        if (checkQuestionsCompleted > numberOfCheckQuestions)
        {
            LogManager.Instance.LogCheckPhaseComplete(
                checkPhase: 1,
                totalQuestions: numberOfCheckQuestions,
                correctAnswers: correctChoiceScore,
                completionTime: Time.time - phaseStartTime,
                phaseType: "PreferenceCheck",
                new Dictionary<string, string>
                {
            {"PercentageCorrect", ((float)correctChoiceScore / numberOfCheckQuestions * 100).ToString("F1") + "%"}
                }
            );
        }

        PlayerPrefs.SetInt("Check1Score", Mathf.Min(correctChoiceScore, 6));
        PlayerPrefs.Save();

        currentTrialNumber++;
        Invoke("SetupNewTrial", 1.0f);
    }

    // Helper method for determining correct answer
    private string DetermineCorrectAnswer((GameObject, GameObject) pair)
    {
        if (pair.Item1.name.Contains("Apple") || pair.Item2.name.Contains("Apple"))
            return "Apple";
        if (pair.Item1.name.Contains("Grapes") || pair.Item2.name.Contains("Grapes"))
            return "Grapes";
        return "Watermelon";
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

        questionStartTime = Time.time; // Add time tracking for each trial
        var currentPair = fruitPairs[currentTrialNumber];

        // Clear debug text at the start of each new trial
        debugText.text = "";

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

        instructionText.text = "In order to save energy, which fruit would you prefer to choose?";
        // buttonInstructionText.text = "Use ← → to choose; Press Space or Enter to confirm";
        // buttonInstructionText.text = "Use A/D to choose";
        buttonInstructionText.text = "Use A/D or ← → to choose";
        // debugText.text = $"Trial {currentTrialNumber + 1} of {fruitPairs.Count}: {leftFruit.name} vs {rightFruit.name}";
        UpdateProgressText();
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
        var shuffledPairs = new List<(GameObject, GameObject)>();
        var remainingPairs = new List<(GameObject, GameObject)>(fruitPairs);

        // Create groups of non-overlapping pairs
        var groups = new List<List<(GameObject, GameObject)>>();

        while (remainingPairs.Count > 0)
        {
            var currentGroup = new List<(GameObject, GameObject)>();
            var pairsToRemove = new List<(GameObject, GameObject)>();

            // Try to add each remaining pair to the current group
            foreach (var pair in remainingPairs)
            {
                bool canAddToGroup = true;
                foreach (var groupPair in currentGroup)
                {
                    if (SharesFruits(pair, groupPair))
                    {
                        canAddToGroup = false;
                        break;
                    }
                }

                if (canAddToGroup)
                {
                    currentGroup.Add(pair);
                    pairsToRemove.Add(pair);
                }
            }

            // Remove used pairs and add the group
            foreach (var pair in pairsToRemove)
            {
                remainingPairs.Remove(pair);
            }

            if (currentGroup.Count > 0)
            {
                groups.Add(currentGroup);
            }
        }

        // Shuffle pairs within each group
        foreach (var group in groups)
        {
            for (int i = group.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var temp = group[i];
                group[i] = group[j];
                group[j] = temp;
            }
        }

        // Shuffle the groups themselves
        for (int i = groups.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var temp = groups[i];
            groups[i] = groups[j];
            groups[j] = temp;
        }

        // Combine all pairs from the shuffled groups
        foreach (var group in groups)
        {
            shuffledPairs.AddRange(group);
        }

        fruitPairs = shuffledPairs;
    }

    private bool SharesFruits((GameObject, GameObject) pair1, (GameObject, GameObject) pair2)
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

    // void TransitionToNextScene()
    // {
    //     LogManager.Instance.LogExperimentEnd();
    //     SceneManager.LoadScene("Check2_Recognition");
    //     // SceneManager.LoadScene("Check3_ComprehensionQuiz");
    // }

    void TransitionToNextScene()
    {
        // LogManager.Instance.LogCheckPhaseComplete(
        //     checkPhase: 1,
        //     totalQuestions: numberOfCheckQuestions,
        //     correctAnswers: correctChoiceScore,
        //     completionTime: Time.time - phaseStartTime,
        //     phaseType: "PreferenceCheck",
        //     new Dictionary<string, string>
        //     {
        //         {"PercentageCorrect", ((float)correctChoiceScore / numberOfCheckQuestions * 100).ToString("F1") + "%"}
        //     }
        // );

        // Check if we need to return to practice blocks
        if (PlayerPrefs.GetInt("ResumeFromCheck", 0) == 1)
        {
            Debug.Log("Check1_Preference completed. Returning to practice blocks.");

            // Here we are NOT resetting the ResumeFromCheck flag
            // Instead, we're updating the blocks completed
            // int blocksCompleted = PlayerPrefs.GetInt("PracticeBlocksCompleted", 0);
            PlayerPrefs.SetInt("IsPracticeTrial", 1);
            PlayerPrefs.Save();

            // Find PracticeManager if it exists
            PracticeManager practiceManager = FindAnyObjectByType<PracticeManager>();
            if (practiceManager != null)
            {
                // Enable updates before calling ResumeFromCheck
                practiceManager.PauseUpdates(false);

                // Let PracticeManager handle returning to practice
                practiceManager.ResumeFromCheck();
            }
            else
            {
                Debug.LogError("PracticeManager not found! Falling back to direct scene load.");
                // If PracticeManager not found, load PracticePhase scene
                SceneManager.LoadScene("PracticePhase");
            }
        }
        else
        {
            // Normal progression - go to next check scene
            Debug.Log("Check1_Preference completed. Moving to Check2_Recognition.");
            LogManager.Instance.LogExperimentEnd();
            SceneManager.LoadScene("Check2_Recognition");
        }
    }

    void UpdateProgressText()
    {
        progressText.text = $"Check Questions Completed: {checkQuestionsCompleted}/{numberOfCheckQuestions}";
        if (trialIndexText != null)
        {
            trialIndexText.text = $"Question: {currentTrialNumber + 1}/{fruitPairs.Count}";
        }
    }
}