using UnityEngine;

public class TimeManager : MonoBehaviour
{
    private bool isPaused = false; // Flaga okre�laj�ca, czy gra jest zatrzymana

    void Update()
    {
        // Sprawdzenie, czy naci�ni�to klawisz np. P
        if (Input.GetKeyDown(KeyCode.P))
        {
            TogglePause();
        }
    }

    // Funkcja prze��czaj�ca pauz�
    void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    // Zatrzymanie gry
    void PauseGame()
    {
        Time.timeScale = 0f; // Zatrzymanie czasu
        isPaused = true;
    }

    // Wznowienie gry
    void ResumeGame()
    {
        Time.timeScale = 1f; // Przywr�cenie normalnego czasu
        isPaused = false;
    }
}
