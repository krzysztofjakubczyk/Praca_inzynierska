using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class StatisticManagerForSczanieckiej : MonoBehaviour
{
    [SerializeField] private float waitingTime;
    [SerializeField] List<EntryTrigger> entryTriggers;
    [SerializeField] List<Counter> counters;
    [SerializeField] List<ExitTrigger> exitTriggers;
    [SerializeField] List<TMP_Text> gestoscText;
    [SerializeField] List<TMP_Text> natezenieText;
    [SerializeField] List<TMP_Text> sredniaText;
    [SerializeField] List<TMP_Text> iloscText;
    [SerializeField] TMP_Text naczjestszyKierunekText;
    [SerializeField] GameObject choosedDayPanel;
    [SerializeField] private int count;
    [SerializeField] GameObject statisticButton;
    [SerializeField] GameObject cameraDropDownMenu;
    [SerializeField] TMP_Text activePhaseText;
    [SerializeField] private MainTrafficControllerForSczanieckiej MainControllerForSczanieckiej;
    private bool isShown;
    [SerializeField] private GameObject panel;
    private void Start()
    {
        StartCoroutine(getCarCount());

    }

    private void Update()
    {
        //iloscText.text = count.ToString();
        //naczjestszyKierunekText.text = GetMostFrequentExit();
    }
    private string GetMostFrequentExit()
    {
        if (counters.Count == 0) return "Brak danych";

        var mostFrequent = counters.OrderByDescending(c => c.countOfVehicles).FirstOrDefault();
        if (mostFrequent == null || mostFrequent.countOfVehicles == 0) return "Brak ruchu";

        return $"{mostFrequent.name} ({mostFrequent.countOfVehicles} aut)";
    }
    public void OnButtonClick()
    {
        if (isShown)
        {
            isShown = false;
        }
        else
        {
            isShown = true;
        }
        cameraDropDownMenu.SetActive(!isShown);
        statisticButton.SetActive(!isShown);
        choosedDayPanel.SetActive(!isShown);
        panel.SetActive(isShown);

    }


    private IEnumerator getCarCount()
    {
        while (true)
        {
            foreach (EntryTrigger trigger in entryTriggers)
            {
                count += trigger.count;
                trigger.count = 0;
            }
            yield return new WaitForSeconds(waitingTime);
        }
    }
}


