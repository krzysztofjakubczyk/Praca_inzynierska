using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;


public class CarController : MonoBehaviour
{
    private NavMeshAgent agent;
    public LocalTrafficController controller;
    [SerializeField] float detectionDistance = 10f;
    [SerializeField] float stopDistance = 8f;
    [SerializeField] float detectionInterval = 0.5f;
    public Waypoint currentWaypoint;  // Aktualny waypoint, na który pojazd zmierza
    void Start()
    {
        // Pobranie komponentu NavMeshAgent
        agent = GetComponent<NavMeshAgent>();

        // Ustawienie celu jako miejsce, do którego pojazd ma siê udaæ
        Transform nearestParkingSpot = FindLineChooser(agent.transform);
        SetAgentDestination(nearestParkingSpot.position);


        StartCoroutine(DetectCarsCoroutine());
        StartCoroutine(MoveCoroutine());
    }

    private IEnumerator MoveCoroutine()
    {
        while (currentWaypoint == null)
        {
            Debug.LogWarning("Waiting to have currentWaypoint...");
            yield return null;
        }

        while (true)
        {
            if (currentWaypoint.isFirstWaypoint)
            {
                controller = currentWaypoint.linkedController;
            }
            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                currentWaypoint = currentWaypoint.NextWaypoint;
                agent.SetDestination(currentWaypoint.transform.position);
            }

            yield return null;
        }
    }

    public IEnumerator MonitorTrafficLight()
    {
        while (true)
        {
            if (controller != null)
            {
                switch (controller.currentLight)
                {
                    case TrafficLightColor.Green:
                        agent.speed = 3f; // Pe³na prêdkoœæ
                        break;
                    case TrafficLightColor.Red:
                        agent.speed = 0f; // Zatrzymanie pojazdu
                        break;
                    case TrafficLightColor.Yellow:
                        agent.speed = 1.5f; // Zwolnienie przed œwiat³em
                        break;
                }
            }
            print("korutyna dizala" + name);
            yield return new WaitForSeconds(1f); // Regularne sprawdzanie stanu sygnalizacji
        }
    }

    private Transform FindLineChooser(Transform carTransform)
    {
        GameObject[] parkingSpots = GameObject.FindGameObjectsWithTag("StopPoint");

        Transform nearestSpot = null;
        float nearestDistance = Mathf.Infinity;

        foreach (GameObject spot in parkingSpots)
        {
            float distance = Vector3.Distance(carTransform.position, spot.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestSpot = spot.transform;
            }
        }

        return nearestSpot;
    }

    private Coroutine trafficLightCoroutine; // Przechowuje referencjê do uruchomionej korutyny

    public void StartTrafficLightMonitoring()
    {
        if (trafficLightCoroutine == null)
        {
            trafficLightCoroutine = StartCoroutine(MonitorTrafficLight());
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
        agent.speed = 3f;
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
        Vector3 rayStartPosition = transform.position + Vector3.up * 2f;
        Vector3 forwardDirection = transform.TransformDirection(Vector3.forward) * detectionDistance;

        // Sprawdzenie, czy raycast trafia w inne auto
        if (Physics.Raycast(rayStartPosition, forwardDirection, out hit, detectionDistance))
        {
            print(hit.collider.name + " + " + hit.distance);
            if (hit.collider.CompareTag("Car")) // Zak³adamy, ¿e inne samochody maj¹ tag "Car"
            {
                // Jeœli wykryto inne auto, zatrzymaj siê, jeœli jest za blisko
                if (hit.distance < stopDistance)
                {
                    print("Zatrzymano autko");
                    agent.isStopped = true; // Zatrzymaj pojazd
                }
                else
                {
                    agent.isStopped = false; // Wznów jazdê
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
        Vector3 rayStartPosition = transform.position + Vector3.up * 2f;

        Vector3 forwardDirection = transform.TransformDirection(Vector3.forward) * detectionDistance;
        Debug.DrawRay(rayStartPosition, forwardDirection, Color.green); // Rysuje czerwony promieñ
    }
}