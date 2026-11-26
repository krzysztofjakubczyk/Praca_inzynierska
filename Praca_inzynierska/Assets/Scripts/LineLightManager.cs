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
    private bool hasAppliedColor;
    private TrafficLightColor appliedColor;

    [Header("Timings")]
    public double timeToStartGreenLight;
    public double timeOfGreenLight;
    public double timeOfBeforeYellowLight;
    public double timeOfAfterYellowLight;

    [Header("Flags")]
    public bool isBlocked = false;        // Czy pas jest zablokowany przez zator za skrzyżowaniem?
    public bool leftTurnAllowed = false;  // Czy można bezpiecznie skręcić w lewo?
    public bool hasToWait;                // Flaga oczekiwania na skrzyżowaniu

    /// <summary>Kolor przed ostatnią zmianą – pozwala odróżnić żółte po zielonym od czerwono-żółtego.</summary>
    public TrafficLightColor PreviousColor { get; private set; } = TrafficLightColor.red;

    /// <summary>Żółte po zielonym: kierowca, który nie zdąży bezpiecznie wyhamować, może przejechać.</summary>
    public bool IsAmberAfterGreen => currentColor == TrafficLightColor.yellow && PreviousColor == TrafficLightColor.green;

    private void Start()
    {
        if (planeToShowCurrentColor != null)
            meshRenderer = planeToShowCurrentColor.GetComponent<MeshRenderer>();

        StartCoroutine(UpdateTrafficStateMaster());
    }

    private IEnumerator UpdateTrafficStateMaster()
    {
        WaitForSeconds wait = new WaitForSeconds(0.2f); // Odświeżanie 5 razy na sekundę
        while (true)
        {
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

                // 2. Sensor zjazdowy za skrzyżowaniem (zator na wyjeździe)
                if (sensor.isForCollision && sensor.isBlockingTraffic)
                {
                    shouldBlock = true;
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

            if (hasCollisionLine)
            {
                bool isRedOrYellow = currentColor == TrafficLightColor.red || currentColor == TrafficLightColor.yellow;

                if (sensorToHide != null)
                {
                    sensorToHide.gameObject.SetActive(!isRedOrYellow);
                    if (isRedOrYellow) sensorToHide.ResetSensor();
                }

                if (laneChooserToHide != null)
                {
                    foreach (GameObject lanechooser in laneChooserToHide)
                    {
                        if (lanechooser != null) lanechooser.SetActive(!isRedOrYellow);
                    }
                }
            }
            leftTurnAllowed = !leftTurnBlockedBySensor;

            yield return wait;
        }
    }

    private void Update()
    {
        // Materiał zmieniamy tylko przy zmianie koloru (wcześniej co klatkę).
        if (meshRenderer == null || (hasAppliedColor && appliedColor == currentColor)) return;

        switch (currentColor)
        {
            case TrafficLightColor.green: meshRenderer.material.color = Color.green; break;
            case TrafficLightColor.yellow: meshRenderer.material.color = Color.yellow; break;
            case TrafficLightColor.red: meshRenderer.material.color = Color.red; break;
        }
        appliedColor = currentColor;
        hasAppliedColor = true;
    }

    public void ChangeColor(TrafficLightColor color)
    {
        if (color == currentColor) return;
        PreviousColor = currentColor;
        currentColor = color;
    }
}
