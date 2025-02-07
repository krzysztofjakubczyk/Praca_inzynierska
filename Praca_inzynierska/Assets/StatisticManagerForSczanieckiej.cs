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
    [SerializeField] private List<TMP_Text> sredniaText;
    [SerializeField] private List<TMP_Text> iloscText;
    [SerializeField] private List<TMP_Text> nazwaPasaText;
    [SerializeField] private TMP_Dropdown phaseDropdown;
    [SerializeField] private GameObject statisticsPanel; // Panel statystyk
    [SerializeField] private Button toggleButton; // Przycisk do otwierania/zamykania panelu

    private bool isPanelVisible = false; // Czy panel jest widoczny?

    private Dictionary<int, List<int>> phaseToLanes = new Dictionary<int, List<int>>
    {
        { 1, new List<int> { 1, 2, 6, 7 } },
        { 2, new List<int> { 3, 4, 5 } },
        { 3, new List<int> { 8, 9 } }
    };

    private void OnEnable()
    {
        ExitTrigger.OnVehicleExit += AddTravelTimeToEntry;
    }

    private void OnDisable()
    {
        ExitTrigger.OnVehicleExit -= AddTravelTimeToEntry;
    }

    private void Start()
    {
        phaseDropdown.onValueChanged.AddListener(delegate { UpdateLanesUI(); });
        toggleButton.onClick.AddListener(ToggleStatisticsPanel); // Obs³uga przycisku
        statisticsPanel.SetActive(isPanelVisible); // Ukryj panel na start
        StartCoroutine(UpdateUIRoutine());
    }

    private void ToggleStatisticsPanel()
    {
        isPanelVisible = !isPanelVisible;
        statisticsPanel.SetActive(isPanelVisible);
    }

    private IEnumerator UpdateUIRoutine()
    {
        while (true)
        {
            if (isPanelVisible) // Aktualizujemy tylko gdy panel jest widoczny
            {
                UpdateLanesUI();
            }
            yield return new WaitForSeconds(waitingTime);
        }
    }

    private void AddTravelTimeToEntry(string cameFrom, float travelTime)
    {
        EntryTrigger entry = entryTriggers.FirstOrDefault(e => e.gameObject.name == cameFrom);
        if (entry != null)
        {
            entry.AddTravelTime(travelTime);
        }
    }

    private void UpdateLanesUI()
    {
        if (!isPanelVisible) return; // Nie aktualizuj, jeœli panel jest zamkniêty

        int selectedPhase = phaseDropdown.value + 1;
        if (!phaseToLanes.ContainsKey(selectedPhase)) return;

        List<int> lanes = phaseToLanes[selectedPhase];

        for (int i = 0; i < sredniaText.Count; i++)
        {
            if (i < lanes.Count)
            {
                EntryTrigger entry = FindEntryTrigger(lanes[i]);

                if (entry != null)
                {
                    float avgTime = entry.GetAverageTravelTime();
                    sredniaText[i].text = $"Œredni czas: {avgTime:F2} s";
                    iloscText[i].text = $"Iloœæ pojazdów: {entry.count}";
                    nazwaPasaText[i].text = $"Pas: {lanes[i]}";
                }
                else
                {
                    sredniaText[i].text = "Brak danych";
                    iloscText[i].text = "Brak danych";
                    nazwaPasaText[i].text = $"Pas: {lanes[i]}";
                }
            }
            else
            {
                sredniaText[i].text = "";
                iloscText[i].text = "";
                nazwaPasaText[i].text = "";
            }
        }
    }

    private EntryTrigger FindEntryTrigger(int laneID)
    {
        return entryTriggers.FirstOrDefault(entry => entry.name.Contains(laneID.ToString()));
    }
}
