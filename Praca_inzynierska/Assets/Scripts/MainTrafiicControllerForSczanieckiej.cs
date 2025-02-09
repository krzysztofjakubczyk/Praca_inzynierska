using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainTrafficControllerForSczanieckiej : MonoBehaviour
{
    [Header("List of lanes to concrete cycle")]
    [SerializeField] private List<LineLightManager> Phase1 = new List<LineLightManager>();
    [SerializeField] private List<LineLightManager> Phase2 = new List<LineLightManager>();
    [SerializeField] private List<LineLightManager> Phase3 = new List<LineLightManager>();
    private List<List<LineLightManager>> listToChangeColors;

    [Header("Time Management")]
    public int choosedHour;
    public float currentCycleTime = 0f;
    private const int fullCycleTime = 120;
    [SerializeField] private float yellowLightDuration = 3f;

    private Dictionary<int, List<int>> phaseStartTimesByHour = new Dictionary<int, List<int>>
    {
        { 7, new List<int> { 1, 36, 87 } },
        { 12, new List<int> { 1, 36, 87 } },
        { 15, new List<int> { 1, 36, 77 } },
        { 21, new List<int> { 1, 36, 77 } }
    };

    private Dictionary<int, List<int>> phaseDurationsByHour = new Dictionary<int, List<int>>
    {
        { 7, new List<int> { 30, 46, 27 } },
        { 12, new List<int> { 30, 46, 27 } },
        { 15, new List<int> { 30, 36, 37 } },
        { 21, new List<int> { 30, 36, 37 } }
    };

    private void Start()
    {
        listToChangeColors = new List<List<LineLightManager>> { Phase1, Phase2, Phase3 };

        Debug.Log("⏳ Oczekiwanie 2 sekundy przed uruchomieniem cyklu świateł...");
        StartCoroutine(StartTrafficCycleWithDelay());
    }

    // NOWA FUNKCJA: Opóźnione uruchomienie `TraficCycle()`, aby `TimeManager` miał czas na ustawienie `choosedHour`
    private IEnumerator StartTrafficCycleWithDelay()
    {
        yield return new WaitForSeconds(2f); // Czekamy 2 sekundy

        TimeManager timeManager = FindObjectOfType<TimeManager>();
        if (timeManager != null)
        {
            choosedHour = timeManager.GetChoosedHour(); // Pobieramy godzinę dopiero po opóźnieniu
            Debug.Log($"⏰ Godzina po opóźnieniu: {choosedHour}");
        }

        Debug.Log("🚦 Rozpoczynam cykl świateł!");
        StartCoroutine(TraficCycle());
    }


    private IEnumerator TraficCycle()
    {
        while (true)
        {
            currentCycleTime = 0f; // Reset cyklu

           
            List<int> startTimes = phaseStartTimesByHour[choosedHour];
            List<int> durations = phaseDurationsByHour[choosedHour];

            for (int phaseIndex = 0; phaseIndex < listToChangeColors.Count; phaseIndex++)
            {
                if (phaseIndex >= startTimes.Count || phaseIndex >= durations.Count)
                {
                    Debug.LogError("❌ Błąd przypisania czasów faz. Sprawdź liczbę faz w harmonogramie.");
                    continue;
                }

                int startTime = startTimes[phaseIndex];
                int greenTime = durations[phaseIndex];

                Debug.Log($"🚦 Faza {phaseIndex + 1} | Start: {startTime}s | Czas trwania: {greenTime}s");

                yield return new WaitForSeconds(startTime - currentCycleTime);

                List<Coroutine> runningCoroutines = new List<Coroutine>();

                foreach (var line in listToChangeColors[phaseIndex])
                {
                    runningCoroutines.Add(StartCoroutine(HandleLaneCycle(line, greenTime)));
                }

                currentCycleTime = startTime;
                yield return new WaitForSeconds(greenTime + yellowLightDuration);

                currentCycleTime += greenTime;
            }

            Debug.Log("🔄 Restart cyklu świateł!");
            yield return new WaitForSeconds(fullCycleTime - currentCycleTime);
        }
    }

    private IEnumerator HandleLaneCycle(LineLightManager line, int greenTime)
    {
        Debug.Log($"🚦 {line.name} ZIELONE: Czas trwania = {greenTime}s");

        line.ChangeColor(TrafficLightColor.green);
        yield return new WaitForSeconds(greenTime);

        Debug.Log($"🟡 {line.name} ŻÓŁTE: {yellowLightDuration}s");
        line.ChangeColor(TrafficLightColor.yellow);
        yield return new WaitForSeconds(yellowLightDuration);

        Debug.Log($"🔴 {line.name} CZERWONE!");
        line.ChangeColor(TrafficLightColor.red);
    }
    public void ResetTrafficCycle()
    {
        StopAllCoroutines(); // Zatrzymanie wszystkich korutyn

        foreach (var phase in listToChangeColors)
        {
            foreach (var line in phase)
            {
                line.ChangeColor(TrafficLightColor.red); // Wszystkie światła na czerwone
            }
        }

        currentCycleTime = 0f; // Resetowanie licznika cyklu
        Debug.Log("🔄 Cykl świateł został zresetowany.");

        StartCoroutine(StartTrafficCycleWithDelay()); // Ponowne uruchomienie cyklu świateł
    }

}
