using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;

[RequireComponent(typeof(BehaviorParameters))]
public sealed class TrafficSignalMlAgent : Agent
{
    private MainTrafficController controller;
    private bool hasAction;
    private readonly float[] greenTimePreferences = new float[3];

    public bool HasAction => hasAction;
    public float[] GreenTimePreferences => greenTimePreferences;

    public void Initialize(MainTrafficController trafficController)
    {
        controller = trafficController;
        BehaviorParameters behavior = GetComponent<BehaviorParameters>();
        behavior.BehaviorName = "TrafficSignal";
        behavior.BrainParameters.ActionSpec = ActionSpec.MakeContinuous(3);
        behavior.BrainParameters.VectorObservationSize = 12;
    }

    public override void OnEpisodeBegin() => hasAction = false;

    public override void CollectObservations(VectorSensor sensor)
    {
        if (controller == null) controller = GetComponent<MainTrafficController>();
        if (controller == null)
        {
            for (int i = 0; i < 12; i++) sensor.AddObservation(0f);
            return;
        }
        controller.AddPhaseObservations(sensor);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (actions.ContinuousActions.Length < greenTimePreferences.Length)
        {
            Debug.LogError("TrafficSignalMlAgent wymaga trzech akcji ciągłych: preferencji czasu zielonego dla każdej fazy.");
            return;
        }

        for (int i = 0; i < greenTimePreferences.Length; i++)
            greenTimePreferences[i] = Mathf.Clamp(actions.ContinuousActions[i], -1f, 1f);
        hasAction = true;
    }

    public void RequestCyclePlan()
    {
        hasAction = false;
        RequestDecision();
    }

    public void AddPhaseReward(float reward) => AddReward(reward);
}
