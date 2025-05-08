using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum systemType { staticSystem, fuzzySystem };
public class MainTrafficController : MonoBehaviour
{
    [Header("List of lanes to concrete cycle")]
    [SerializeField] private List<PhaseSO> phasesInCycle = new List<PhaseSO>();

    private Dictionary<PhaseSO, int> carCountOnInlet = new Dictionary<PhaseSO, int>();
    private Dictionary<PhaseSO, float> carQueueOnInlet = new Dictionary<PhaseSO, float>();
    ITrafficTimingStrategy timingStrategy;

    [SerializeField] private float currentCycleTime = 0f; // Aktualny czas w cyklu

    [SerializeField] private float timeBeforeNextPhase = 2f;
    [SerializeField] private systemType currentChoosedSystemType;
    private int fullCycleTime = 120;
    private int periodOfTimeToCheckCountOfVehicles = 4;
    private int sample = 0;

    public int activePhase; //aktywna faza
    private void Start()
    {
        switch (currentChoosedSystemType)
        {
            case systemType.staticSystem:
                timingStrategy = new StaticInterfaceImplementation();

                break;
            case systemType.fuzzySystem:
                timingStrategy = new FuzzyInterfaceImplementation(phasesInCycle, carCountOnInlet, carQueueOnInlet, fullCycleTime);
                break;
            default:
                break;
        }

        foreach (var phaseInCycle in phasesInCycle)
        {
            carCountOnInlet.Add(phaseInCycle, 0);
            carQueueOnInlet.Add(phaseInCycle, 0);
            foreach (var lineInCycle in phaseInCycle.lanes)
            {
                lineInCycle.currentColor = TrafficLightColor.red;
            }
        }

        StartCoroutine(getVehicleCountOnEntrance());
        StartCoroutine(GlobalTrafficCycle());
    }

    //dane do logiki rozmytej

    private IEnumerator getVehicleCountOnEntrance()
    {
        Dictionary<PhaseSO, int> tempCarCount = new Dictionary<PhaseSO, int>(); //pomocnicze slowniki
        Dictionary<PhaseSO, float> tempQueueLength = new Dictionary<PhaseSO, float>();

        foreach (var phaseInCycle in phasesInCycle)
        {
            tempCarCount[phaseInCycle] = 0;
            tempQueueLength[phaseInCycle] = 0f;
        } //wypelnij pomocnicze slowniki zerami na poczatek

        while (true)
        {
            foreach (var phaseInCycle in phasesInCycle)
            {
                foreach (var laneInPhase in phaseInCycle.lanes)
                {
                    int countOfVehiclesOnLane = laneInPhase.countOfVehicles;
                    float queueLengthVehiclesOnLane = laneInPhase.queueLength;
                    tempCarCount[phaseInCycle] += countOfVehiclesOnLane;
                    tempQueueLength[phaseInCycle] += queueLengthVehiclesOnLane;
                }

            }
            sample++;
            if (sample >= periodOfTimeToCheckCountOfVehicles)
            {
                foreach (var phaseInCycle in phasesInCycle)
                {
                    carCountOnInlet[phaseInCycle] = tempCarCount[phaseInCycle] / sample;
                    carQueueOnInlet[phaseInCycle] = tempQueueLength[phaseInCycle] / sample;
                    tempCarCount[phaseInCycle] = 0;
                    tempQueueLength[phaseInCycle] = 0f;

                }
                sample = 0;
            }
            yield return new WaitForSeconds(fullCycleTime / periodOfTimeToCheckCountOfVehicles);
        }
    }
    private IEnumerator GlobalTrafficCycle()
    {
        while (true)
        {
            if(currentChoosedSystemType == systemType.fuzzySystem)
            {
                timingStrategy.AdjustGreenLightDurations();
            }
            for (int phaseIndex = 0; phaseIndex < phasesInCycle.Count; phaseIndex++)
            {
                var phase = phasesInCycle[phaseIndex];
                activePhase = phaseIndex;

                //dotyczy całej fazy 
                foreach (var lane in phase.lanes)
                {
                    double startLaneTime = lane.timeToStartGreenLight;
                    double greenLaneTime = lane.timeOfGreenLight;
                    StartCoroutine(HandleLaneCycle(lane, startLaneTime, greenLaneTime));
                }

            }
            yield return new WaitForSeconds(fullCycleTime);
        }
    }

    private IEnumerator HandleLaneCycle(LineLightManager line, double startTime, double greenTime)
    {
        while (currentCycleTime < startTime - line.timeOfBeforeYellowLight) // Czekamy na moment startu
        {
            yield return null;
        }

        int timeLeftInCycle = (int)(fullCycleTime - currentCycleTime);
        double timeInNextCycle = greenTime - timeLeftInCycle;
        line.ChangeColor(TrafficLightColor.yellow);
        yield return new WaitForSeconds((float)line.timeOfBeforeYellowLight);
        line.ChangeColor(TrafficLightColor.green);

        if (timeInNextCycle > 0)
        {
            yield return new WaitForSeconds(timeLeftInCycle);

            float waitTime = 0f;
            while (currentCycleTime >= fullCycleTime)
            {
                waitTime += Time.deltaTime;
                yield return null;
                if (waitTime > 2f)
                {
                    break;
                }
            }
            line.ChangeColor(TrafficLightColor.green);
            yield return new WaitForSeconds((float)timeInNextCycle);
        }
        else
        {
            yield return new WaitForSeconds((float)greenTime);
        }

        line.ChangeColor(TrafficLightColor.yellow);
        yield return new WaitForSeconds((float)line.timeOfAfterYellowLight);

        line.ChangeColor(TrafficLightColor.red);
    }


    private void Update()
    {
        currentCycleTime += Time.deltaTime;
        if (currentCycleTime >= fullCycleTime)
        {
            currentCycleTime = 0f; // Reset cyklu
        }
    }
}
