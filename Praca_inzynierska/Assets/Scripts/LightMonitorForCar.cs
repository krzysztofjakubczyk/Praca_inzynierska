using UnityEngine;

/// <summary>
/// Linia zatrzymania przed sygnalizatorem (cienki trigger). Pojazdy wykrywają ją z wyprzedzeniem
/// (CarController.Sense) i hamują płynnie modelem IDM. Opuszczenie triggera = pojazd przejechał linię.
/// </summary>
public class LightMonitorForCar : MonoBehaviour
{
    public bool wantWarning;

    [Tooltip("Opcjonalnie: sygnalizator tej linii. Puste = pojazd używa sygnalizatora ze swojej trasy " +
             "(waypoint z linkedController), a w ostateczności najbliższego waypointu z sygnalizatorem.")]
    [SerializeField] private LineLightManager controlledLane;

    private bool autoMapped;

    public LineLightManager ControlledLane
    {
        get
        {
            if (controlledLane == null && !autoMapped)
            {
                autoMapped = true;
                controlledLane = FindNearestLinkedLight(15f);
                if (wantWarning)
                    Debug.Log($"[Linia] {name}: przypisano sygnalizator {(controlledLane != null ? controlledLane.name : "brak")}.");
            }
            return controlledLane;
        }
    }

    private LineLightManager FindNearestLinkedLight(float maxDistance)
    {
        LineLightManager best = null;
        float bestDistance = maxDistance;
        foreach (Waypoint waypoint in FindObjectsOfType<Waypoint>())
        {
            if (waypoint.linkedController == null) continue;
            float distance = Vector3.Distance(waypoint.transform.position, transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = waypoint.linkedController;
            }
        }
        return best;
    }

    private void OnTriggerEnter(Collider other)
    {
        CarController carController = other.GetComponentInParent<CarController>();
        if (carController != null)
            carController.StartTrafficLightMonitoring();
    }

    private void OnTriggerExit(Collider other)
    {
        CarController carController = other.GetComponentInParent<CarController>();
        if (carController != null)
            carController.StopTrafficLightMonitoring();
    }
}
