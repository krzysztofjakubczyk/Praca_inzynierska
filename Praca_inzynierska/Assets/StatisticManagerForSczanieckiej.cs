using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatisticManagerForSczanieckiej : MonoBehaviour
{
    [SerializeField] private float waitingTime = 1f;
    [SerializeField] private List<EntryTrigger> entryTriggers;
    [SerializeField] private List<DensitySensor> densitySensors;

    [SerializeField] private List<TMP_Text> sredniaText;
    [SerializeField] private List<TMP_Text> iloscText;
    [SerializeField] private List<TMP_Text> nazwaPasaText;
    [SerializeField] private List<TMP_Text> przepustowoscText;
    [SerializeField] private List<TMP_Text> obciazenieText;
    [SerializeField] private List<TMP_Text> gestoscText;
    [SerializeField] private List<TMP_Text> oczekiwanieText;
    [SerializeField] private TMP_Dropdown phaseDropdown;
    [SerializeField] private GameObject statisticsPanel;
    [SerializeField] private GameObject hourPanel;
    [SerializeField] private GameObject cameraPanel;
    [SerializeField] private Button toggleButton;
    [SerializeField] private Button exitButton;

    private bool isPanelVisible = false;
    private static Dictionary<int, List<float>> waitingTimesPerLane = new Dictionary<int, List<float>>();
    private static Dictionary<int, List<float>> timeSpentPerLane = new Dictionary<int, List<float>>();
    public static Dictionary<int, int> vehicleCountPerLane = new Dictionary<int, int>();


    private Dictionary<int, List<int>> phaseToLanes = new Dictionary<int, List<int>>
    {
        { 1, new List<int> { 1, 2, 6, 7 } },
        { 2, new List<int> { 3, 4, 5 } },
        { 3, new List<int> { 8, 9 } }
    };

    private Dictionary<int, int> natezenieNasycenia = new Dictionary<int, int>
{
    { 0, 425 },  // Plater od Firlika WL
    { 1, 463 },  // Plater od Firlika PW
    { 2, 510 },  // Sczanieckiej L
    { 3, 600 },  // Sczanieckiej W
    { 4, 555 },  // Sczanieckiej PW
    { 5, 425 },  // Plater od Staszica L
    { 6, 463 },  // Plater od Staszica PW
    { 7, 570 },  // Gontyny WL
    { 8, 570 }   // Gontyny PW
};


    private void Start()
    {
        phaseDropdown.onValueChanged.AddListener(delegate { UpdateLanesUI(); });
        statisticsPanel.SetActive(isPanelVisible);
        StartCoroutine(UpdateUIRoutine());
    }

    private float GetVehicleDensityForLane(int laneID)
    {
        if (vehicleCountPerLane.ContainsKey(laneID))
        {
            return vehicleCountPerLane[laneID] / 1.0f; // Gęstość na godzinę
        }
        return 0f;
    }

    public void ResetStatistics()
    {
        // Zerowanie liczników pojazdów na wlotach
        foreach (EntryTrigger entry in entryTriggers)
        {
            entry.count = 0;
            entry.travelTimes.Clear();
        }

        // Zerowanie czujników gęstości
        foreach (DensitySensor sensor in densitySensors)
        {
            sensor.ResetCounter();
        }

        // Zerowanie globalnych słowników statystyk
        waitingTimesPerLane.Clear();
        timeSpentPerLane.Clear();
        vehicleCountPerLane.Clear();

        // Zerowanie UI statystyk
        foreach (TMP_Text text in sredniaText) text.text = "0 s";
        foreach (TMP_Text text in oczekiwanieText) text.text = "0 s";
        foreach (TMP_Text text in iloscText) text.text = "0";
        foreach (TMP_Text text in nazwaPasaText) text.text = "";
        foreach (TMP_Text text in przepustowoscText) text.text = "0 poj/h";
        foreach (TMP_Text text in obciazenieText) text.text = "0";
        foreach (TMP_Text text in gestoscText) text.text = "0 poj/km";


        UpdateLanesUI(); // Aktualizacja interfejsu użytkownika
    }


    public void ToggleStatisticsPanel()
    {
        isPanelVisible = !isPanelVisible;
        toggleButton.gameObject.SetActive(!isPanelVisible);
        statisticsPanel.SetActive(isPanelVisible);
        cameraPanel.SetActive(!isPanelVisible);
        TimeManager time = FindAnyObjectByType<TimeManager>();
        if (!time.isAfternoonPeakOnly)
        {
            hourPanel.SetActive(!isPanelVisible);
        }
    }

    private float GetAverageWaitingTimeForLane(int laneID)
    {
        if (waitingTimesPerLane.ContainsKey(laneID) && waitingTimesPerLane[laneID].Count > 0)
        {
            return waitingTimesPerLane[laneID].Average();
        }
        return 0f; // Jeśli brak danych, zwracamy 0
    }
    private float GetAverageTimeSpentForLane(int laneID)
    {
        if (timeSpentPerLane.ContainsKey(laneID) && timeSpentPerLane[laneID].Count > 0)
        {
            return timeSpentPerLane[laneID].Average();
        }
        return 0f; // Jeśli brak danych, zwracamy 0
    }

    public static void RecordTimeSpent(int laneID, float timeSpent)
    {
        if (!timeSpentPerLane.ContainsKey(laneID))
        {
            timeSpentPerLane[laneID] = new List<float>();
        }

        timeSpentPerLane[laneID].Add(timeSpent);
    }

    public static void RecordWaitingTime(int laneID, float waitingTime)
    {
        if (!waitingTimesPerLane.ContainsKey(laneID))
        {
            waitingTimesPerLane[laneID] = new List<float>();
        }

        waitingTimesPerLane[laneID].Add(waitingTime);
    }


    private IEnumerator UpdateUIRoutine()
    {
        while (true)
        {
            if (isPanelVisible)
            {
                UpdateLanesUI();
            }
            yield return new WaitForSeconds(waitingTime);
        }
    }

    private void UpdateLanesUI()
    {
        if (!isPanelVisible) return;

        int selectedPhase = phaseDropdown.value + 1;
        if (!phaseToLanes.ContainsKey(selectedPhase)) return;

        List<int> lanes = phaseToLanes[selectedPhase];

        for (int i = 0; i < sredniaText.Count; i++)
        {
            if (i < lanes.Count)
            {
                int laneID = lanes[i];
                EntryTrigger entry = FindEntryTrigger(laneID);
                DensitySensor sensor = densitySensors.FirstOrDefault(s => s.gameObject.name.StartsWith(laneID.ToString()));

                if (entry != null && sensor != null)
                {
                    float avgTimeSpent = GetAverageTimeSpentForLane(laneID);
                    float avgWaitingTime = GetAverageWaitingTimeForLane(laneID);
                    float natezenie = entry.count / (waitingTime / 3600f);
                    float przepustowosc = natezenieNasycenia.ContainsKey(laneID) ? natezenieNasycenia[laneID] : 0;
                    float stopienObciazenia = (przepustowosc > 0) ? natezenie / przepustowosc : 0;

                    float gestosc = GetVehicleDensityForLane(laneID); // Pobieramy gęstość

                    sredniaText[i].text = $"{avgTimeSpent:F2} s";
                    oczekiwanieText[i].text = $"{avgWaitingTime:F2} s";
                    iloscText[i].text = $"{entry.count}";
                    nazwaPasaText[i].text = $"Pas: {laneID}";
                    przepustowoscText[i].text = $"{przepustowosc:F0} poj/h";
                    obciazenieText[i].text = $"{stopienObciazenia:F2}";
                    gestoscText[i].text = $"{gestosc:F1} poj/km";
                }
                else
                {
                    sredniaText[i].text = "Brak danych";
                    oczekiwanieText[i].text = "Brak danych";
                    iloscText[i].text = "Brak danych";
                    nazwaPasaText[i].text = $"Pas: {laneID}";
                    przepustowoscText[i].text = "Brak danych";
                    obciazenieText[i].text = "Brak danych";
                    gestoscText[i].text = "Brak danych";
                }
            }
            else
            {
                sredniaText[i].text = "";
                oczekiwanieText[i].text = "";
                iloscText[i].text = "";
                nazwaPasaText[i].text = "";
                przepustowoscText[i].text = "";
                obciazenieText[i].text = "";
                gestoscText[i].text = "";
            }
        }
    }



    private EntryTrigger FindEntryTrigger(int laneID)
    {
        return entryTriggers.FirstOrDefault(entry => entry.name.Contains(laneID.ToString()));
    }
}
