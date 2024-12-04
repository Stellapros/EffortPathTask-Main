using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class CheckManager3 : MonoBehaviour
{
    [System.Serializable]
    public class Question
    {
        public string questionText;
        public string optionA;
        public string optionB;
        public string optionC;
        public char correctAnswer; // 'A', 'B', or 'C'
    }

    public Question[] questions = new Question[4]
    {
        new Question
        {
            questionText = "Which of the two FRUITS needs more presses to get?",
            optionA = "The [FRUIT1]",
            optionB = "The [FRUIT2]",
            optionC = "They have required the same amount of presses",
            correctAnswer = 'B'
        },
        new Question
        {
            questionText = "Your objective in the game is to:",
            optionA = "Choose to work for every fruit",
            optionB = "Press as many buttons as you can",
            optionC = "Choose to work for fruit when you want in the time available",
            correctAnswer = 'C'
        },
        new Question
        {
            questionText = "You will visit two islands during your time in the game. What can differ between the two islands?",
            optionA = "The types of fruits you will see",
            optionB = "How often you will see each kind of fruits",
            optionC = "The amount of points you will get",
            correctAnswer = 'B'
        },
        new Question
        {
            questionText = "What does it mean when you successfully collect a fruit?",
            optionA = "I pressed more buttons",
            optionB = "My bonus payment stays the same",
            optionC = "I get more points that increases my bonus",
            correctAnswer = 'C'
        }
    };

    public TextMeshProUGUI questionText;
    public TextMeshProUGUI optionAText;
    public TextMeshProUGUI optionBText;
    public TextMeshProUGUI optionCText;
    public Button buttonA;
    public Button buttonB;
    public Button buttonC;
    public TextMeshProUGUI attemptText; // Add this to show remaining attempts


    private int currentQuestionIndex = 0;
    private int score = 0;
    
    // Static variable to track attempts across scene reloads
    private static int failedAttempts = 0;

    void Start()
    {
        // Add click listeners to buttons
        buttonA.onClick.AddListener(() => OnAnswerSelected('A'));
        buttonB.onClick.AddListener(() => OnAnswerSelected('B'));
        buttonC.onClick.AddListener(() => OnAnswerSelected('C'));

        // Display first question and attempt count
        DisplayCurrentQuestion();
        UpdateAttemptText();

        // Add in SetupButtons() method
ButtonNavigationController navigationController = gameObject.AddComponent<ButtonNavigationController>();
navigationController.AddElement(buttonA);
navigationController.AddElement(buttonB);
navigationController.AddElement(buttonC);
    }

    void UpdateAttemptText()
    {
        if (attemptText != null)
        {
            attemptText.text = $"Remaining Attempts: {2 - failedAttempts}";
        }
    }

    void DisplayCurrentQuestion()
    {
        Question currentQuestion = questions[currentQuestionIndex];
        questionText.text = $"Question {currentQuestionIndex + 1}/4:\n{currentQuestion.questionText}";
        optionAText.text = $"A. {currentQuestion.optionA}";
        optionBText.text = $"B. {currentQuestion.optionB}";
        optionCText.text = $"C. {currentQuestion.optionC}";
    }

    void OnAnswerSelected(char answer)
    {
        // Check if answer is correct
        if (answer == questions[currentQuestionIndex].correctAnswer)
        {
            score++;
        }

        // Move to next question or finish
        currentQuestionIndex++;
        
        if (currentQuestionIndex < questions.Length)
        {
            DisplayCurrentQuestion();
        }
        else
        {
            CalculateScoreAndProceed();
        }
    }

    void CalculateScoreAndProceed()
    {
        if (score >= 3)
        {
            // Reset failed attempts on success
            failedAttempts = 0;
            SceneManager.LoadScene("GetReadyFormal");
        }
        else
        {
            failedAttempts++;
            
            if (failedAttempts >= 2)
            {
                // Load the EndExperiment scene instead of quitting
                SceneManager.LoadScene("EndExperiment");
            }
            else
            {
                SceneManager.LoadScene("GetReadyPractise");
            }
        }
    }

    // void ShowQuitMessage()
    // {
    //     // Hide all other UI elements
    //     questionText.transform.parent.gameObject.SetActive(false);

    //     // Create and show quit message
    //     GameObject quitPanel = new GameObject("QuitPanel");
    //     quitPanel.transform.SetParent(transform);
        
    //     Text quitText = quitPanel.AddComponent<Text>();
    //     quitText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
    //     quitText.fontSize = 36;
    //     quitText.alignment = TextAnchor.MiddleCenter;
    //     quitText.color = Color.white;
    //     quitText.text = "You have failed the manipulation check twice.\nThe experiment will now end.\nThank you for your participation.";

    //     // Position the quit message in the center of the screen
    //     RectTransform rectTransform = quitText.GetComponent<RectTransform>();
    //     rectTransform.anchorMin = new Vector2(0, 0);
    //     rectTransform.anchorMax = new Vector2(1, 1);
    //     rectTransform.offsetMin = new Vector2(0, 0);
    //     rectTransform.offsetMax = new Vector2(0, 0);
    // }

    // void QuitApplication()
    // {
    //     #if UNITY_EDITOR
    //         UnityEditor.EditorApplication.isPlaying = false;
    //     #else
    //         Application.Quit();
    //     #endif
    // }

    // Optional: Add this method to reset attempts (useful for testing)
    public static void ResetAttempts()
    {
        failedAttempts = 0;
    }
}