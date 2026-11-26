using System;
using System.Globalization;
using System.IO;
using UnityEngine;

/// <summary>
/// Zbiera wskaźniki (KPI) z jednego przebiegu godziny i zapisuje je jako wiersz CSV
/// w folderze "Results/kpi_runs.csv" obok projektu (lub obok pliku .exe w buildzie).
/// Pomiar zaczyna się po rozgrzewce (TimeManager, 400 s) i kończy po godzinie / epizodzie.
/// </summary>
public static class SimulationKpi
{
    public static int GridlockEvents;
    public static bool IsMeasuring { get; private set; }

    private static double measuredSeconds;
    private static double waitingVehicleSeconds;  // zatrzymane pojazdy + kolejka przed wjazdem
    private static double haltedVehicleSeconds;   // tylko pojazdy w sieci
    private static int maxBacklog;
    private static int maxHalted;
    private static int crossedAtStart;
    private static int exitedAtStart;
    private static int collisionsAtStart;
    private static int stuckAtStart;
    private static int gridlockAtStart;

    public static string CsvPath => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Results", "kpi_runs.csv"));

    public static void StopMeasurement() => IsMeasuring = false;

    public static void BeginMeasurement()
    {
        IsMeasuring = true;
        measuredSeconds = 0;
        waitingVehicleSeconds = 0;
        haltedVehicleSeconds = 0;
        maxBacklog = 0;
        maxHalted = 0;
        crossedAtStart = CarController.VehiclesCrossedStopLine;
        exitedAtStart = ExitTrigger.TotalVehiclesExited;
        collisionsAtStart = CarController.CollisionCount;
        stuckAtStart = CarController.StuckEvents;
        gridlockAtStart = GridlockEvents;
    }

    public static void Accumulate(int halted, int backlog, float dt)
    {
        if (!IsMeasuring) return;
        measuredSeconds += dt;
        haltedVehicleSeconds += halted * dt;
        waitingVehicleSeconds += (halted + backlog) * dt;
        if (backlog > maxBacklog) maxBacklog = backlog;
        if (halted > maxHalted) maxHalted = halted;
    }

    public static void WriteRow(string system, string mode, int hour, float demandScale, int seed, int episode)
    {
        if (!IsMeasuring || measuredSeconds < 1.0) return;
        IsMeasuring = false;

        int crossed = CarController.VehiclesCrossedStopLine - crossedAtStart;
        int exited = ExitTrigger.TotalVehiclesExited - exitedAtStart;
        double avgWaitPerVehicle = crossed > 0 ? waitingVehicleSeconds / crossed : 0;
        CultureInfo ci = CultureInfo.InvariantCulture;

        string header = "timestamp,scene,system,mode,hour,demand_scale,seed,episode,measured_s,vehicles_crossed,vehicles_exited," +
                        "total_waiting_veh_s,avg_waiting_per_vehicle_s,avg_halted,max_halted,max_backlog,collisions,stuck_events,gridlock_events";
        string row = string.Join(",",
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", ci),
            Quote(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name),
            system,
            mode,
            hour.ToString(ci),
            demandScale.ToString("F2", ci),
            seed.ToString(ci),
            episode.ToString(ci),
            measuredSeconds.ToString("F0", ci),
            crossed.ToString(ci),
            exited.ToString(ci),
            waitingVehicleSeconds.ToString("F0", ci),
            avgWaitPerVehicle.ToString("F2", ci),
            (haltedVehicleSeconds / measuredSeconds).ToString("F2", ci),
            maxHalted.ToString(ci),
            maxBacklog.ToString(ci),
            (CarController.CollisionCount - collisionsAtStart).ToString(ci),
            (CarController.StuckEvents - stuckAtStart).ToString(ci),
            (GridlockEvents - gridlockAtStart).ToString(ci));

        try
        {
            string path = CsvPath;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            bool newFile = !File.Exists(path);
            using (var writer = new StreamWriter(path, true))
            {
                if (newFile) writer.WriteLine(header);
                writer.WriteLine(row);
            }
            Debug.Log($"[KPI] {system}/{mode}: śr. oczekiwanie {avgWaitPerVehicle:F1} s/poj., przejechało {crossed}, " +
                      $"kolizje {CarController.CollisionCount - collisionsAtStart}. Zapisano: {path}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[KPI] Nie udało się zapisać CSV: {e.Message}");
        }
    }

    private static string Quote(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";
}
