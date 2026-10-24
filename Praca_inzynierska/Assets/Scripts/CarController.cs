using System.Collections;
using UnityEngine;

public class CarController : MonoBehaviour
{
    [Header("Car Detection & Obstacle")]
    [SerializeField] private float stopDistance = 5f;
    [SerializeField] private float obstacleCheckRadius = 0.8f;
    private bool isAtMiddleOfIntersection = false;

    [Header("General")]
    public Waypoint[] waypoints;
    public float speed = 5f;
    public float rotationSpeed = 5f;
    public float CurrentSpeed;
    public int vehicleLength = 4;

    [Header("Intersection & Lights")]
    public LineLightManager lineManager;
    public bool isTurningLeft = false;

    [Header("Status Flags")]
    public bool stopForLight = false;
    public bool isAfterCar = false;
    public bool stopForCollision = false;
    public bool isLaneBlocked = false;
    private Waypoint previousWaypoint;

    private Coroutine trafficLightCoroutine;

    public int currentWaypointIndex = 0;
    private Waypoint CurrentWaypoint => (waypoints != null && currentWaypointIndex < waypoints.Length) ? waypoints[currentWaypointIndex] : null;

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
            if (CurrentWaypoint == null)
            {
                yield return null;
                continue;
            }

            if (CurrentWaypoint.linkedController != null)
            {
                lineManager = CurrentWaypoint.linkedController;
            }

            // POPRAWKA SKRĘTU W LEWO:
            // Auto czeka przed skrzyżowaniem jeśli skręca w lewo, linia zabrania skrętu (!leftTurnAllowed)
            // ORAZ auto jeszcze nie minęło skrzyżowania (nie wjechało na waypoint za światłami).
            bool waitingForLeftTurnBeforeIntersection = false;
            if (isTurningLeft && lineManager != null && !lineManager.leftTurnAllowed)
            {
                    waitingForLeftTurnBeforeIntersection = true;
               
            }

            bool mustStop = stopForLight || isAfterCar || stopForCollision || isLaneBlocked || waitingForLeftTurnBeforeIntersection;
            float targetSpeed = mustStop ? 0f : speed;

            float accelRate = mustStop ? 15f : 5f;
            CurrentSpeed = Mathf.MoveTowards(CurrentSpeed, targetSpeed, accelRate * Time.deltaTime);

            if (CurrentSpeed > 0.001f)
            {
                Vector3 targetPos = CurrentWaypoint.transform.position;
                Vector3 direction = (targetPos - transform.position).normalized;
                direction.y = 0;

                transform.position += direction * CurrentSpeed * Time.deltaTime;

                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }

                Vector3 toWaypoint = targetPos - transform.position;
                float distance = toWaypoint.magnitude;
                float forwardDot = Vector3.Dot(transform.forward, toWaypoint.normalized);

                if (distance < 2.5f || forwardDot < 0f)
                {
                    if (CurrentWaypoint.isBeforeTrafiicLight)
                    {
                        previousWaypoint = CurrentWaypoint;
                    }

                    // Wyłączenie statusu skrętu po minięciu skrzyżowania
                    if (CurrentWaypoint.isAfterTrafiicLight)
                    {
                        isTurningLeft = false;
                    }

                    if (currentWaypointIndex < waypoints.Length - 1)
                    {
                        previousWaypoint = CurrentWaypoint;
                        currentWaypointIndex++;
                    }
                }
            }

            yield return null;
        }
    }

    private IEnumerator CheckCarInFront()
    {
        WaitForSeconds wait = new WaitForSeconds(0.05f);

        while (true)
        {
            isAfterCar = false;
            Vector3 rayStartPosition = transform.position + transform.forward * (vehicleLength * 0.5f) + Vector3.up * 0.5f;

            RaycastHit[] hits = Physics.SphereCastAll(rayStartPosition, obstacleCheckRadius, transform.forward, stopDistance);

            foreach (var hit in hits)
            {
                if (hit.collider.transform.root == transform.root) continue;

                if (hit.collider.CompareTag("Car") || hit.collider.CompareTag("Bus"))
                {
                    isAfterCar = true;
                    break;
                }
            }

            yield return wait;
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
            if (previousWaypoint != null && previousWaypoint.laneChooser != null && previousWaypoint.laneChooser.isDrivingStraight)
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

    public void SetFirstWaypoint(Waypoint[] newWaypoints)
    {
        if (newWaypoints == null || newWaypoints.Length == 0)
        {
            Debug.LogError($"🚗 {gameObject.name} otrzymał pustą tablicę waypointów!");
            return;
        }

        waypoints = newWaypoints;

        int closestIndex = 0;
        float minDistance = float.MaxValue;

        for (int i = 0; i < newWaypoints.Length; i++)
        {
            if (newWaypoints[i] == null) continue;

            float dist = Vector3.Distance(transform.position, newWaypoints[i].transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closestIndex = i;
            }
        }

        currentWaypointIndex = closestIndex;

        if (CurrentWaypoint != null && CurrentWaypoint.linkedController != null)
        {
            lineManager = CurrentWaypoint.linkedController;
        }
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
        Vector3 rayStartPosition = transform.position + transform.forward * (vehicleLength * 0.5f) + Vector3.up * 0.5f;

        bool waitingForLeftTurnBeforeIntersection = (CurrentWaypoint != null && CurrentWaypoint.isBeforeTrafiicLight && isTurningLeft && lineManager != null && !lineManager.leftTurnAllowed);
        bool mustStop = stopForLight || isAfterCar || stopForCollision || isLaneBlocked || waitingForLeftTurnBeforeIntersection;

        Gizmos.color = mustStop ? Color.red : Color.green;

        Gizmos.DrawWireSphere(rayStartPosition + transform.forward * stopDistance, obstacleCheckRadius);
        Debug.DrawRay(rayStartPosition, transform.forward * stopDistance, Gizmos.color);
    }
}