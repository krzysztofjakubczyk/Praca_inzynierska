using System.Collections;
using UnityEngine;

public class CarController : MonoBehaviour
{
    [Header("Car Detection & Obstacle")]
    [SerializeField] private float stopDistance = 5f;
    [SerializeField] private float obstacleCheckRadius = 1f;
    private bool isAtMiddleOfIntersection = false;

    [Header("General")]
    public Waypoint[] waypoints;
    public float speed = 5f;
    public float rotationSpeed = 5f;
    public float CurrentSpeed;
    public int vehicleLength;

    public LineLightManager lineManager;

    public bool stopForLight = false;
    public bool isAfterCar = false;
    public bool stopForCollision = false;
    private bool isLaneBlocked = false;
    public bool hadLineManager = false;
    private Waypoint previousWaypoint;

    private Coroutine trafficLightCoroutine;

    public int currentWaypointIndex = 0;
    private Waypoint CurrentWaypoint => waypoints[currentWaypointIndex];

    void Start()
    {
        StartCoroutine(MoveToWaypoints());
        StartCoroutine(CheckCarInFront());
    }

    private IEnumerator MoveToWaypoints()
    {
        while (waypoints == null || waypoints.Length == 0)
            yield return null;

        while (true)
        {
            if (hadLineManager == false && currentWaypointIndex == 0)
            {
                lineManager = CurrentWaypoint.linkedController;
                hadLineManager = true;
            }
            if (stopForLight || isAfterCar || stopForCollision || isLaneBlocked)
            {
                CurrentSpeed = 0;
                yield return null;
                continue;
            }

            Vector3 targetPos = CurrentWaypoint.transform.position;
            Vector3 direction = (targetPos - transform.position).normalized;
            direction.y = 0;

            // Ruch
            transform.position += direction * speed * Time.deltaTime;
            CurrentSpeed = speed;

            // Obrót
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            // Jeśli blisko waypointa – przejdź do kolejnego
            if (Vector3.Distance(transform.position, targetPos) < 2f)
            {
                if (CurrentWaypoint.isBeforeTrafiicLight)
                {
                    previousWaypoint = CurrentWaypoint;
                }

                if (currentWaypointIndex < waypoints.Length - 1)
                {
                    previousWaypoint = CurrentWaypoint;
                    currentWaypointIndex++;
                }
            }

            yield return null;
        }
    }


    private IEnumerator CheckCarInFront()
    {
        while (true)
        {
            isAfterCar = false;
            RaycastHit hit;
            Vector3 rayStartPosition = transform.position + Vector3.up * 1f;
            Vector3 forwardDirection = transform.forward;

            if (Physics.Raycast(rayStartPosition, forwardDirection, out hit, stopDistance))
            {
                if (hit.collider.CompareTag("Car"))
                {
                    isAfterCar = true;
                }
            }

            yield return null;
        }
    }

    public void SetMiddleIntersectionState(bool state)
    {
        isAtMiddleOfIntersection = state;

        if (isAtMiddleOfIntersection)
        {
            StartCoroutine(CheckCollisionLaneState());
        }
        else
        {
            stopForCollision = false;
        }
    }

    private IEnumerator CheckCollisionLaneState()
    {
        while (isAtMiddleOfIntersection)
        {
            if (previousWaypoint.laneChooser != null && previousWaypoint.laneChooser.isDrivingStraight)
            {
                stopForCollision = false;
                break;
            }
            else if (lineManager != null && lineManager.leftTurnAllowed)
            {
                stopForCollision = false;
                isAtMiddleOfIntersection = false;
                break;
            }
            else
            {
                stopForCollision = true;
            }

            yield return null;
        }
    }

    private IEnumerator monitorTrafficLightForVehicle()
    {
        while (true)
        {
            if (lineManager != null)
            {
                isLaneBlocked = lineManager.isBlocked;

                switch (lineManager.currentColor)
                {
                    case TrafficLightColor.green:
                        stopForLight = false;
                        break;
                    case TrafficLightColor.red:
                    case TrafficLightColor.yellow:
                        stopForLight = true;
                        break;
                }
            }
            yield return null;
        }
    }

    public void SetFirstWaypoint(Waypoint[] waypoint)
    {
        if (waypoint == null || waypoint.Length == 0)
        {
            Debug.LogError($"🚗 {gameObject.name} otrzymał `null` jako pierwszy waypoint! Sprawdź `VehicleSpawner`.");
            return;
        }

        waypoints = waypoint;
        currentWaypointIndex = 0;
    }

    public void StartTrafficLightMonitoring()
    {
        if (trafficLightCoroutine == null)
        {
            trafficLightCoroutine = StartCoroutine(monitorTrafficLightForVehicle());
        }
    }

    public void StopTrafficLightMonitoring()
    {
        if (trafficLightCoroutine != null)
        {
            StopCoroutine(trafficLightCoroutine);
            trafficLightCoroutine = null;
        }
        stopForLight = false;
    }

    private void OnDrawGizmos()
    {
        Debug.DrawRay(transform.position, transform.forward * stopDistance, Color.green);
    }
}
