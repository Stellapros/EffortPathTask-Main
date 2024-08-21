using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;


public class ArrowKeyCounter : MonoBehaviour
{
    public TextMeshProUGUI counterText;  // Reference to the TextMeshProUGUI component
    private int counter = 0;
    private float elapsedTime = 0f;
    private bool gameStopped = false;

    void Update()
    {
        if (gameStopped) return;

        elapsedTime += Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow) ||
            Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            counter++;
            UpdateCounterText();
            Debug.Log("Counter value is: " + counter);  // Print the count value to the Console
        }

        if (elapsedTime >= 5f)
        {
            StopGame();
        }
    }

    void UpdateCounterText()
    {
        counterText.text = counter.ToString();
    }

    void StopGame()
    {
        gameStopped = true;
        //-- CounterData.totalKeyPresses = counter;
        Debug.Log("Total key presses: " + counter); //--  CounterData.totalKeyPresses);  
        PlayerPrefs.SetInt("totalKeyPresses", counter);
        SceneManager.LoadScene("Scene_Game");
    }
}
