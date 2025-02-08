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
    private static List<float> waitingTimes = new List<float>(); // Lista czasów oczekiwania

    private Dictionary<int, List<int>> phaseToLanes = new Dictionary<int, List<int>>
    {
        { 1, new List<int> { 1, 2, 6, 7 } },
        { 2, new List<int> { 3, 4, 5 } },
        { 3, new List<int> { 8, 9 } }
    };

    private Dictionary<int, int> natezenieNasycenia = new Dictionary<int, int>
    {
        { 1, 1850 }, { 2, 1850 }, { 6, 1850 }, { 7, 1850 },
        { 3, 1700 }, { 4, 2000 }, { 5, 2000 },
        { 8, 1850 }, { 9, 1850 }
    };

    private void Start()
    {
        phaseDropdown.onValueChanged.AddListener(delegate { UpdateLanesUI(); });
        statisticsPanel.SetActive(isPanelVisible);
        StartCoroutine(UpdateUIRoutine());
    }

    public void ToggleStatisticsPanel()
    {
        isPanelVisible = !isPanelVisible;
        toggleButton.gameObject.SetActive(!isPanelVisible);
        statisticsPanel.SetActive(isPanelVisible);
        cameraPanel.SetActive(!isPanelVisible);
        hourPanel.SetActive(!isPanelVisible);
    }


    public static void RecordWaitingTime(float waitingTime)
    {
        waitingTimes.Add(waitingTime);
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
                EntryTrigger entry = FindEntryTrigger(lanes[i]);
                DensitySensor sensor = densitySensors[i];

                if (entry != null && sensor != null)
                {
                    float avgTime = entry.GetAverageTravelTime();
                    float natezenie = entry.count / (waitingTime / 3600f);
                    float przepustowosc = natezenieNasycenia.ContainsKey(lanes[i]) ? natezenieNasycenia[lanes[i]] * 0.4f : 0;
                    float stopienObciazenia = (przepustowosc > 0) ? natezenie / przepustowosc : 0;
                    float gestosc = sensor.GetDensity();
                    float avgWaitingTime = waitingTimes.Count > 0 ? waitingTimes.Average() : 0;

                    sredniaText[i].text = $"{avgTime:F2} s";
                    iloscText[i].text = $"{entry.count}";
                    nazwaPasaText[i].text = $"Pas: {lanes[i]}";
                    przepustowoscText[i].text = $"{przepustowosc:F0} poj/h";
                    obciazenieText[i].text = $"{stopienObciazenia:F2}";
                    gestoscText[i].text = $"{gestosc:F1} poj/km";
                    oczekiwanieText[i].text = $"{avgWaitingTime:F2} s";
                }
                else
                {
                    sredniaText[i].text = "Brak danych";
                    iloscText[i].text = "Brak danych";
                    nazwaPasaText[i].text = $"Pas: {lanes[i]}";
                    przepustowoscText[i].text = "Brak danych";
                    obciazenieText[i].text = "Brak danych";
                    gestoscText[i].text = "Brak danych";
                    oczekiwanieText[i].text = "Brak danych";
                }
            }
            else
            {
                sredniaText[i].text = "";
                iloscText[i].text = "";
                nazwaPasaText[i].text = "";
                przepustowoscText[i].text = "";
                obciazenieText[i].text = "";
                gestoscText[i].text = "";
                oczekiwanieText[i].text = "";
            }
        }
    }

    private EntryTrigger FindEntryTrigger(int laneID)
    {
        return entryTriggers.FirstOrDefault(entry => entry.name.Contains(laneID.ToString()));
    }
}
