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

    private void Start()
    {
        InitializeChoiceRecords();
        SetupButtons();
        SpawnRandomFruit();
    }

    private void InitializeChoiceRecords()
    {
        choiceRecords["Cherry"] = new Dictionary<int, int> { { 50, 0 }, { 70, 0 }, { 90, 0 } };
        choiceRecords["Banana"] = new Dictionary<int, int> { { 50, 0 }, { 70, 0 }, { 90, 0 } };
        choiceRecords["Orange"] = new Dictionary<int, int> { { 50, 0 }, { 70, 0 }, { 90, 0 } };
    }

    private void SetupButtons()
    {
        choice50Button.onClick.AddListener(() => RecordChoice(50));
        choice70Button.onClick.AddListener(() => RecordChoice(70));
        choice90Button.onClick.AddListener(() => RecordChoice(90));
    }

    private void SpawnRandomFruit()
    {
        // Destroy previous fruit if it exists
        if (currentFruit != null)
        {
            Destroy(currentFruit.gameObject);
        }

        // Randomly select and spawn a fruit
        int randomIndex = Random.Range(0, 3);
        GameObject selectedFruit = new[] { cherryPrefab, bananaPrefab, orangePrefab }[randomIndex];
        currentFruit = Instantiate(selectedFruit, fruitImage.transform.position, Quaternion.identity);

        // Ensure the fruit game object has a RectTransform component
        RectTransform fruitRect = currentFruit.AddComponent<RectTransform>();
        fruitRect.sizeDelta = fruitImage.rectTransform.sizeDelta;

        // Update question text
        string fruitName = selectedFruit.name.Replace("(Clone)", "");
        questionText.text = $"Which threshold did you see most frequently with this {fruitName}?";
    }

    private void RecordChoice(int threshold)
    {
        string currentFruitName = currentFruit.name.Replace("(Clone)", "");
        choiceRecords[currentFruitName][threshold]++;

        // Log the choice for debugging
        Debug.Log($"Recorded choice for {currentFruitName}: {threshold}%");
        LogCurrentStats(currentFruitName);

        // Spawn next fruit
        SpawnRandomFruit();
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
}