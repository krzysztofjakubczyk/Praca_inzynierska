using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class CarController : MonoBehaviour
{
    private NavMeshAgent agent;

    [Header("Car Detection & Obstacle")]
    [SerializeField] private float detectionDistance;
    [SerializeField] private float stopDistance;
    [SerializeField] private float obstacleCheckRadius;
    private bool isAtMiddleOfIntersection = false; // NOWE: Czy auto faktycznie jest na środku?

    [Header("General")]
    public Waypoint CurrentWaypoint;
    public int FullSpeed;
    public int vehicleLength;

    public LineLightManager lineManager;    // Aktualny sygnalizator, pod który podlega auto

    public bool stopForCar = false;        // Czy musimy się zatrzymać z powodu innego auta
    public bool stopForLight = false;      // Czy musimy się zatrzymać z powodu czerwonego/żółtego światła
    private bool isAfterCar = false;        // Flaga używana przy detekcji innego samochodu
    public bool stopForCollision = false;        // Flaga używana przy detekcji innego samochodu
    private bool isLaneBlocked = false;        // Flaga używana przy detekcji innego samochodu
    private Waypoint previousWaypoint;


    private Vector3 lastPosition;
    private float stuckTimer;

    private Coroutine trafficLightCoroutine;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
        if (agent == null)
        {
            Debug.LogError($"🚗 {gameObject.name} nie ma przypisanego `NavMeshAgent`! Sprawdź prefab.");
            return;
        }

        StartCoroutine(moveCoroutine());
        StartCoroutine(CheckCarInFront());
    }

    private IEnumerator moveCoroutine()
    {
        // Czekaj, aż zostanie ustawiony pierwszy waypoint
        while (CurrentWaypoint == null)
        {
            yield return null;
        }

        while (true)
        {
            if (CurrentWaypoint != null) // Jeżeli pojazd ma gdzie zmierzać
            {
                if (CurrentWaypoint.name == "CarDestroyer")
                {
                    Destroy(gameObject, 0.5f);
                    //Jeżeli następnym punktem trasy jest niszczyciel- zniszcz pojazd
                }
                if (CurrentWaypoint.isFirstWaypoint && CurrentWaypoint.linkedController != null)
                {
                    lineManager = CurrentWaypoint.linkedController;
                    //Jeżeli pojazd jedzie w kierunku pierwszego punktu oraz nie ma przypisanego kontrolera swiateł- ustaw kontroler
                }

                //Metoda sprawdzająca czy przed pojazdem nie znjaduje się inny pojazd
                agent.isStopped = ShouldStop() ? true : false;
                //Zatrzymanie pojazdu zależy od wyniku metody ShouldStop
                if (!ShouldStop() && !agent.pathPending && agent.remainingDistance < 2f)
                {
                    //Jeżeli pojazd porusza się oraz najbliższy punkt jest mniej niż 2 jednostki
                    if (CurrentWaypoint.isBeforeTrafiicLight)
                    {
                        previousWaypoint = CurrentWaypoint;
                        agent.SetDestination(CurrentWaypoint.laneChooser.transform.position);
                    }
                    else
                    {
                        // Aktualizacja na następny waypoint
                        previousWaypoint = CurrentWaypoint; // Zapis poprzedniego waypointu
                        CurrentWaypoint = CurrentWaypoint.NextWaypoint; //Podmień obecny punkt na następny 
                        if (CurrentWaypoint != null)
                        agent.SetDestination((CurrentWaypoint.transform.position)); // Ustaw cel poruszania się 

                    }
                }

            }
            yield return null;
        }
    }
    private bool ShouldStop()
    {
        return stopForCar || stopForLight || stopForCollision || isLaneBlocked;
    }
    private IEnumerator CheckCarInFront()
    {
        while (true)
        {
            RaycastHit hit;
            Vector3 rayStartPosition = transform.position + Vector3.up * 0.5f; // Podniesienie Raycasta na wysokość maski auta
            Vector3 forwardDirection = transform.forward;

            stopForCar = false;

            if (Physics.Raycast(rayStartPosition, forwardDirection, out hit, detectionDistance))
            {
                if (hit.collider.CompareTag("Car"))
                {
                    isAfterCar = true;

                    if (hit.distance < stopDistance)
                    {
                        stopForCar = true; // Zatrzymaj pojazd, jeśli jest za blisko innego auta
                    }
                }
                else if (hit.collider.CompareTag("Obstacle"))
                {
                    // Obracanie się w stronę celu (jeśli tak ma działać logika)
                    Vector3 directionToTarget = (agent.destination - transform.position).normalized;
                    directionToTarget.y = 0;
                    transform.rotation = Quaternion.LookRotation(directionToTarget);
                    stopForCar = false;
                }
            }
            else
            {
                if (isAfterCar)
                {
                    isAfterCar = false; // Auto przed nami odjechało
                }
            }

            yield return new WaitForSeconds(0.3f); // Sprawdzaj co 0.5 sekundy
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
            // Jeśli wychodzimy ze strefy kolizyjnej, upewnij się, że przestajemy blokować
            stopForCollision = false;
        }
    }

    private IEnumerator CheckCollisionLaneState()
    {
        while (isAtMiddleOfIntersection)
        {
            // Auto jedzie prosto – nie musi czekać
            if (previousWaypoint.laneChooser != null && previousWaypoint.laneChooser.isDrivingStraight)
            {
                stopForCollision = false;
                break;
            }
            // Auto skręca w lewo – sprawdzamy, czy droga jest wolna
            else if (lineManager != null && lineManager.leftTurnAllowed)
            {
                stopForCollision = false;
                isAtMiddleOfIntersection = false; // Kończymy oczekiwanie
                break;
            }
            else
            {
                stopForCollision = true; // Musi czekać
            }

            yield return null;
        }
    }



    /// <summary>
    /// Korutyna ciągle monitorująca kolor światła i ustawiająca "stopForLight".
    /// </summary>
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
                        stopForLight = true;
                        break;

                    case TrafficLightColor.yellow:
                        stopForLight = true;
                        break;
                }
            }
            yield return null;
        }
    }

    /// <summary>
    /// Sprawdza, czy pojazd się nie "zawiesił" w miejscu(poza światłami i bez auta z przodu).
    /// Jeśli tak – usuwa go.
    /// </summary>
    //private IEnumerator CheckIfStuck()
    //{
    //    while (true)
    //    {
    //        Jeśli jedziemy albo jesteśmy tuż przy celu, reset timera
    //        if (agent.velocity.magnitude > 0.1f || agent.remainingDistance < 0.5f)
    //        {
    //            stuckTimer = 0f;
    //        }
    //        else
    //        {
    //            stuckTimer += 1f;
    //        }

    //        lastPosition = transform.position;

    //        Jeśli zbyt długo stoimy, a nie mamy czerwonego światła ani nie ma auta z przodu
    //        if (stuckTimer >= stuckTimeThreshold && !stopForLight && !isAfterCar)
    //        {
    //            lineManager.countOfVehicles--;
    //            foreach (var sensor in lineManager.sensors)
    //            {
    //                if (sensor.isForLeftTurnCheck)
    //                {
    //                    sensor.VehicleCount--;
    //                }
    //            }
    //            Destroy(gameObject, 0.5f);
    //            print("DESTROY STANIE W MIEJSCU");
    //        }

    //        Sprawdzenie, czy pojazd nie jest "w środku" przeszkody

    //       yield return new WaitForSeconds(1f);
    //    }
    //}


    public void SetFirstWaypoint(Waypoint waypoint)
    {
        if (waypoint == null)
        {
            Debug.LogError($"🚗 {gameObject.name} otrzymał `null` jako pierwszy waypoint! Sprawdź `VehicleSpawner`.");
            return;
        }

        CurrentWaypoint = waypoint;

        if (agent != null)
        {
            agent.SetDestination(CurrentWaypoint.transform.position);
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
        stopForLight = false;         // skoro wyjeżdżamy z obszaru sygnalizacji, nie musimy stać
    }
    private void OnDrawGizmos()
    {
        // Wizualizacja promienia do wykrywania samochodu z przodu
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.forward * detectionDistance);

        // Wizualizacja sfery do wykrywania przeszkód "od środka"
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, obstacleCheckRadius);
    }
    public void SetAgentDestination(Vector3 destinationTransform)
    {
        agent.SetDestination(destinationTransform);
    }
}