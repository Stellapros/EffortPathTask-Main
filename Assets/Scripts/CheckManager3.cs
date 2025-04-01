using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

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
        optionA = "The Apple",
        optionB = "The Watermelon",
        optionC = "They have required the same amount of presses",
        correctAnswer = 'B'
        },
        new Question
        {
            questionText = "Your objective in the game is to:",
            optionA = "Choose to work for every fruit",
            optionB = "Press as many buttons as you can",
            optionC = "Choose to work for fruit when you want within the available time",
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
    public TextMeshProUGUI attemptText;
    public TextMeshProUGUI debugText;
    public TextMeshProUGUI trialIndexText; // New text component for trial index
    public TextMeshProUGUI buttonInstructionText; // New text for button instructions
    [SerializeField] private Color normalColor = new Color(0.67f, 0.87f, 0.86f);
    [SerializeField] private Color disabledColor = new Color(0.7f, 0.7f, 0.7f);
    private int currentQuestionIndex = 0;
    private int comprehensionScore = 0;

    // Static variables to track total score and attempts across scene reloads
    private static int failedAttempts = 0;
    private bool isProcessing = false;
    private float questionStartTime;
    private float phaseStartTime;
    void Start()
    {
        phaseStartTime = Time.time;
        questionStartTime = Time.time;

        // Add click listeners to buttons
        buttonA.onClick.AddListener(() => OnAnswerSelected('A'));
        buttonB.onClick.AddListener(() => OnAnswerSelected('B'));
        buttonC.onClick.AddListener(() => OnAnswerSelected('C'));

        // Display first question and attempt count
        DisplayCurrentQuestion();
        UpdateAttemptText();

        // Add button navigation
        ButtonNavigationController navigationController = gameObject.AddComponent<ButtonNavigationController>();
        navigationController.AddElement(buttonA);
        navigationController.AddElement(buttonB);
        navigationController.AddElement(buttonC);

        // Retrieve and validate scores from PlayerPrefs
        int check1Score = ValidateScore(PlayerPrefs.GetInt("Check1Score", 0), 6);
        int check2Score = ValidateScore(PlayerPrefs.GetInt("Check2Score", 0), 3);

        // Calculate and display debug information
        UpdateDebugText(check1Score, check2Score);
        Debug.Log($"Score Update: check1Score = {check1Score}, check1Score = {check1Score}");
    }


    // Validate score to ensure it doesn't exceed maximum possible score
    int ValidateScore(int score, int maxScore)
    {
        return Mathf.Clamp(score, 0, maxScore);
    }

    void UpdateDebugText(int check1Score, int check2Score)
    {
        if (debugText != null)
        {
            int currentComprehensionScore = comprehensionScore;
            int totalScore = check1Score + check2Score + currentComprehensionScore;

            debugText.text = $"Debug Info:\n" +
                             $"Check 1 Score (Max 6): {check1Score}\n" +
                             $"Check 2 Score (Max 3): {check2Score}\n" +
                             $"Comprehension Score (Max 4): {currentComprehensionScore}\n" +
                             $"Current Total Score: {totalScore}\n" +
                             $"Remaining Attempts: {1 - failedAttempts}";

            Debug.Log($"Debug Information:\n" +
                             $"Check 1 Score (Max 6): {check1Score}\n" +
                             $"Check 2 Score (Max 3): {check2Score}\n" +
                             $"Comprehension Score (Max 4): {currentComprehensionScore}\n" +
                             $"Current Total Score: {totalScore}\n" +
                             $"Remaining Attempts: {1 - failedAttempts}");
        }
    }

    void UpdateAttemptText()
    {
        if (attemptText != null)
        {
            attemptText.text = $"Remaining Attempts: {1 - failedAttempts}";
        }
    }

    void DisplayCurrentQuestion()
    {
        questionStartTime = Time.time;

        Question currentQuestion = questions[currentQuestionIndex];
        // questionText.text = $"Question {currentQuestionIndex + 1}/4:\n{currentQuestion.questionText}";
        questionText.text = $"Question {currentQuestionIndex + 1}:\n{currentQuestion.questionText}";
        optionAText.text = $"A. {currentQuestion.optionA}";
        optionBText.text = $"B. {currentQuestion.optionB}";
        optionCText.text = $"C. {currentQuestion.optionC}";

        // Update trial index text
        if (trialIndexText != null)
        {
            trialIndexText.text = $"Question: {currentQuestionIndex + 1}/4";
        }

        buttonInstructionText.text = "Use ↑ ↓ to choose; Press Space or Enter to select/confirm\n";
    }

    void OnAnswerSelected(char answer)
    {
        // Immediate guard against multiple processing
        if (isProcessing) return;

        // Synchronization flag
        isProcessing = true;

        float responseTime = Time.time - questionStartTime;
        Question currentQuestion = questions[currentQuestionIndex];

        bool isCorrect = answer == questions[currentQuestionIndex].correctAnswer;
        LogManager.Instance.LogCheckQuestionResponse(
            trialNumber: currentQuestionIndex,
            checkPhase: 3,
            questionNumber: currentQuestionIndex,
            questionType: "ComprehensionQuiz",
            questionText: currentQuestion.questionText,
            selectedAnswer: isCorrect ? "T" : "F", // Simplified to T/F
            correctAnswer: "T", // Always expect T for correct
            isCorrect: isCorrect,
            responseTime: responseTime
        );


        // Disable all buttons during processing
        buttonA.interactable = false;
        buttonB.interactable = false;
        buttonC.interactable = false;

        // Change button colors to grey
        buttonA.GetComponent<Image>().color = disabledColor;
        buttonB.GetComponent<Image>().color = disabledColor;
        buttonC.GetComponent<Image>().color = disabledColor;

        var navigationController = GetComponent<ButtonNavigationController>();
        if (navigationController != null)
        {
            navigationController.isProcessing = true;
            navigationController.DisableAllButtons();
        }

        // Check answer
        if (answer == questions[currentQuestionIndex].correctAnswer)
        {
            comprehensionScore++;
            if (debugText != null)
            {
                debugText.text += $"\nQuestion {currentQuestionIndex + 1} Correct!";
            }
        }
        else
        {
            if (debugText != null)
            {
                debugText.text += $"\nQuestion {currentQuestionIndex + 1} Incorrect.";
            }
        }

        if (currentQuestionIndex == questions.Length - 1)
        {
            // Ensure final score is correct before logging phase completion
            int check1Score = PlayerPrefs.GetInt("Check1Score", 0);
            int check2Score = PlayerPrefs.GetInt("Check2Score", 0);
            int totalScore = check1Score + check2Score + comprehensionScore;

            Debug.Log($"Final Detailed Scores:\n" +
                     $"Check 1 (Max 6): {check1Score}\n" +
                     $"Check 2 (Max 3): {check2Score}\n" +
                     $"Comprehension (Max 4): {comprehensionScore}\n" +
                     $"Total Score: {totalScore}");
        }

        // Use Invoke to manage progression and reset
        Invoke("ProcessQuestionProgression", 0.5f);
    }


    private string GetQuestionDifficulty(int questionIndex)
    {
        // You can adjust these based on your assessment of question difficulty
        switch (questionIndex)
        {
            case 0: return "Medium";
            case 1: return "Easy";
            case 2: return "Hard";
            case 3: return "Medium";
            default: return "Unknown";
        }
    }

    void ProcessQuestionProgression()
    {
        currentQuestionIndex++;

        // Re-enable buttons
        buttonA.interactable = true;
        buttonB.interactable = true;
        buttonC.interactable = true;

        // Reset button colors to normal
        buttonA.GetComponent<Image>().color = normalColor;
        buttonB.GetComponent<Image>().color = normalColor;
        buttonC.GetComponent<Image>().color = normalColor;

        // Re-enable navigation but clear default selection
        var navigationController = GetComponent<ButtonNavigationController>();
        if (navigationController != null)
        {
            navigationController.isProcessing = false;
            navigationController.EnableAllButtons();
            navigationController.ResetSelection(); // Clear previous selection
        }

        isProcessing = false;

        if (currentQuestionIndex < questions.Length)
        {
            DisplayCurrentQuestion();
        }
        else
        {
            // Retrieve and validate previous scores
            int check1Score = ValidateScore(PlayerPrefs.GetInt("Check1Score", 0), 6);
            int check2Score = ValidateScore(PlayerPrefs.GetInt("Check2Score", 0), 3);

            // Calculate total score
            int totalScore = check1Score + check2Score + comprehensionScore;

            // Save the total score to PlayerPrefs
            PlayerPrefs.SetInt("TotalScore", totalScore);
            PlayerPrefs.Save();

            // Update debug text with final score
            if (debugText != null)
            {
                debugText.text += $"\n\nFinal Detailed Scores:\n" +
                                  $"Check 1 (Max 6): {check1Score}\n" +
                                  $"Check 2 (Max 3): {check2Score}\n" +
                                  $"Comprehension (Max 4): {comprehensionScore}\n" +
                                  $"Total Score: {totalScore}";
                Debug.Log($"Final Detailed Scores:\n" +
                                  $"Check 1 (Max 6): {check1Score}\n" +
                                  $"Check 2 (Max 3): {check2Score}\n" +
                                  $"Comprehension (Max 4): {comprehensionScore}\n" +
                                  $"Total Score: {totalScore}");
            }

            // Log phase completion
            LogManager.Instance.LogCheckPhaseComplete(
                checkPhase: 3,
                totalQuestions: questions.Length,
                correctAnswers: comprehensionScore,
                completionTime: Time.time - phaseStartTime,
                phaseType: "ComprehensionCheck",
                new Dictionary<string, string>
                {
                {"FailedAttempts", failedAttempts.ToString()},
                {"Check1Score", check1Score.ToString()},
                {"Check2Score", check2Score.ToString()},
                {"Check3Score", comprehensionScore.ToString()},
                {"TotalCheckScore", totalScore.ToString()}
                }
            );

            // Proceed to score calculation
            CalculateScoreAndProceed(totalScore);
        }
    }

    public void CalculateScoreAndProceed(int totalScore)
    {
        Debug.Log($"Total Score: {totalScore}, Failed Attempts: {failedAttempts}");

        if (totalScore >= 11)
        {
            failedAttempts = 0;
            PlayerPrefs.DeleteKey("PracticeAttempts"); // Clear practice attempts on success
            SceneManager.LoadScene("GetReadyFormal");
        }
        else
        {
            failedAttempts++;

            if (failedAttempts >= 2)
            {
                SceneManager.LoadScene("EndExperiment");
            }
            else
            {
                // Set flags for practice retry
                PlayerPrefs.SetInt("NeedsPracticeRetry", 1);
                PlayerPrefs.SetInt("PracticeAttempts", failedAttempts);
                PlayerPrefs.Save();

                Debug.Log("Failed checks, returning to practice");
                // Important: Reset practice state before loading the scene
                var practiceManager = FindAnyObjectByType<PracticeManager>();
                if (practiceManager != null)
                {
                    practiceManager.ResetPracticeForNewAttempt();
                }
                // SceneManager.LoadScene("GetReadyPractice");
                SceneManager.LoadScene("PracticePhase");
            }
        }
    }

    // Optional: Add this method to reset attempts and score (useful for testing)
    public static void ResetAttemptsAndScore()
    {
        failedAttempts = 0;
        PlayerPrefs.DeleteKey("Check1Score");
        PlayerPrefs.DeleteKey("Check2Score");
        PlayerPrefs.DeleteKey("TotalScore");
        PlayerPrefs.Save();
    }
}