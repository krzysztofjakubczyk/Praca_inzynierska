using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TrafficLightColor
{
    red,
    yellow,
    green,
}

public class LineLightManager : MonoBehaviour
{
    [SerializeField] public TrafficLightColor currentColor;
    [SerializeField] public int countOfVehicles;
    [SerializeField] public float queueLength;
    [SerializeField] public int idAtDraw;
    [SerializeField] bool hasCollisionLine;
    [SerializeField] Sensor sensorToHide;
    [SerializeField] GameObject[] laneChooserToHide;
    [SerializeField] ChooseLane laneChooserToCollision;
    public List<Sensor> sensors;
    public double timeToStartGreenLight;
    public double timeOfGreenLight;
    public double timeOfBeforeYellowLight;
    public double timeOfAfterYellowLight;
    public bool isBlocked = false; // Czy pas jest zablokowany?
    public bool leftTurnAllowed = false; // Czy można skręcać w lewo?
    public bool leftTurnWaiting = false; // Czy są auta na środku skrzyżowania?
    public bool hasToWait; // Czy są auta na środku skrzyżowania?

    private void Start()
    {
        if (hasCollisionLine)
        {
            StartCoroutine(UpdateTrafficStateRoutine());
        }
        StartCoroutine(UpdateVehicleData());

    }
    private IEnumerator UpdateVehicleData()
    {
        while (true)
        {
            int totalVehicles = 0;
            float totalQueueLength = 0f;

            foreach (var sensor in sensors)
            {
                totalVehicles += sensor.VehicleCount;
                totalQueueLength += sensor.QueueLength;
            }

            countOfVehicles = totalVehicles;
            queueLength = totalQueueLength;


            yield return new WaitForSeconds(5f); // Aktualizacja co 5 sekund
        }
    }

    private IEnumerator UpdateTrafficStateRoutine()
    {
        while (true)
        {
            UpdateTrafficState();
            if(currentColor == TrafficLightColor.red)
            {
                sensorToHide.gameObject.SetActive(false);
                sensorToHide.VehicleCount = 0;
                sensorToHide.QueueLength = 0;
                foreach (GameObject lanechooser in laneChooserToHide)
                {
                    lanechooser.SetActive(false);
                    
                }
            }
            else if(currentColor == TrafficLightColor.green)
            {
                sensorToHide.gameObject.SetActive(true);
                foreach (GameObject lanechooser in laneChooserToHide)
                {
                    lanechooser.SetActive(true);
                }
            }
            if (hasToWait && laneChooserToCollision.isDrivingStraight)
            {
               leftTurnAllowed = false; // Zatrzymujemy ruch na przeciwnym pasie
            }
            yield return new WaitForSeconds(0.5f); // Sprawdzamy co 3 sekundy
        }
    }

    public void UpdateTrafficState()
    {   
        //countOfVehicles = 0;
        //queueLength = 0;
        bool shouldBlock = false;
        bool leftTurnBlocked = false;
        leftTurnWaiting = false; // Resetujemy flagę

        foreach (var sensor in sensors)
        {
            if (sensor.isForCollision)
            {
                if (sensor.isBlockingTraffic)
                {
                    shouldBlock = true; // Jeśli chociaż jeden sensor zgłasza blokadę → blokujemy pas
                }
            }
            else if (sensor.isForLeftTurnCheck)
            {
                if (sensor.VehicleCount > 0)
                {
                    leftTurnBlocked = true; // Jeśli na wprost są auta, skręt w lewo jest zablokowany
                }
            }

            
        }

        // PRZENIESIONE: Aktualizujemy PO pętli, nie w trakcie!
        leftTurnAllowed = !leftTurnBlocked; // Jeśli NIE ma aut na wprost, można skręcać w lewo

        if (shouldBlock)
        {
            isBlocked = true;
        }
        else
        {
            isBlocked = false;
        }


    }

    public void ChangeColor(TrafficLightColor color)
    {
        currentColor = color;
    }
}
