using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatisticManager : MonoBehaviour
{
    [SerializeField] private float waitingTime;
    [SerializeField] List<EntryTrigger> entryTriggers;
    [SerializeField] List<ExitTrigger> exitTriggers;
    [SerializeField] TMP_Text gestoscText;
    [SerializeField] TMP_Text natezenieText;
    [SerializeField] TMP_Text sredniaText;
    [SerializeField] TMP_Text iloscText;
    [SerializeField] TMP_Text naczjestszyKierunekText;
    [SerializeField] GameObject choosedDayPanel;
    [SerializeField] private int count;
    [SerializeField] GameObject statisticButton;
    [SerializeField] GameObject cameraDropDownMenu;
    [SerializeField] TMP_Text activePhaseText;
    [SerializeField] private MainTrafficController MainController;
    [SerializeField] private MainTrafficControllerForSczanieckiej MainControllerForSczanieckiej;
    private bool isShown;
    [SerializeField] private GameObject panel;
    private void Start()
    {
        StartCoroutine(getCarCount());

    }

    private void Update()
    {
        iloscText.text = count.ToString();
        if(gameObject.name == "StatisticsManager")
        {
            activePhaseText.text = MainController.activePhase.ToString();
        }
        else if(gameObject.name == "StatisticsManagerSczanieckiej")
        {
            activePhaseText.text = MainControllerForSczanieckiej.activePhase.ToString();
        }
    }
    public void OnButtonClick()
    {
        if(isShown)
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
