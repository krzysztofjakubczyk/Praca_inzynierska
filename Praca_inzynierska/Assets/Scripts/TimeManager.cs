using UnityEngine;

public class TimeManager : MonoBehaviour
{
    private bool isPaused = false; // Flaga okreœlaj¹ca, czy gra jest zatrzymana

    void Update()
    {
        // Sprawdzenie, czy naciœniêto klawisz np. P
        if (Input.GetKeyDown(KeyCode.P))
        {
            TogglePause();
        }
    }

    // Funkcja prze³¹czaj¹ca pauzê
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
        Time.timeScale = 1f; // Przywrócenie normalnego czasu
        isPaused = false;
    }
}
