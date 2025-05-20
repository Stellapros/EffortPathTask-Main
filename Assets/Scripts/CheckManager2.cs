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
    [SerializeField] private Button choice17Button;
    [SerializeField] private Button choice33Button;
    [SerializeField] private Button choice50Button;
    [SerializeField] private TextMeshProUGUI questionText;
    [SerializeField] private TextMeshProUGUI debugText;
    [SerializeField] private TextMeshProUGUI trialIndexText;
    [SerializeField] private TextMeshProUGUI buttonInstructionText;

    [Header("Audio")]
    private AudioSource audioSource;
    public AudioClip successSound;
    public AudioClip failSound;

    // Time tracking variables
    private float questionStartTime;
    private float phaseStartTime;

    private GameObject currentFruit;
    private readonly Dictionary<string, Dictionary<int, int>> choiceRecords = new Dictionary<string, Dictionary<int, int>>();
    private readonly List<string> fruitTypes = new List<string> { "Apple", "Grapes", "Watermelon" };
    private int trialCount = 0;
    private int totalTrials = 6; // 3 fruits x 2 islands = 6 trials
    private int currentTrialIndex = 0;
    private bool isProcessing = false;
    private int correctChoiceScore = 0;

    // Island tracking
    private bool isGreenIsland = true; // Start with Green Island
    private int islandTrialCount = 0;  // Track trials per island

    // Track which fruits have been shown on each island
    private List<string> greenIslandFruits;
    private List<string> blueIslandFruits;

    private int currentSelection = -1; // Start with no selection
    private readonly int[] thresholdOptions = { 17, 33, 50 };
    private Color defaultButtonColor;
    private Color selectedButtonColor = new Color(0.87f, 0.86f, 0.67f);
    [SerializeField] private Color normalColor = new Color(0.67f, 0.87f, 0.86f);
    [SerializeField] private Color disabledColor = new Color(0.7f, 0.7f, 0.7f);

    // Island-specific correct answers
    private readonly Dictionary<string, Dictionary<string, int>> correctAnswers = new Dictionary<string, Dictionary<string, int>>
    {
        { "Green", new Dictionary<string, int> { { "Apple", 50 }, { "Grapes", 33 }, { "Watermelon", 17 } } },
        { "Blue", new Dictionary<string, int> { { "Apple", 17 }, { "Grapes", 33 }, { "Watermelon", 50 } } }
    };

    private void Start()
    {
        // Add this code to ensure we have an AudioSource
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Initialize phase start time when the check phase begins
        phaseStartTime = Time.time;

        InitializeChoiceRecords();
        SetupButtons();
        defaultButtonColor = choice17Button.GetComponent<Image>().color;

        // Initialize lists to track which fruits have been shown on each island
        greenIslandFruits = new List<string>(fruitTypes);
        blueIslandFruits = new List<string>(fruitTypes);

        PrepareNextTrial();
        UpdateButtonHighlight();
    }

    private void PrepareNextTrial()
    {
        SpawnNextFruit();
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
        Debug.Log("Setting up choice buttons");
        choice17Button.onClick.AddListener(() =>
        {
            Debug.Log("17 button clicked");
            RecordChoice(17);
        });
        choice33Button.onClick.AddListener(() =>
        {
            Debug.Log("33 button clicked");
            RecordChoice(33);
        });
        choice50Button.onClick.AddListener(() =>
        {
            Debug.Log("50 button clicked");
            RecordChoice(50);
        });
    }

    private void DisableAllButtons()
    {
        choice17Button.interactable = false;
        choice33Button.interactable = false;
        choice50Button.interactable = false;

        // Set all buttons to disabled color
        choice17Button.GetComponent<Image>().color = disabledColor;
        choice33Button.GetComponent<Image>().color = disabledColor;
        choice50Button.GetComponent<Image>().color = disabledColor;
    }

    private void EnableAllButtons()
    {
        choice17Button.interactable = true;
        choice33Button.interactable = true;
        choice50Button.interactable = true;

        // Reset all buttons to normal color
        choice17Button.GetComponent<Image>().color = normalColor;
        choice33Button.GetComponent<Image>().color = normalColor;
        choice50Button.GetComponent<Image>().color = normalColor;
    }

    private void SpawnNextFruit()
    {
        if (currentFruit != null)
        {
            Destroy(currentFruit.gameObject);
        }

        // Set question start time when spawning a new fruit
        questionStartTime = Time.time;

        // Clear debug text at the start of each new trial
        debugText.text = "";

        // Select the next fruit based on which island we're currently on
        string fruitName;
        if (isGreenIsland)
        {
            // Get next fruit from green island list
            int randomIndex = Random.Range(0, greenIslandFruits.Count);
            fruitName = greenIslandFruits[randomIndex];
            greenIslandFruits.RemoveAt(randomIndex);
        }
        else
        {
            // Get next fruit from blue island list
            int randomIndex = Random.Range(0, blueIslandFruits.Count);
            fruitName = blueIslandFruits[randomIndex];
            blueIslandFruits.RemoveAt(randomIndex);
        }

        // Get the appropriate fruit prefab
        GameObject selectedFruit = null;
        switch (fruitName)
        {
            case "Apple":
                selectedFruit = applePrefab;
                break;
            case "Grapes":
                selectedFruit = grapesPrefab;
                break;
            case "Watermelon":
                selectedFruit = watermelonPrefab;
                break;
        }

        currentFruit = Instantiate(selectedFruit, Vector3.zero, Quaternion.identity);

        if (fruitImage != null)
        {
            fruitImage.sprite = currentFruit.GetComponent<SpriteRenderer>().sprite;
            fruitImage.rectTransform.sizeDelta = new Vector2(100, 100);
        }

        string islandName = isGreenIsland ? "Green" : "Blue";
        questionText.text = $"How often did you see this {fruitName} on {islandName} island?";
        buttonInstructionText.text = "Use ←/→ to choose; Press Space or Enter to confirm";

        trialCount++;
        islandTrialCount++;

        if (trialIndexText != null)
        {
            trialIndexText.text = $"Question: {currentTrialIndex + 1} of {totalTrials}";
        }

        // Reset selection and enable buttons
        currentSelection = -1;
        EnableAllButtons();
        UpdateButtonHighlight();
    }

    private void RecordChoice(int threshold)
    {
        if (currentFruit == null || isProcessing) return;
        isProcessing = true;

        DisableAllButtons();

        string currentFruitName = currentFruit.name.Replace("(Clone)", "").Replace("Reward_", "");
        float responseTime = Time.time - questionStartTime;
        string islandName = isGreenIsland ? "Green" : "Blue";

        // Get correct threshold for this fruit on this island
        int correctThreshold = correctAnswers[islandName][currentFruitName];
        bool isCorrect = threshold == correctThreshold;

        // Simplified logging with T/F for correct/incorrect
        LogManager.Instance.LogCheckQuestionResponse(
            trialNumber: currentTrialIndex,
            checkPhase: 2,
            questionNumber: currentTrialIndex,
            questionType: "FruitFrequency",
            questionText: questionText.text,
            selectedAnswer: isCorrect ? "T" : "F",
            correctAnswer: "T",
            isCorrect: isCorrect,
            responseTime: responseTime,
            new Dictionary<string, string>
            {
                {"FruitType", currentFruitName},
                {"Island", islandName}
            }
        );

        // Add feedback to the debug text
        if (isCorrect)
        {
            debugText.text += $"\nCorrect! The {currentFruitName} appeared {threshold}% of the time on {islandName} island.";
            if (successSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(successSound);
            }
        }
        else
        {
            debugText.text += $"\nIncorrect. The {currentFruitName} appeared {correctThreshold}% of the time on {islandName} island.";
            if (failSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(failSound);
            }
        }

        if (isCorrect)
        {
            correctChoiceScore++;
        }

        // // If this was the last question, log the phase completion
        // if (currentTrialIndex == totalTrials - 1)
        // {
        //     LogManager.Instance.LogCheckPhaseComplete(
        //         checkPhase: 2,
        //         totalQuestions: totalTrials,
        //         correctAnswers: correctChoiceScore,
        //         completionTime: Time.time - phaseStartTime,
        //         phaseType: "FrequencyCheck",
        //         new Dictionary<string, string>
        //         {
        //         {"PercentageCorrect", ((float)correctChoiceScore / totalTrials * 100).ToString("F1") + "%"}
        //         }
        //     );
        // }

        PlayerPrefs.SetInt("Check2Score", Mathf.Min(correctChoiceScore, 6));
        PlayerPrefs.Save();

        Invoke("ProcessNextTrial", 1.0f);
    }

    private void ProcessNextTrial()
    {
        Debug.Log("ProcessNextTrial called. Current trial index: " + currentTrialIndex);
        currentTrialIndex++;
        isProcessing = false;

        var navigationController = GetComponent<ButtonNavigationController>();
        if (navigationController != null)
        {
            navigationController.isProcessing = false;
            navigationController.EnableAllButtons();
            navigationController.ResetSelection();
        }

        // Check if we need to switch islands
        if (islandTrialCount >= 3)
        {
            islandTrialCount = 0;
            isGreenIsland = !isGreenIsland; // Switch islands
        }

        if (currentTrialIndex < totalTrials)
        {
            Debug.Log("Starting next trial");
            PrepareNextTrial();
        }
        else
        {
            Debug.Log("All trials completed, logging phase completion");

            // // Log the phase completion
            // LogManager.Instance.LogCheckPhaseComplete(
            //     checkPhase: 2,
            //     totalQuestions: totalTrials,
            //     correctAnswers: correctChoiceScore,
            //     completionTime: Time.time - phaseStartTime,
            //     phaseType: "FrequencyCheck",
            //     new Dictionary<string, string>
            //     {
            //         {"PercentageCorrect", ((float)correctChoiceScore / totalTrials * 100).ToString("F1") + "%"}
            //     }
            // );

            Debug.Log("Loading next scene");
            LogManager.Instance.LogExperimentEnd();
            LoadNextScene();
        }
    }

    private void UpdateButtonHighlight()
    {
        // Reset all buttons to normal color first
        choice17Button.GetComponent<Image>().color = normalColor;
        choice33Button.GetComponent<Image>().color = normalColor;
        choice50Button.GetComponent<Image>().color = normalColor;

        // If there's a selection, highlight the selected button
        if (currentSelection >= 0 && currentSelection <= 2)
        {
            Button selectedButton = currentSelection switch
            {
                0 => choice17Button,
                1 => choice33Button,
                2 => choice50Button,
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
        Debug.Log("LoadNextScene called. Loading scene: Check3_ComprehensionQuiz");
        UnityEngine.SceneManagement.SceneManager.LoadScene("Check3_ComprehensionQuiz");
    }

    private void InitializeChoiceRecords()
    {
        choiceRecords["Reward_Apple"] = new Dictionary<int, int> { { 17, 0 }, { 33, 0 }, { 50, 0 } };
        choiceRecords["Reward_Grapes"] = new Dictionary<int, int> { { 17, 0 }, { 33, 0 }, { 50, 0 } };
        choiceRecords["Reward_Watermelon"] = new Dictionary<int, int> { { 17, 0 }, { 33, 0 }, { 50, 0 } };
    }

    public Dictionary<string, Dictionary<int, int>> GetChoiceRecords()
    {
        return new Dictionary<string, Dictionary<int, int>>(choiceRecords);
    }
}