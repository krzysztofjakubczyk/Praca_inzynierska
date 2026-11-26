using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ruch pojazdu oparty o model IDM (Intelligent Driver Model, Treiber i in. 2000).
///
/// Każdy powód zatrzymania jest traktowany jak przeszkoda w odległości "gap" poruszająca się z prędkością
/// "obstacleSpeed": pojazd z przodu, linia zatrzymania przy czerwonym/żółtym, zablokowany wyjazd,
/// punkt ustąpienia pierwszeństwa przy skręcie. Przyspieszenie to minimum z równań IDM dla wszystkich przeszkód:
///
///     a = aMax * [1 - (v/v0)^4 - (s*/s)^2],   s* = s0 + v*T + v*dv / (2*sqrt(aMax*b))
///
/// Pojazd jest kinematyczny: porusza się dokładnie po torze (waypointach), nie jest przepychany przez ściany
/// między pasami ani inne pojazdy. Kolizje są wykrywane geometrycznie (nakładanie się brył) i zliczane.
/// Ruch odbywa się w FixedUpdate (stały krok), więc wynik nie zależy od przyspieszenia czasu (timeScale).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    public enum StopReason { None, Leader, Signal, ExitBlocked, Yield, RoadClosure, Curve }

    // ---- Rejestr i liczniki globalne (statystyki, nagroda ML) ----
    public static readonly List<CarController> Active = new List<CarController>();
    public static int CollisionCount { get; private set; }
    public static int StuckEvents { get; private set; }
    public static int VehiclesCrossedStopLine { get; private set; }

    public static void ResetGlobalCounters()
    {
        CollisionCount = 0;
        StuckEvents = 0;
        VehiclesCrossedStopLine = 0;
        RedLightViolations = 0;
        GridlockResolutions = 0;
    }

    private const float HaltedSpeedThreshold = 0.5f;
    private const float MaxLeaderDetectionRadius = 1.2f;  // ≈ pół szerokości auta (1.1 m) + zapas
    private const float StopLineProbeRadius = 1.0f;
    private const float CommitMinimumSpeed = 3f;      // wolno jadący pojazd zawsze zdąży stanąć
    private const float StopLineBackCheck = 1.5f;     // wykrywa linię także, gdy przód lekko ją przekroczył
    private const float ConflictDistance = 1.8f;      // tory bliżej niż tyle = punkt konfliktu
    private const float YieldClearance = 3.5f;        // jak daleko przed punktem konfliktu czeka ustępujący
    private const float MyPathLength = 45f;
    private const float OtherPathLength = 75f;

    [Header("Car Detection & Obstacle")]
    [Tooltip("Minimalny zasięg wykrywania przeszkód (m).")]
    [SerializeField] private float stopDistance = 5f;
    [Tooltip("Promień wykrywania pojazdu z przodu (ograniczony do 1 m, aby nie hamować przed autami z sąsiedniego pasa).")]
    [SerializeField] private float obstacleCheckRadius = 0.8f;
    [SerializeField] private float maxLookAhead = 70f;
    [Tooltip("Co ile sekund symulacji odświeżać wykrywanie (między odczytami odległości są aktualizowane na bieżąco).")]
    [SerializeField] private float sensingInterval = 0.1f;

    [Header("General")]
    public Waypoint[] waypoints;
    [Tooltip("Prędkość pożądana v0 [m/s] (12 m/s ≈ 43 km/h).")]
    public float speed = 12f;
    [Tooltip("Nieużywane – skręt wynika z toru jazdy.")]
    public float rotationSpeed = 5f;
    public float CurrentSpeed;
    [Tooltip("Długość pojazdu [m] – ustawiana automatycznie z collidera.")]
    public int vehicleLength = 4;
    [HideInInspector] public int sourceLaneId;
    [HideInInspector] public bool hasChosenLaneRoute;
    [HideInInspector] public bool stopForRoadClosure;

    [Header("Model IDM")]
    [Tooltip("Czas odstępu T [s].")]
    [SerializeField] private float timeHeadway = 1.2f;
    [Tooltip("Minimalny odstęp s0 [m].")]
    [SerializeField] private float minimumGap = 2f;
    [Tooltip("Maksymalne przyspieszenie [m/s²]. Autobusy: automatycznie 1.2.")]
    [SerializeField] private float maxAcceleration = 2.0f;
    [Tooltip("Komfortowe opóźnienie b [m/s²].")]
    [SerializeField] private float comfortableDeceleration = 2.5f;
    [Tooltip("Fizyczny limit hamowania [m/s²].")]
    [SerializeField] private float maxDeceleration = 8f;
    [Tooltip("Żółte po zielonym: jeśli zatrzymanie wymaga większego opóźnienia, kierowca przejeżdża.")]
    [SerializeField] private float yellowCommitDeceleration = 3.5f;
    [Tooltip("Prędkość na ostrym zakręcie [m/s].")]
    [SerializeField] private float turnSpeed = 7f;
    [Tooltip("Jak daleko przed linią zatrzymania staje pojazd [m].")]
    [SerializeField] private float stopLineOffset = 1f;
    [Tooltip("Ułamek v0, z jakim pojazd wjeżdża do symulacji.")]
    [SerializeField, Range(0f, 1f)] private float initialSpeedFactor = 0.7f;
    [Tooltip("Zapas czasu [s] przy ustępowaniu: pojazd z pierwszeństwem nie może dojechać do punktu konfliktu " +
             "wcześniej niż (czas mojego przejazdu przez konflikt + zapas).")]
    [SerializeField] private float yieldSafetyMargin = 2.0f;
    [Tooltip("Po ilu sekundach postoju (nie na czerwonym) zgłosić podejrzenie zakleszczenia.")]
    [SerializeField] private float stuckWarningSeconds = 300f;

    [Header("Intersection & Lights")]
    public LineLightManager lineManager;
    public bool isTurningLeft = false;
    [Tooltip("Manewr na skrzyżowaniu (ustawiany przez ChooseLane / spawner). Pierwszeństwo: prosto > w prawo > w lewo.")]
    public TrafficMovement movement = TrafficMovement.Straight;

    [Header("Status (diagnostyka)")]
    public bool stopForLight;
    public bool isAfterCar;
    public bool stopForCollision;
    public bool isLaneBlocked;
    public StopReason limitingReason;
    public int currentWaypointIndex = 0;

    public bool IsHalted => CurrentSpeed < HaltedSpeedThreshold;
    public bool HasCrossedStopLine { get; private set; }
    public float Length { get; private set; } = 4f;

    /// <summary>Sygnalizator, który obowiązuje ten pojazd (najbliższy na trasie, w razie braku – ostatnio przypisany).</summary>
    public LineLightManager GoverningLight => upcomingLight != null ? upcomingLight : lineManager;

    private Waypoint CurrentWaypoint => (waypoints != null && currentWaypointIndex < waypoints.Length) ? waypoints[currentWaypointIndex] : null;
    private Vector3 Pos => rb != null ? rb.position : transform.position;
    private TrafficMovement EffectiveMovement => isTurningLeft ? TrafficMovement.Left : movement;
    // Prosta zasada: jadący prosto ma pierwszeństwo i nikomu nie ustępuje; skręcający (w lewo/prawo) czekają.
    private bool IsTurning => EffectiveMovement != TrafficMovement.Straight;
    private bool MustYieldAtIntersection => IsTurning && !committedTurn;

    private Rigidbody rb;
    private BoxCollider body;
    private float frontOffset;
    private float groundOffset;
    private float rideHeight = float.NaN;
    private float accelerationLimit;
    private float decelerationComfort;
    private Vector3 segmentStart;
    private bool pathFinished;
    private Vector3 finalDirection = Vector3.forward;
    private bool hasLastPointOverride;
    private Vector3 lastPointOverride;
    private bool destroyAtPathEnd;
    private float sensingTimer;
    private float yieldTimer;

    // Wyniki wykrywania (odległości liczone od przedniego zderzaka wzdłuż toru)
    private float leaderGap = float.PositiveInfinity;
    private CarController leaderVehicle;          // pojazd, który jest obecnie liderem (do wykrywania zakleszczeń)
    private float leaderSpeed;
    private float stopLineDistance = float.PositiveInfinity;
    private LightMonitorForCar stopLineMonitor;
    private LineLightManager stopLineLight;
    private LightMonitorForCar committedMonitor;
    private float yieldGap = float.PositiveInfinity;
    private bool committedTurn;
    private float curveDistance = float.PositiveInfinity;
    private float curveSpeed = float.PositiveInfinity;
    private LineLightManager upcomingLight;

    // Tor jazdy przed pojazdem (od tylnego zderzaka), używany do wyznaczania punktów konfliktu
    private readonly List<Vector3> pathPoints = new List<Vector3>(12);
    private readonly List<float> pathDistances = new List<float>(12); // odległość od PRZEDNIEGO zderzaka (tył < 0)
    private float realPathEnd;
    private float exitDistance = float.PositiveInfinity;

    private float stoppedTime;
    private bool stuckReported;
    private float spawnTime;
    private float distanceTravelled;
    private Vector3 spawnPosition;
    private readonly HashSet<int> touching = new HashSet<int>();

    private static readonly RaycastHit[] HitBuffer = new RaycastHit[32];
    private static readonly Collider[] OverlapBuffer = new Collider[32];
    private static readonly Dictionary<Collider, CarController> CarByCollider = new Dictionary<Collider, CarController>();
    private static readonly Dictionary<Collider, LightMonitorForCar> MonitorByCollider = new Dictionary<Collider, LightMonitorForCar>();

    // =====================================================================
    // Cykl życia
    // =====================================================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;   // tor jazdy wyznacza model – fizyka nie przepycha pojazdów
        rb.useGravity = false;
        // Prefaby mają "Freeze Position Y" + zablokowane obroty – blokowało to osadzenie auta na jezdni,
        // przez co auta jechały na wysokości spawnera i promienie mijały triggery linii zatrzymania.
        rb.constraints = RigidbodyConstraints.None;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        body = GetComponent<BoxCollider>();
        if (body != null)
        {
            Vector3 scale = transform.lossyScale;
            frontOffset = (body.center.z + body.size.z * 0.5f) * scale.z;
            Length = Mathf.Abs(body.size.z * scale.z);
            groundOffset = -(body.center.y - body.size.y * 0.5f) * scale.y; // środek -> spód pojazdu
        }
        else
        {
            frontOffset = vehicleLength * 0.5f;
            Length = vehicleLength;
            groundOffset = 1f;
        }
        vehicleLength = Mathf.Max(1, Mathf.RoundToInt(Length));

        bool isBus = CompareTag("Bus");
        accelerationLimit = isBus ? Mathf.Min(maxAcceleration, 1.2f) : maxAcceleration;
        decelerationComfort = isBus ? Mathf.Min(comfortableDeceleration, 2.0f) : comfortableDeceleration;
    }

    private void OnEnable() => Active.Add(this);
    private void OnDisable() => Active.Remove(this);

    private void Start()
    {
        CurrentSpeed = speed * initialSpeedFactor;
        spawnTime = Time.time;
        spawnPosition = Pos;
        if (waypoints != null && waypoints.Length > 0 && segmentStart == Vector3.zero)
            segmentStart = Flat(Pos);
        SnapToGround();
    }

    private void FixedUpdate()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        float dt = Time.fixedDeltaTime;

        if (AdvanceWaypoints()) return; // pojazd dojechał do niszczyciela

        sensingTimer -= dt;
        if (sensingTimer <= 0f)
        {
            Sense();
            sensingTimer = sensingInterval;
        }

        yieldTimer -= dt;
        if (yieldTimer <= 0f)
        {
            EvaluateYielding();
            yieldTimer = 0.2f;
        }

        float acceleration = ComputeAcceleration();
        float newSpeed = Mathf.Max(0f, CurrentSpeed + acceleration * dt);
        float travelled = 0.5f * (CurrentSpeed + newSpeed) * dt;
        CurrentSpeed = newSpeed;
        distanceTravelled += travelled;

        // Aktualizacja odległości między odczytami czujników (dead reckoning)
        if (!float.IsPositiveInfinity(leaderGap)) leaderGap -= travelled - leaderSpeed * dt;
        stopLineDistance -= travelled;
        yieldGap -= travelled;
        curveDistance -= travelled;

        Quaternion rotation = Steer(dt);
        Vector3 forward = rotation * Vector3.forward;
        forward.y = 0f;
        forward.Normalize();
        Vector3 next = Pos + forward * travelled;
        if (!float.IsNaN(rideHeight)) next.y = rideHeight;
        rb.MovePosition(next);
        rb.MoveRotation(rotation);

        UpdateStuckDetection(dt);
    }

    // =====================================================================
    // Trasa
    // =====================================================================

    public void SetFirstWaypoint(Waypoint[] newWaypoints)
    {
        if (newWaypoints == null || newWaypoints.Length == 0)
        {
            Debug.LogError($"🚗 {gameObject.name} otrzymał pustą tablicę waypointów!");
            return;
        }

        waypoints = newWaypoints;
        Vector3 position = Flat(Pos);
        Vector3 forward = FlatForward();
        segmentStart = position;
        ComputeFinalSegment();

        // Najbliższy waypoint PRZED pojazdem (wcześniej: najbliższy w ogóle, co mogło zawrócić auto).
        int bestAhead = -1, bestAny = 0;
        float bestAheadDistance = float.MaxValue, bestAnyDistance = float.MaxValue;
        for (int i = 0; i < newWaypoints.Length; i++)
        {
            if (newWaypoints[i] == null) continue;
            Vector3 toWaypoint = WaypointPos(i) - position;
            float distance = toWaypoint.magnitude;
            if (distance < bestAnyDistance) { bestAnyDistance = distance; bestAny = i; }
            if (Vector3.Dot(forward, toWaypoint) > 0f && distance < bestAheadDistance)
            {
                bestAheadDistance = distance;
                bestAhead = i;
            }
        }

        currentWaypointIndex = bestAhead >= 0 ? bestAhead : bestAny;
        pathFinished = false;
        sensingTimer = 0f;
        yieldTimer = 0f;

        if (CurrentWaypoint != null && CurrentWaypoint.linkedController != null)
            lineManager = CurrentWaypoint.linkedController;
    }

    /// <summary>
    /// Ostatni odcinek do niszczyciela (CarDestroyer) w scenie często biegnie lekko na skos przez kilka pasów
    /// (np. z x=2.8 do x=-2 na 117 m), przez co pojazdy przecinały sąsiedni pas. Taki odcinek prostujemy
    /// wzdłuż osi drogi – pojazd zostaje na swoim pasie i znika na końcu trasy.
    /// </summary>
    private void ComputeFinalSegment()
    {
        hasLastPointOverride = false;
        destroyAtPathEnd = false;
        int last = waypoints.Length - 1;
        if (last < 0 || waypoints[last] == null) return;

        destroyAtPathEnd = waypoints[last].GetComponent<CarDestroyer>() != null;
        if (!destroyAtPathEnd) return;

        Vector3 end = Flat(waypoints[last].transform.position);
        Vector3 start = last >= 1 && waypoints[last - 1] != null ? Flat(waypoints[last - 1].transform.position) : Flat(Pos);
        Vector3 segment = end - start;
        if (segment.magnitude < 15f) return;

        Vector3 axis = Mathf.Abs(segment.x) > Mathf.Abs(segment.z)
            ? new Vector3(Mathf.Sign(segment.x), 0f, 0f)
            : new Vector3(0f, 0f, Mathf.Sign(segment.z));
        if (Vector3.Angle(segment, axis) > 20f) return; // to skręt, a nie dryf między pasami

        hasLastPointOverride = true;
        lastPointOverride = start + axis * Vector3.Dot(segment, axis);
    }

    private Vector3 WaypointPos(int index)
    {
        if (hasLastPointOverride && index == waypoints.Length - 1) return lastPointOverride;
        return Flat(waypoints[index].transform.position);
    }

    /// <returns>true, jeśli pojazd został zniszczony.</returns>
    private bool AdvanceWaypoints()
    {
        if (pathFinished) return false;
        Vector3 position = Flat(Pos);

        for (int guard = 0; guard < waypoints.Length; guard++)
        {
            Waypoint target = CurrentWaypoint;
            if (target == null)
            {
                if (!MoveToNextWaypoint(Vector3.zero)) return false;
                continue;
            }

            Vector3 end = WaypointPos(currentWaypointIndex);
            Vector3 segment = end - segmentStart;
            float t = segment.sqrMagnitude > 0.0001f ? Vector3.Dot(position - segmentStart, segment) / segment.sqrMagnitude : 1f;
            // Osiągnięty także, gdy znalazł się za pojazdem w pobliżu – inaczej auto mogło krążyć wokół
            // waypointu leżącego wewnątrz jego promienia skrętu.
            Vector3 toEnd = end - position;
            bool behindAndClose = Vector3.Dot(FlatForward(), toEnd) < 0f && toEnd.sqrMagnitude < 64f;
            bool reached = t >= 1f || toEnd.sqrMagnitude < 1f || behindAndClose;
            if (!reached) break;

            OnWaypointReached(target);
            segmentStart = end;
            if (!MoveToNextWaypoint(segment))
            {
                if (destroyAtPathEnd)
                {
                    Destroy(gameObject); // koniec trasy = wyjazd z symulacji
                    return true;
                }
                return false;
            }
        }

        if (CurrentWaypoint != null && CurrentWaypoint.linkedController != null)
            lineManager = CurrentWaypoint.linkedController;
        return false;
    }

    private bool MoveToNextWaypoint(Vector3 lastSegment)
    {
        if (currentWaypointIndex < waypoints.Length - 1)
        {
            currentWaypointIndex++;
            return true;
        }

        pathFinished = true;
        finalDirection = lastSegment.sqrMagnitude > 0.0001f ? lastSegment.normalized : FlatForward();
        return false;
    }

    private void OnWaypointReached(Waypoint waypoint)
    {
        if (waypoint.isAfterTrafiicLight)
        {
            MarkCrossedStopLine();
            isTurningLeft = false;                 // manewr zakończony
            movement = TrafficMovement.Straight;
            committedTurn = false;
        }
    }

    private Quaternion Steer(float dt)
    {
        float lookAhead = Mathf.Clamp(CurrentSpeed * 0.7f, 2.5f, 8f);
        Vector3 direction = PointAlongPath(lookAhead) - Pos;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return rb.rotation;

        Quaternion target = Quaternion.LookRotation(direction.normalized, Vector3.up);
        float maxDegrees = Mathf.Max(30f, CurrentSpeed * 25f) * dt;
        return Quaternion.RotateTowards(rb.rotation, target, maxDegrees);
    }

    /// <summary>Punkt na torze jazdy w zadanej odległości przed rzutem pojazdu na tor (pure pursuit).</summary>
    private Vector3 PointAlongPath(float distance)
    {
        Vector3 position = Flat(Pos);
        if (pathFinished || CurrentWaypoint == null)
            return position + (pathFinished ? finalDirection : FlatForward()) * distance;

        Vector3 end = WaypointPos(currentWaypointIndex);
        Vector3 segment = end - segmentStart;
        float length = segment.magnitude;
        Vector3 direction = length > 0.001f ? segment / length : FlatForward();
        float t = length > 0.001f ? Mathf.Clamp01(Vector3.Dot(position - segmentStart, direction) / length) : 1f;
        Vector3 projected = segmentStart + segment * t;

        float remaining = length * (1f - t);
        if (distance <= remaining) return projected + direction * distance;

        distance -= remaining;
        Vector3 previous = end;
        for (int i = currentWaypointIndex + 1; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            Vector3 next = WaypointPos(i);
            Vector3 step = next - previous;
            float stepLength = step.magnitude;
            if (stepLength < 0.001f) continue;
            direction = step / stepLength;
            if (distance <= stepLength) return previous + direction * distance;
            distance -= stepLength;
            previous = next;
        }
        return previous + direction * distance;
    }

    // =====================================================================
    // Wykrywanie przeszkód wzdłuż toru jazdy
    // =====================================================================

    private void Sense()
    {
        leaderGap = float.PositiveInfinity; leaderVehicle = null;
        leaderSpeed = 0f;
        stopLineDistance = float.PositiveInfinity;
        stopLineMonitor = null;
        curveDistance = float.PositiveInfinity;
        curveSpeed = float.PositiveInfinity;
        upcomingLight = null;

        SnapToGround();
        DetectBodyOverlaps();
        BuildPathAhead();
        ComputeExitDistance();

        float v = CurrentSpeed;
        float lookAhead = Mathf.Clamp(minimumGap + v * timeHeadway + v * v / (2f * decelerationComfort) + 15f,
                                      Mathf.Max(stopDistance, 15f), maxLookAhead);

        Vector3 forward = FlatForward();
        Vector3 origin = Pos + forward * frontOffset;
        Vector3 from = origin;
        Vector3 lastDirection = forward;
        float travelled = 0f;
        int index = currentWaypointIndex;
        bool firstSegment = true;

        // Linia zatrzymania tuż za przodem pojazdu (przód lekko ją przekroczył, ale auto jeszcze nie przejechało).
        if (!HasCrossedStopLine)
        {
            int backHits = Physics.SphereCastNonAlloc(origin - forward * StopLineBackCheck, StopLineProbeRadius, forward, HitBuffer,
                                                      StopLineBackCheck, ~0, QueryTriggerInteraction.Collide);
            float bestBack = float.PositiveInfinity;
            for (int i = 0; i < backHits; i++)
            {
                if (!HitBuffer[i].collider.isTrigger || HitBuffer[i].distance >= bestBack) continue;
                LightMonitorForCar monitor = GetMonitor(HitBuffer[i].collider);
                if (monitor == null) continue;
                bestBack = HitBuffer[i].distance;
                stopLineMonitor = monitor;
            }
            if (stopLineMonitor != null) stopLineDistance = bestBack - StopLineBackCheck;
        }

        for (int guard = 0; guard < 64 && travelled < lookAhead; guard++)
        {
            bool beyondEnd = pathFinished || waypoints == null || index >= waypoints.Length;
            Vector3 to;
            if (!beyondEnd)
            {
                if (waypoints[index] == null) { index++; continue; }
                to = WaypointPos(index);
                to.y = origin.y;
            }
            else
            {
                to = from + lastDirection * (lookAhead - travelled);
            }

            Vector3 segment = to - from;
            float segmentLength = segment.magnitude;

            // Waypoint między środkiem a przodem pojazdu – nie rzutujemy "do tyłu".
            if (firstSegment && !beyondEnd && (segmentLength < 0.05f || Vector3.Dot(segment, forward) <= 0f))
            {
                ExamineWaypoint(index, 0f);
                index++;
                continue;
            }
            firstSegment = false;
            if (segmentLength < 0.05f)
            {
                if (beyondEnd) break;
                ExamineWaypoint(index, travelled);
                index++;
                continue;
            }

            Vector3 direction = segment / segmentLength;
            lastDirection = direction;
            float castLength = Mathf.Min(segmentLength, lookAhead - travelled);

            // Linię zatrzymania szukamy także ZA pojazdem z przodu – inaczej auto jadące tuż za liderem,
            // który przejechał na żółtym, widziało linię za późno i przejeżdżało na czerwonym.
            CastSegment(from, direction, castLength, travelled, float.IsPositiveInfinity(leaderGap));
            travelled += castLength;

            if (!float.IsPositiveInfinity(leaderGap) && stopLineMonitor != null) break;
            if (castLength < segmentLength || beyondEnd) break;

            ExamineWaypoint(index, travelled);
            from = to;
            index++;
        }

        DetectConvergingVehicles(forward, origin);

        // Który sygnalizator obowiązuje na wykrytej linii zatrzymania?
        stopLineLight = null;
        if (stopLineMonitor != null)
        {
            if (upcomingLight != null) stopLineLight = upcomingLight;
            else if (!HasCrossedStopLine) stopLineLight = lineManager != null ? lineManager : stopLineMonitor.ControlledLane;
            // Po przejechaniu linii ignorujemy kolejne "linie" bez nowego sygnalizatora na trasie.
        }
    }

    private void CastSegment(Vector3 from, Vector3 direction, float length, float distanceOffset, bool searchLeader = true)
    {
        // 1. Pojazd z przodu
        float radius = Mathf.Min(obstacleCheckRadius, MaxLeaderDetectionRadius);
        int hitCount = searchLeader
            ? Physics.SphereCastNonAlloc(from, radius, direction, HitBuffer, length, ~0, QueryTriggerInteraction.Ignore)
            : 0;
        for (int i = 0; i < hitCount; i++)
        {
            CarController other = GetCar(HitBuffer[i].collider);
            if (other == null || other == this || IgnoresCrossing(other)) continue;

            float gap = distanceOffset + HitBuffer[i].distance;
            if (gap < leaderGap)
            {
                leaderGap = gap; leaderVehicle = other;
                leaderSpeed = other.CurrentSpeed * Mathf.Max(0f, Vector3.Dot(other.FlatForward(), direction));
            }
        }

        // 2. Linia zatrzymania (trigger LightMonitorForCar) – kula o szerokości pojazdu, bo tor skrętu
        //    może biec skosem przy samej krawędzi triggera (cienki promień go wtedy mijał).
        if (stopLineMonitor != null) return;
        hitCount = Physics.SphereCastNonAlloc(from, StopLineProbeRadius, direction, HitBuffer, length, ~0, QueryTriggerInteraction.Collide);
        float best = float.PositiveInfinity;
        for (int i = 0; i < hitCount; i++)
        {
            if (!HitBuffer[i].collider.isTrigger) continue;
            LightMonitorForCar monitor = GetMonitor(HitBuffer[i].collider);
            if (monitor == null || HitBuffer[i].distance >= best) continue;
            best = HitBuffer[i].distance;
            stopLineMonitor = monitor;
        }
        if (stopLineMonitor != null) stopLineDistance = distanceOffset + best;
    }

    /// <summary>
    /// Pojazdy, które nie są (jeszcze) na moim torze, ale zbiegają się z nim tuż przede mną.
    /// Pojazd skręcający (który jeszcze nie ruszył w manewr) ustępuje jadącemu prosto; równorzędni –
    /// ustępuje ten bardziej z tyłu. Dzięki temu dwa pojazdy nigdy nie czekają na siebie nawzajem.
    /// </summary>
    private void DetectConvergingVehicles(Vector3 forward, Vector3 front)
    {
        Vector3 right = new Vector3(forward.z, 0f, -forward.x);
        float radius = Mathf.Clamp(4f + CurrentSpeed * 0.6f, 4f, 9f);
        Vector3 center = front + forward * (radius * 0.8f);
        int count = Physics.OverlapSphereNonAlloc(center, radius, OverlapBuffer, ~0, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            CarController other = GetCar(OverlapBuffer[i]);
            if (other == null || other == this || IgnoresCrossing(other)) continue;

            Vector3 toOther = other.Pos - Pos;
            toOther.y = 0f;
            float longitudinal = Vector3.Dot(toOther, forward);
            if (longitudinal <= 0.5f) continue; // pojazd obok/za mną – to on ma uważać

            Vector3 otherForward = other.FlatForward();
            float lateral = Vector3.Dot(toOther, right);
            float otherLateralHeading = Vector3.Dot(otherForward, right);
            const float corridorHalfWidth = 2.4f;
            bool inCorridor = Mathf.Abs(lateral) < corridorHalfWidth;

            int myRank = PriorityRank(), otherRank = other.PriorityRank();
            if (myRank > otherRank && !inCorridor) continue; // mam pierwszeństwo – reaguję tylko na fizyczną blokadę
            if (myRank == otherRank)
            {
                float meInOtherFrame = Vector3.Dot(-toOther, otherForward);
                if (meInOtherFrame > longitudinal || (Mathf.Approximately(meInOtherFrame, longitudinal) && GetInstanceID() < other.GetInstanceID()))
                    continue;
            }

            bool converging = other.CurrentSpeed > 1f && Mathf.Abs(lateral) < 6f &&
                              lateral * otherLateralHeading < 0f && Mathf.Abs(otherLateralHeading) > 0.1f;
            if (converging && !inCorridor)
            {
                float lateralSpeed = Mathf.Max(0.5f, other.CurrentSpeed * Mathf.Abs(otherLateralHeading));
                float timeToCorridor = (Mathf.Abs(lateral) - corridorHalfWidth) / lateralSpeed;
                float timeToReach = (longitudinal - frontOffset) / Mathf.Max(1f, CurrentSpeed);
                converging = timeToCorridor < timeToReach + 1.5f;
            }
            if (!inCorridor && !converging) continue;

            float gap = Mathf.Max(0f, longitudinal - frontOffset - other.Length * 0.5f);
            if (gap < leaderGap)
            {
                leaderGap = gap; leaderVehicle = other;
                leaderSpeed = other.CurrentSpeed * Mathf.Max(0f, Vector3.Dot(otherForward, forward));
            }
        }
    }

    private void ExamineWaypoint(int index, float distance)
    {
        Waypoint waypoint = waypoints[index];
        if (waypoint == null) return;

        if (upcomingLight == null && waypoint.linkedController != null)
            upcomingLight = waypoint.linkedController;

        // Ograniczenie prędkości na zakręcie
        float angle = HeadingChangeAfter(index, 12f);
        if (angle > 20f)
        {
            float allowed = Mathf.Lerp(speed, Mathf.Min(turnSpeed, speed), Mathf.InverseLerp(20f, 70f, angle));
            float limitAtCar = Mathf.Sqrt(allowed * allowed + 2f * decelerationComfort * distance);
            float currentLimit = float.IsPositiveInfinity(curveDistance)
                ? float.PositiveInfinity
                : Mathf.Sqrt(curveSpeed * curveSpeed + 2f * decelerationComfort * Mathf.Max(0f, curveDistance));
            if (limitAtCar < currentLimit)
            {
                curveDistance = distance;
                curveSpeed = allowed;
            }
        }
    }

    /// <summary>Zmiana kierunku [stopnie] między odcinkiem wchodzącym do waypointu a kierunkiem toru po kolejnych metrach.</summary>
    private float HeadingChangeAfter(int index, float span)
    {
        Vector3 point = WaypointPos(index);
        Vector3 incoming = Vector3.zero;
        for (int i = index - 1; i >= 0; i--)
        {
            if (waypoints[i] == null) continue;
            incoming = point - WaypointPos(i);
            break;
        }
        if (index == currentWaypointIndex) incoming = point - segmentStart;
        if (incoming.sqrMagnitude < 0.0001f) return 0f;

        Vector3 previous = point;
        float travelled = 0f;
        Vector3 outgoing = Vector3.zero;
        for (int i = index + 1; i < waypoints.Length && travelled < span; i++)
        {
            if (waypoints[i] == null) continue;
            Vector3 next = WaypointPos(i);
            travelled += (next - previous).magnitude;
            outgoing = next - point;
            previous = next;
        }
        if (outgoing.sqrMagnitude < 0.0001f) return 0f;
        return Vector3.Angle(incoming, outgoing);
    }

    // =====================================================================
    // Pierwszeństwo na skrzyżowaniu (punkty konfliktu torów)
    // =====================================================================

    /// <summary>2 = jazda prosto (lub manewr już rozpoczęty), 1 = skręt w prawo, 0 = skręt w lewo.</summary>
    public int PriorityRank()
    {
        if (committedTurn) return 2;
        switch (EffectiveMovement)
        {
            case TrafficMovement.Left: return 0;
            case TrafficMovement.Right: return 1;
            default: return 2;
        }
    }

    /// <summary>Tor jazdy od tylnego zderzaka do ~75 m przed pojazdem (punkty XZ + odległość od przodu).</summary>
    private void BuildPathAhead()
    {
        pathPoints.Clear();
        pathDistances.Clear();
        Vector3 forward = FlatForward();
        Vector3 front = Flat(Pos) + forward * frontOffset;
        pathPoints.Add(front - forward * Length);
        pathDistances.Add(-Length);
        pathPoints.Add(front);
        pathDistances.Add(0f);

        float travelled = 0f;
        Vector3 previous = front;
        bool first = true;
        for (int i = currentWaypointIndex; !pathFinished && waypoints != null && i < waypoints.Length && travelled < OtherPathLength; i++)
        {
            if (waypoints[i] == null) continue;
            Vector3 p = WaypointPos(i);
            if (first && Vector3.Dot(p - front, forward) <= 0f) continue; // waypoint za przodem pojazdu
            first = false;
            travelled += (p - previous).magnitude;
            pathPoints.Add(p);
            pathDistances.Add(travelled);
            previous = p;
        }
        realPathEnd = pathFinished ? 0f : travelled;
        if (travelled < OtherPathLength)
        {
            Vector3 direction = pathPoints.Count >= 2 ? (pathPoints[pathPoints.Count - 1] - pathPoints[pathPoints.Count - 2]) : forward;
            direction.y = 0f;
            direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : forward;
            pathPoints.Add(previous + direction * (OtherPathLength - travelled));
            pathDistances.Add(OtherPathLength);
        }
    }

    /// <summary>
    /// Pojazd skręcający w lewo/prawo ustępuje pojazdom o wyższym priorytecie (prosto > prawo > lewo),
    /// których tor przecina jego tor. Czeka przed punktem konfliktu, jeśli pojazd z pierwszeństwem
    /// dojedzie tam, zanim on zdąży przejechać (z zapasem). Gdy luka jest wystarczająca – rusza i kończy manewr.
    /// </summary>
    private void EvaluateYielding()
    {
        yieldGap = float.PositiveInfinity;
        if (!MustYieldAtIntersection || pathPoints.Count < 2) return;

        int myRank = PriorityRank();
        float nearestConflict = float.PositiveInfinity;
        Vector3 myPos = Pos;
        Vector3 myForward = FlatForward();

        for (int c = 0; c < Active.Count; c++)
        {
            CarController other = Active[c];
            if (other == this || other.pathPoints.Count < 2 || other.PriorityRank() <= myRank) continue;
            Vector3 toOther = other.Pos - myPos;
            if (toOther.sqrMagnitude > 110f * 110f) continue;

            // Pojazd jadący przede mną w tym samym kierunku to "lider", a nie konflikt.
            if (Vector3.Angle(other.FlatForward(), myForward) < 35f) continue;

            if (!FindConflict(other, out float myDistance, out float otherDistance)) continue;
            if (myDistance > MyPathLength) continue;
            nearestConflict = Mathf.Min(nearestConflict, myDistance);

            float otherEta;
            if (otherDistance <= 0f) otherEta = 0f; // zajmuje strefę konfliktu
            else if (other.CurrentSpeed > 0.5f) otherEta = otherDistance / other.CurrentSpeed;
            else if (other.CanStartSoon()) otherEta = 1f + Mathf.Sqrt(2f * otherDistance / Mathf.Max(0.5f, other.accelerationLimit));
            else continue; // stoi na czerwonym – nie stanowi zagrożenia

            // Ostrożnie: przejazd przez konflikt z prędkością skrętu (albo ruszając z miejsca – co dłużej trwa).
            float clearDistance = myDistance + Length + 2f;
            float fromStandstill = 1f + Mathf.Sqrt(2f * clearDistance / Mathf.Max(0.5f, accelerationLimit));
            float atTurnSpeed = clearDistance / Mathf.Max(2f, Mathf.Min(CurrentSpeed, turnSpeed));
            float myClearTime = CurrentSpeed > 3f ? atTurnSpeed : Mathf.Max(fromStandstill, atTurnSpeed);

            if (otherEta < myClearTime + yieldSafetyMargin)
                yieldGap = Mathf.Min(yieldGap, myDistance - YieldClearance);
        }

        // Poniższe dwie zasady decydują o WJEŹDZIE na skrzyżowanie, więc działają tylko przed linią zatrzymania.
        // Pojazd, który już jest na skrzyżowaniu, ma z niego zjechać, gdy tylko nic nie nadjeżdża
        // (stojące na czerwonym auta z naprzeciwka go nie blokują – inaczej stał na środku w nieskończoność).

        // Za skrzyżowaniem brak miejsca -> nie wjeżdżam w strefę konfliktu (inaczej stanę na środku i zablokuję innych).
        if (!HasCrossedStopLine && !float.IsPositiveInfinity(nearestConflict) && NoRoomBehindIntersection())
            yieldGap = Mathf.Min(yieldGap, nearestConflict - YieldClearance);

        // Dawna logika sceny: czujnik z naprzeciwka widzi pojazd jadący prosto -> skręcający w lewo czeka.
        if (!HasCrossedStopLine && EffectiveMovement == TrafficMovement.Left && !float.IsPositiveInfinity(nearestConflict))
        {
            LineLightManager light = GoverningLight;
            if (light != null && !light.leftTurnAllowed)
                yieldGap = Mathf.Min(yieldGap, nearestConflict - YieldClearance);
        }

        // Wolna luka i jestem tuż przed punktem konfliktu -> wjeżdżam i kończę manewr bez zatrzymywania się.
        if (IsTurning && float.IsPositiveInfinity(yieldGap) && nearestConflict - YieldClearance < 1.5f)
            committedTurn = true;
        // Już za punktem oczekiwania (nie da się stanąć przed konfliktem) -> jadę dalej.
        if (yieldGap < 0f && CurrentSpeed > CommitMinimumSpeed)
        {
            if (IsTurning) committedTurn = true;
            yieldGap = float.PositiveInfinity;
        }
    }

    /// <summary>
    /// Stojący pojazd jest zagrożeniem tylko, jeśli za chwilę ruszy: stoi pierwszy przed linią, a ma zielone.
    /// Pojazd zablokowany przez innych (lider / ustępowanie) nie ruszy – czekanie na niego prowadziło do zakleszczeń.
    /// </summary>
    private bool CanStartSoon()
    {
        if (limitingReason == StopReason.Leader || limitingReason == StopReason.Yield || limitingReason == StopReason.ExitBlocked)
            return false;
        if (HasCrossedStopLine) return limitingReason == StopReason.None;
        LineLightManager light = GoverningLight;
        return light == null || light.currentColor == TrafficLightColor.green;
    }

    /// <summary>Pierwszy (najbliższy mi) punkt, w którym nasze tory zbliżają się na mniej niż ConflictDistance.</summary>
    private bool FindConflict(CarController other, out float myDistance, out float otherDistance)
    {
        myDistance = float.PositiveInfinity;
        otherDistance = float.PositiveInfinity;
        // Mój tor tylko do ostatniego prawdziwego waypointu (bez przedłużenia "na wprost" za końcem trasy).
        for (int i = 1; i + 1 < pathPoints.Count && pathDistances[i] < MyPathLength && pathDistances[i + 1] <= realPathEnd + 0.01f; i++)
        {
            Vector3 a = pathPoints[i], b = pathPoints[i + 1];
            for (int j = 0; j + 1 < other.pathPoints.Count; j++)
            {
                Vector3 c = other.pathPoints[j], d = other.pathPoints[j + 1];
                if (!ClosestPointsOnSegments(a, b, c, d, out float s, out float t, out float distance) || distance > ConflictDistance)
                    continue;

                float mine = Mathf.Lerp(pathDistances[i], pathDistances[i + 1], s);
                if (mine >= myDistance) continue;
                myDistance = mine;
                otherDistance = Mathf.Lerp(other.pathDistances[j], other.pathDistances[j + 1], t);
            }
            if (!float.IsPositiveInfinity(myDistance)) return true; // dalsze odcinki są dalej
        }
        return false;
    }

    private static bool ClosestPointsOnSegments(Vector3 p1, Vector3 q1, Vector3 p2, Vector3 q2, out float s, out float t, out float distance)
    {
        Vector2 a = new Vector2(p1.x, p1.z), b = new Vector2(q1.x, q1.z), c = new Vector2(p2.x, p2.z), d = new Vector2(q2.x, q2.z);
        Vector2 d1 = b - a, d2 = d - c, r = a - c;
        float aa = Vector2.Dot(d1, d1), ee = Vector2.Dot(d2, d2), ff = Vector2.Dot(d2, r);
        s = t = 0f;
        distance = float.PositiveInfinity;
        if (aa < 1e-6f || ee < 1e-6f) return false;

        float cc = Vector2.Dot(d1, r), bb = Vector2.Dot(d1, d2), denom = aa * ee - bb * bb;
        s = denom > 1e-6f ? Mathf.Clamp01((bb * ff - cc * ee) / denom) : 0f;
        t = (bb * s + ff) / ee;
        if (t < 0f) { t = 0f; s = Mathf.Clamp01(-cc / aa); }
        else if (t > 1f) { t = 1f; s = Mathf.Clamp01((bb - cc) / aa); }
        distance = Vector2.Distance(a + d1 * s, c + d2 * t);
        return true;
    }

    // =====================================================================
    // IDM
    // =====================================================================

    private float ComputeAcceleration()
    {
        float v = CurrentSpeed;
        stopForLight = false;
        isLaneBlocked = false;
        stopForCollision = false;
        limitingReason = StopReason.None;

        if (stopForRoadClosure)
        {
            limitingReason = StopReason.RoadClosure;
            return v > 0f ? -maxDeceleration : 0f;
        }

        float desiredSpeed = speed;
        if (!float.IsPositiveInfinity(curveDistance))
            desiredSpeed = Mathf.Min(speed, Mathf.Sqrt(curveSpeed * curveSpeed + 2f * decelerationComfort * Mathf.Max(0f, curveDistance)));

        float acceleration = Idm(v, desiredSpeed, float.PositiveInfinity, 0f);
        if (desiredSpeed < speed - 0.1f && acceleration < 0f) limitingReason = StopReason.Curve;

        // Pojazd z przodu
        isAfterCar = leaderGap < Mathf.Max(stopDistance, minimumGap + v * timeHeadway);
        if (!float.IsPositiveInfinity(leaderGap))
            Consider(Idm(v, desiredSpeed, leaderGap, leaderSpeed), StopReason.Leader, ref acceleration);

        // Linia zatrzymania: czerwone / żółte / brak miejsca za skrzyżowaniem
        if (stopLineMonitor != null && stopLineLight != null && stopLineMonitor != committedMonitor)
        {
            bool redOrYellow = stopLineLight.currentColor != TrafficLightColor.green;
            bool exitBlocked = stopLineLight.isBlocked || NoRoomBehindIntersection();
            if (redOrYellow || exitBlocked)
            {
                float distance = stopLineDistance - stopLineOffset;
                float needed = RequiredDeceleration(v, distance);
                bool cannotStopComfortably = v > CommitMinimumSpeed && needed > yellowCommitDeceleration;
                bool amberOrBlockedOnly = !redOrYellow || stopLineLight.IsAmberAfterGreen;

                if (cannotStopComfortably && (amberOrBlockedOnly || needed > maxDeceleration))
                {
                    committedMonitor = stopLineMonitor; // "dilemma zone" – przejeżdża
                }
                else
                {
                    StopReason reason = redOrYellow ? StopReason.Signal : StopReason.ExitBlocked;
                    Consider(Idm(v, desiredSpeed, stopLineDistance - stopLineOffset + minimumGap, 0f), reason, ref acceleration);
                    stopForLight = redOrYellow;
                    isLaneBlocked = exitBlocked;
                }
            }
        }

        // Ustąpienie pierwszeństwa przy skręcie: czekamy przed punktem konfliktu
        if (!float.IsPositiveInfinity(yieldGap) && !committedTurn)
        {
            Consider(Idm(v, desiredSpeed, Mathf.Max(0f, yieldGap) + minimumGap, 0f), StopReason.Yield, ref acceleration);
            stopForCollision = true;
        }

        return Mathf.Clamp(acceleration, -maxDeceleration, accelerationLimit);
    }

    /// <summary>
    /// "Nie blokuj skrzyżowania": wjeżdżamy tylko, jeśli pojazd przed nami na naszym torze nie stoi
    /// w strefie skrzyżowania ani tuż za nią (musi być miejsce na całą długość naszego pojazdu za strefą).
    /// </summary>
    private bool NoRoomBehindIntersection()
    {
        if (float.IsPositiveInfinity(leaderGap) || float.IsPositiveInfinity(exitDistance)) return false;
        if (leaderSpeed > 2f) return false; // lider odjeżdża
        return leaderGap < exitDistance + Length + minimumGap;
    }

    /// <summary>Odległość (od przodu, wzdłuż toru) do miejsca, w którym tor opuszcza strefę skrzyżowania.</summary>
    private void ComputeExitDistance()
    {
        exitDistance = float.PositiveInfinity;
        if (!IntersectionZone.TryGet(out Rect zone)) return;

        bool entered = false;
        for (int i = 1; i + 1 < pathPoints.Count; i++)
        {
            Vector3 a = pathPoints[i], b = pathPoints[i + 1];
            float length = Vector3.Distance(a, b);
            int steps = Mathf.Max(1, Mathf.CeilToInt(length / 1f));
            for (int s = 1; s <= steps; s++)
            {
                Vector3 p = Vector3.Lerp(a, b, s / (float)steps);
                bool inside = zone.Contains(new Vector2(p.x, p.z));
                if (inside) entered = true;
                else if (entered)
                {
                    exitDistance = Mathf.Lerp(pathDistances[i], pathDistances[i + 1], s / (float)steps);
                    return;
                }
            }
            if (pathDistances[i + 1] > 90f) return;
        }
    }

    /// <summary>Opóźnienie potrzebne do zatrzymania na zadanym dystansie (v²/2d). Dla stojącego pojazdu = 0.</summary>
    private static float RequiredDeceleration(float v, float distance) => v * v / (2f * Mathf.Max(distance, 0.01f));

    private void Consider(float candidate, StopReason reason, ref float acceleration)
    {
        if (candidate < acceleration)
        {
            acceleration = candidate;
            limitingReason = reason;
        }
    }

    private float Idm(float v, float desiredSpeed, float gap, float obstacleSpeed)
    {
        float v0 = Mathf.Max(0.1f, desiredSpeed);
        float freeRoad = 1f - Mathf.Pow(v / v0, 4f);
        freeRoad = Mathf.Max(freeRoad, -decelerationComfort / accelerationLimit); // łagodne zwalnianie przed zakrętem
        if (float.IsPositiveInfinity(gap)) return accelerationLimit * freeRoad;

        float approach = v - obstacleSpeed;
        float desiredGap = minimumGap + Mathf.Max(0f, v * timeHeadway + v * approach / (2f * Mathf.Sqrt(accelerationLimit * decelerationComfort)));
        float ratio = desiredGap / Mathf.Max(gap, 0.05f);
        return accelerationLimit * (freeRoad - ratio * ratio);
    }

    // =====================================================================
    // Diagnostyka: kolizje, zakleszczenia, przejazd przez linię, osadzenie na jezdni
    // =====================================================================

    /// <summary>Pojazd kinematyczny nie dostaje OnCollisionEnter – kolizje wykrywamy nakładaniem się brył.</summary>
    private void DetectBodyOverlaps()
    {
        if (body == null) return;
        // 85% wymiarów: liczymy realne wjechanie w siebie, a nie otarcie narożnikami na zakręcie.
        Vector3 halfExtents = Vector3.Scale(body.size, transform.lossyScale) * 0.5f * 0.85f;
        Vector3 center = Pos + rb.rotation * Vector3.Scale(body.center, transform.lossyScale);
        int count = Physics.OverlapBoxNonAlloc(center, halfExtents, OverlapBuffer, rb.rotation, ~0, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            CarController other = GetCar(OverlapBuffer[i]);
            if (other == null || other == this || GetInstanceID() > other.GetInstanceID()) continue;
            if (IgnoresCrossing(other)) continue; // celowe przenikanie przy rozwiązywaniu zakleszczenia
            if (!touching.Add(other.GetInstanceID())) continue;

            CollisionCount++;
            if (CollisionCount <= 30)
            {
                Vector3 toOther = other.Pos - Pos;
                toOther.y = 0f;
                float headingAngle = Vector3.Angle(FlatForward(), other.FlatForward());
                float bearing = Vector3.SignedAngle(FlatForward(), toOther, Vector3.up);
                string kind = headingAngle < 30f ? "ten sam kierunek" : headingAngle > 150f ? "czołowo" : "poprzecznie";
                Debug.LogWarning($"[Ruch] Kolizja #{CollisionCount} ({kind}, kąt {headingAngle:F0}°, drugi pojazd pod kątem {bearing:F0}°) w {Pos}\n" +
                                 $"  A: {Describe()}\n  B: {other.Describe()}");
            }
        }

        // Kontakt zakończony -> przy następnym zetknięciu liczymy kolizję ponownie
        if (touching.Count > 0)
        {
            touching.RemoveWhere(id =>
            {
                for (int i = 0; i < count; i++)
                {
                    CarController other = GetCar(OverlapBuffer[i]);
                    if (other != null && other.GetInstanceID() == id) return false;
                }
                return true;
            });
        }
    }

    /// <summary>Ustawia wysokość pojazdu na jezdni (pojazd kinematyczny nie spada grawitacyjnie).</summary>
    private void SnapToGround()
    {
        Vector3 origin = Pos + Vector3.up * 1.0f;
        int count = Physics.RaycastNonAlloc(origin, Vector3.down, HitBuffer, 10f, ~0, QueryTriggerInteraction.Ignore);
        float best = float.PositiveInfinity;
        float groundY = float.NaN;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = HitBuffer[i];
            if (GetCar(hit.collider) != null || hit.normal.y < 0.7f || hit.distance >= best) continue;
            best = hit.distance;
            groundY = hit.point.y;
        }
        if (!float.IsNaN(groundY)) rideHeight = groundY + groundOffset;

    }

    // ---- Rozwiązywanie zakleszczeń (jak "teleport" w SUMO, ale łagodniej) ----
    public static int GridlockResolutions { get; private set; }
    private static float lastGhostStart = -999f;
    private const float GridlockWaitSeconds = 12f;
    private const float GhostDuration = 6f;
    private float ghostUntil = -1f;

    private bool IsGhosting => Time.time < ghostUntil;

    /// <summary>Podczas rozwiązywania zakleszczenia pojazd ignoruje pojazdy poprzeczne (nie te przed nim w tym samym kierunku).</summary>
    private bool IgnoresCrossing(CarController other)
    {
        if (!IsGhosting && !other.IsGhosting) return false;
        return Vector3.Angle(FlatForward(), other.FlatForward()) > 45f;
    }

    private void UpdateStuckDetection(float dt)
    {
        if (CurrentSpeed >= 0.1f)
        {
            stoppedTime = 0f;
            stuckReported = false;
            return;
        }

        stoppedTime += dt;

        // Zabezpieczenie: pojazd na skrzyżowaniu, który ustępuje (Yield) zbyt długo, kończy manewr
        // (sam ruch nadal pilnuje pojazdów fizycznie przed nim).
        if (HasCrossedStopLine && limitingReason == StopReason.Yield && stoppedTime > 25f && !committedTurn)
        {
            committedTurn = true;
            yieldGap = float.PositiveInfinity;
            stoppedTime = 0f;
        }

        // Prawdziwe zakleszczenie: pojazd JUŻ na skrzyżowaniu (za linią zatrzymania) stoi, bo blokuje go pojazd
        // jadący W POPRZEK, który też stoi. Zwykłe czekanie na światło lub na przepuszczenie (Yield) to nie zakleszczenie.
        bool blockedByStoppedCrossing = leaderVehicle != null && leaderVehicle.CurrentSpeed < 0.1f &&
                                        Vector3.Angle(FlatForward(), leaderVehicle.FlatForward()) > 45f;
        if (stoppedTime > GridlockWaitSeconds && !IsGhosting && HasCrossedStopLine &&
            limitingReason == StopReason.Leader && blockedByStoppedCrossing &&
            Time.time - lastGhostStart > 3f &&
            IntersectionZone.TryGet(out Rect zone) && zone.Contains(new Vector2(Pos.x, Pos.z)))
        {
            lastGhostStart = Time.time;
            ghostUntil = Time.time + GhostDuration;
            committedTurn = true;
            yieldGap = float.PositiveInfinity;
            stoppedTime = 0f;
            GridlockResolutions++;
            SimulationKpi.GridlockEvents++;
            if (GridlockResolutions <= 20)
                Debug.LogWarning($"[Ruch] Rozwiązano zakleszczenie #{GridlockResolutions} (pojazd przejeżdża mimo pojazdu poprzecznego).\n  {Describe()}");
        }
        if (!stuckReported && stoppedTime > stuckWarningSeconds && limitingReason != StopReason.Signal)
        {
            stuckReported = true;
            StuckEvents++;
            Debug.LogWarning($"[Ruch] Pojazd stoi od {stoppedTime:F0} s. Możliwe zakleszczenie.\n  {Describe()}");
        }
    }

    private string Describe()
    {
        string waypoint = CurrentWaypoint != null ? CurrentWaypoint.name : "-";
        string source = waypoints != null && waypoints.Length > 0 && waypoints[0] != null ? waypoints[0].name : "-";
        return $"{name} pas {sourceLaneId}, manewr={EffectiveMovement}, wiek={Time.time - spawnTime:F1} s, przejechane={distanceTravelled:F0} m, " +
               $"start={spawnPosition}, v={CurrentSpeed:F1} m/s, powód={limitingReason}, " +
               $"lider={(float.IsPositiveInfinity(leaderGap) ? "brak" : leaderGap.ToString("F1") + " m")}, " +
               $"przejechałLinię={HasCrossedStopLine}, wManewrze={committedTurn}, " +
               $"cel={waypoint} (#{currentWaypointIndex}, trasa od {source}), koniecTrasy={pathFinished}";
    }

    private void MarkCrossedStopLine()
    {
        if (HasCrossedStopLine) return;
        HasCrossedStopLine = true;
        VehiclesCrossedStopLine++;
    }

    // Wywoływane przez LightMonitorForCar. Wjazd na linię niczego nie przesądza (auto może jeszcze stanąć),
    // dopiero opuszczenie triggera = pojazd w całości przejechał linię zatrzymania.
    public void StartTrafficLightMonitoring()
    {
        // Diagnostyka: wjazd przodem na linię zatrzymania przy czerwonym = przejazd na czerwonym.
        LineLightManager light = GoverningLight;
        if (HasCrossedStopLine || light == null || light.currentColor != TrafficLightColor.red) return;
        RedLightViolations++;
        if (RedLightViolations <= 20)
            Debug.LogWarning($"[Ruch] Przejazd na czerwonym #{RedLightViolations} ({light.name}).\n  {Describe()}");
    }

    public static int RedLightViolations { get; private set; }

    public void StopTrafficLightMonitoring()
    {
        stopForLight = false;
        MarkCrossedStopLine();
    }

    // Zachowane dla zgodności z Sensor.cs (dawna logika kolizji na środku skrzyżowania).
    public void SetMiddleIntersectionState(bool state)
    {
        if (!state) stopForCollision = false;
    }

    // =====================================================================
    // Pomocnicze
    // =====================================================================

    private Vector3 Flat(Vector3 point) => new Vector3(point.x, Pos.y, point.z);

    public Vector3 FlatForward()
    {
        Vector3 forward = (rb != null ? rb.rotation : transform.rotation) * Vector3.forward;
        forward.y = 0f;
        return forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
    }

    private static CarController GetCar(Collider collider)
    {
        if (collider == null) return null;
        if (!CarByCollider.TryGetValue(collider, out CarController car) || car == null)
        {
            car = collider.GetComponentInParent<CarController>();
            if (CarByCollider.Count > 2000) CarByCollider.Clear(); // zniszczone pojazdy
            CarByCollider[collider] = car;
        }
        return car;
    }

    private static LightMonitorForCar GetMonitor(Collider collider)
    {
        if (!MonitorByCollider.TryGetValue(collider, out LightMonitorForCar monitor))
        {
            monitor = collider.GetComponent<LightMonitorForCar>();
            MonitorByCollider[collider] = monitor;
        }
        return monitor;
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        Gizmos.color = Color.magenta;
        for (int i = 0; i + 1 < pathPoints.Count; i++) Gizmos.DrawLine(pathPoints[i] + Vector3.up, pathPoints[i + 1] + Vector3.up);
        Vector3 origin = Pos + FlatForward() * frontOffset;
        Gizmos.color = limitingReason == StopReason.None ? Color.green : Color.red;
        if (!float.IsPositiveInfinity(leaderGap)) Gizmos.DrawLine(origin, origin + FlatForward() * leaderGap);
        Gizmos.color = Color.yellow;
        if (!float.IsPositiveInfinity(yieldGap)) Gizmos.DrawWireSphere(origin + FlatForward() * Mathf.Max(0f, yieldGap), 0.6f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(PointAlongPath(Mathf.Clamp(CurrentSpeed * 0.7f, 2.5f, 8f)), 0.3f);
    }
}
