using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;

/// <summary>
/// Agent sterujący sygnalizacją w schemacie "przedłuż / przełącz".
/// Co kilka sekund zielonego (MainTrafficController.decisionInterval) agent wybiera:
///   0 = przedłuż bieżącą fazę, 1 = przejdź do następnej fazy (kolejność faz jest stała).
/// Minimalny i maksymalny czas zielonego oraz żółte/all-red zapewnia sterownik – agent nie może
/// wykonać niebezpiecznej akcji (maskowanie akcji).
/// </summary>
[RequireComponent(typeof(BehaviorParameters))]
public sealed class TrafficSignalMlAgent : Agent
{
    public const string BehaviorNameValue = "TrafficSignal";
    public const int KeepAction = 0;
    public const int SwitchAction = 1;

    private MainTrafficController controller;
    private bool hasAction;
    private bool switchRequested;
    private bool canSwitch = true;
    private bool mustSwitch;

    public bool HasAction => hasAction;
    public bool SwitchRequested => switchRequested;

    /// <summary>Musi być wywołane PRZED dodaniem komponentu agenta (ML-Agents tworzy politykę w OnEnable).</summary>
    public static void ConfigureBehavior(BehaviorParameters behavior)
    {
        behavior.BehaviorName = BehaviorNameValue;
        behavior.BrainParameters.VectorObservationSize = MainTrafficController.ObservationSize;
        behavior.BrainParameters.NumStackedVectorObservations = 1;
        behavior.BrainParameters.ActionSpec = ActionSpec.MakeDiscrete(2);
    }

    public void Initialize(MainTrafficController trafficController)
    {
        controller = trafficController;
        BehaviorParameters behavior = GetComponent<BehaviorParameters>();
        if (behavior.BrainParameters.VectorObservationSize != MainTrafficController.ObservationSize ||
            behavior.BrainParameters.ActionSpec.NumDiscreteActions != 1)
        {
            Debug.LogError("TrafficSignalMlAgent: BehaviorParameters ma stary rozmiar obserwacji/akcji. " +
                           "Usuń komponenty Behavior Parameters i TrafficSignalMlAgent z obiektu – zostaną utworzone automatycznie.");
        }
    }

    public string ModeLabel
    {
        get
        {
            if (Academy.IsInitialized && Academy.Instance.IsCommunicatorOn) return "training";
            BehaviorParameters behavior = GetComponent<BehaviorParameters>();
            if (behavior != null && behavior.IsInHeuristicMode()) return "heuristic";
            return "inference";
        }
    }

    public override void OnEpisodeBegin() => hasAction = false;

    public override void CollectObservations(VectorSensor sensor)
    {
        if (controller == null) controller = GetComponent<MainTrafficController>();
        if (controller == null)
        {
            for (int i = 0; i < MainTrafficController.ObservationSize; i++) sensor.AddObservation(0f);
            return;
        }
        controller.AddObservations(sensor);
    }

    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        if (!canSwitch) actionMask.SetActionEnabled(0, SwitchAction, false);
        if (mustSwitch) actionMask.SetActionEnabled(0, KeepAction, false);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        switchRequested = actions.DiscreteActions[0] == SwitchAction;
        hasAction = true;
    }

    /// <summary>Heurystyka = sterowanie akomodacyjne (actuated). Działa bez trenera i bez modelu.</summary>
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discrete = actionsOut.DiscreteActions;
        discrete[0] = controller != null && controller.ActuatedWantsSwitch() ? SwitchAction : KeepAction;
    }

    public void RequestSwitchDecision(bool allowSwitch, bool forceSwitch)
    {
        canSwitch = allowSwitch;
        mustSwitch = forceSwitch;
        hasAction = false;
        RequestDecision();
    }

    public void AddStepReward(float reward) => AddReward(reward);
}
