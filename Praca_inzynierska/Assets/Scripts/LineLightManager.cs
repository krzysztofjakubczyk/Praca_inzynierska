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
    [Header("Current State")]
    [SerializeField] public TrafficLightColor currentColor;
    [SerializeField] public int countOfVehicles;
    [SerializeField] public float queueLength;
    [SerializeField] public int idAtDraw;

    [Header("Collision & Left Turn Settings")]
    [SerializeField] bool hasCollisionLine;
    [SerializeField] Sensor sensorToHide;
    [SerializeField] GameObject[] laneChooserToHide;
    [SerializeField] GameObject planeToShowCurrentColor;
    [SerializeField] ChooseLane laneChooserToCollision;

    public List<Sensor> sensors = new List<Sensor>();
    private MeshRenderer meshRenderer;

    [Header("Timings")]
    public double timeToStartGreenLight;
    public double timeOfGreenLight;
    public double timeOfBeforeYellowLight;
    public double timeOfAfterYellowLight;

    [Header("Flags")]
    public bool isBlocked = false;        // Czy pas jest zablokowany przez zator?
    public bool leftTurnAllowed = false; // Czy można bezpiecznie skręcić w lewo?
    public bool hasToWait;                // Flaga oczekiwania na skrzyżowaniu

    private void Start()
    {
        if (planeToShowCurrentColor != null)
        {
            meshRenderer = planeToShowCurrentColor.GetComponent<MeshRenderer>();
        }

        // Uruchamiamy JEDNĄ, ujednoliconą pętlę zarządzania stanem
        StartCoroutine(UpdateTrafficStateMaster());
    }

    private IEnumerator UpdateTrafficStateMaster()
    {
        while (true)
        {
            // 1. Zliczanie pojazdów i długości kolejek
            int totalVehicles = 0;
            float totalQueueLength = 0f;

            bool shouldBlock = false;
            bool leftTurnBlockedBySensor = false;

            foreach (var sensor in sensors)
            {
                if (sensor == null) continue;

                // 1. Zwykły sensor dojazdowy przed światłami (zlicza statystyki)
                if (!sensor.isForCollision && !sensor.isForLeftTurnCheck)
                {
                    totalVehicles += sensor.VehicleCount;
                    totalQueueLength += sensor.QueueLength;
                }

                // 2. Sensor ZJAZDOWY ZA skrzyżowaniem (wykrywa realny zator na wyjeździe)
                if (sensor.isForCollision && sensor.isBlockingTraffic)
                {
                    shouldBlock = true; // Zator ZA skrzyżowaniem -> zatrzymaj wjazd!
                }

                // 3. Sensor z naprzeciwka do skrętu w lewo
                else if (sensor.isForLeftTurnCheck && sensor.HasStraightDrivingVehicles())
                {
                    leftTurnBlockedBySensor = true;
                }
            }

            countOfVehicles = totalVehicles;
            queueLength = totalQueueLength;
            isBlocked = shouldBlock;

            // 2. Obsługa ukrywania sensorów / wyznaczania pasów na czerwonym świetle
            if (hasCollisionLine)
            {
                bool isRedOrYellow = (currentColor == TrafficLightColor.red || currentColor == TrafficLightColor.yellow);

                if (sensorToHide != null)
                {
                    sensorToHide.gameObject.SetActive(!isRedOrYellow);
                    if (isRedOrYellow)
                    {
                        sensorToHide.ResetSensor();
                    }
                }

                if (laneChooserToHide != null)
                {
                    foreach (GameObject lanechooser in laneChooserToHide)
                    {
                        if (lanechooser != null)
                        {
                            lanechooser.SetActive(!isRedOrYellow);
                        }
                    }
                }
            }

            // 3. SPÓJNE WYLICZENIE leftTurnAllowed (połączenie obu warunków)
            bool isBlockedByCollisionLine = (hasCollisionLine && hasToWait && laneChooserToCollision != null && laneChooserToCollision.isDrivingStraight);

            // Skręt w lewo jest dozwolony TYLKO wtedy, gdy żaden z warunków go nie blokuje
            leftTurnAllowed = (!leftTurnBlockedBySensor && !isBlockedByCollisionLine);

            yield return new WaitForSeconds(0.2f); // Odświeżanie 5 razy na sekundę
        }
    }

    private void Update()
    {
        if (meshRenderer != null)
        {
            switch (currentColor)
            {
                case TrafficLightColor.green:
                    meshRenderer.material.color = Color.green;
                    break;
                case TrafficLightColor.yellow:
                    meshRenderer.material.color = Color.yellow;
                    break;
                case TrafficLightColor.red:
                    meshRenderer.material.color = Color.red;
                    break;
            }
        }
    }

    public void ChangeColor(TrafficLightColor color)
    {
        currentColor = color;
    }
}