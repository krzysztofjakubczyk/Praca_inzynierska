using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;


public class CarController : MonoBehaviour
{
    private NavMeshAgent agent;
    public LineLightManager lineManager;
    [SerializeField] float detectionDistance;
    [SerializeField] float stopDistance;
    [SerializeField] float detectionInterval;
    public float vehicleLength;
    public bool WantWarnings;
    public Waypoint CurrentWaypoint;  // Aktualny waypoint, na który pojazd zmierza
    public int FullSpeed;
    void Start()
    {
        // Pobranie komponentu NavMeshAgent
        agent = GetComponent<NavMeshAgent>();
        Rigidbody rb = GetComponent<Rigidbody>();

        rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;

        StartCoroutine(DetectCarsCoroutine());
        StartCoroutine(MoveCoroutine());
    }

    private IEnumerator MoveCoroutine()
    {
        while (CurrentWaypoint == null)
        {
            if (WantWarnings)
            {
                Debug.LogWarning("Waiting to have currentWaypoint...");
            }
            yield return null;
        }

        while (true)
        {
            if (CurrentWaypoint.isFirstWaypoint)
            {
                lineManager = CurrentWaypoint.linkedController;
            }
            if (!agent.pathPending && agent.remainingDistance < 2f  )
            {
                if (CurrentWaypoint.isBeforeTrafiicLight)
                {
                    agent.SetDestination(CurrentWaypoint.laneChooser.transform.position);
                }
                else
                {
                    CurrentWaypoint = CurrentWaypoint.NextWaypoint;
                    agent.SetDestination(CurrentWaypoint.transform.position);
                }
            }

            yield return null;
        }
    }

    public IEnumerator MonitorTrafficLightForVehicle()
    {
        while (true)
        {
            if (lineManager != null)
            {
                var currentColor = lineManager.currentColor;
                switch (currentColor)
                {
                    case TrafficLightColor.green:
                        agent.speed = FullSpeed; // Pe³na prêdkoœæ
                        break;
                    case TrafficLightColor.red:
                        agent.speed = 0f; // Zatrzymanie pojazdu
                        break;
                    case TrafficLightColor.yellow:
                        agent.speed = FullSpeed; // Zwolnienie przed œwiat³em
                        break;
                }
            }
            yield return new WaitForSeconds(1f); // Regularne sprawdzanie stanu sygnalizacji
        }
    }

    public void SetFirstWaypoint(Waypoint waypoint)
    {
        if (waypoint != null)
        {
            CurrentWaypoint = waypoint;
           
        }
        else
        {
            Debug.LogWarning("Przekazany waypoint jest null!");
        }
    }

    private Coroutine trafficLightCoroutine; // Przechowuje referencjê do uruchomionej korutyny

    public void StartTrafficLightMonitoring()
    {
        if (trafficLightCoroutine == null)
        {
            trafficLightCoroutine = StartCoroutine(MonitorTrafficLightForVehicle());
        }
    }

    public void StopTrafficLightMonitoring()
    {
        if (trafficLightCoroutine != null)
        {
            StopCoroutine(trafficLightCoroutine);
            trafficLightCoroutine = null; // Resetuj referencjê
        }

        // Przywróæ pe³n¹ prêdkoœæ, gdy opuszczamy trigger
        agent.speed = FullSpeed;
    }

    private IEnumerator DetectCarsCoroutine()
    {
        while (true)
        {
            DetectCars();
            yield return new WaitForSeconds(detectionInterval); // Czekaj przez okreœlony czas
        }
    }

    private void DetectCars()
    {
        RaycastHit hit;
        // Ustawienie wektora kierunku do przodu
        Vector3 rayStartPosition = transform.position + Vector3.up;
        Vector3 forwardDirection = transform.TransformDirection(Vector3.forward) * detectionDistance;

        // Sprawdzenie, czy raycast trafia w inne auto
        if (Physics.Raycast(rayStartPosition, forwardDirection, out hit, detectionDistance))
        {
            
            if (hit.collider.CompareTag("Car")) // Zak³adamy, ¿e inne samochody maj¹ tag "Car"
            {
                // Jeœli wykryto inne auto, zatrzymaj siê, jeœli jest za blisko
                if (hit.distance < stopDistance)
                {
                    agent.isStopped = true;
                    agent.velocity = Vector3.zero;
                }
                else if (hit.distance < stopDistance + 2f) // Minimalny odstêp
                {
                    agent.speed = FullSpeed * 0.5f; // Zwolnij, ale nie zatrzymuj ca³kowicie
                }
                else
                {
                    agent.isStopped = false;
                    agent.speed = FullSpeed; // Przywróæ pe³n¹ prêdkoœæ
                }
            }
        }
        else
        {
            // Nie wykryto przeszkód, kontynuuj jazdê
            agent.isStopped = false;
        }
    }
    public void SetAgentDestination(Vector3 destinationTransform)
    {
        agent.SetDestination(destinationTransform); 
    }
    private void OnDrawGizmos()
    {
        Vector3 rayStartPosition = transform.position + Vector3.up;

        Vector3 forwardDirection = transform.TransformDirection(Vector3.forward) * detectionDistance;
        Debug.DrawRay(rayStartPosition, forwardDirection, Color.green); // Rysuje czerwony promieñ
    }
}