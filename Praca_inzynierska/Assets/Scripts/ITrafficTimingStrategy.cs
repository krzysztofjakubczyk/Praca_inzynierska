using System.Collections.Generic;
using UnityEngine;

public abstract class ITrafficTimingStrategy : MonoBehaviour
{
    protected FuzzyLogicHandler fuzzyLogicHandler;

    public abstract void AdjustGreenLightDurations();
    protected void InitializeFuzzyLogic()
    {
        fuzzyLogicHandler = new FuzzyLogicHandler();

        var carCountMemberships = new Dictionary<string, (double a, double b, double c, double d)>
        {
            { "Low", (0,5,9,13) },
            { "Medium", (9,13,17,21) },
            { "High", (17,21,25,29) }
        };

        var queueLengthMemberships = new Dictionary<string, (double a, double b, double c, double d)>
        {
            { "Small", (0,20,36,52) },
            { "Medium", ( 36, 52, 68, 84) },
            { "Big", (68, 84, 100, 116) }
        };

        var greenLightDurationMemberships = new Dictionary<string, (double a, double b, double c, double d)>
        {
            { "Short", (0, 5, 10, 15) },  // Dla ma³ych kolejek
            { "Medium", (15, 20, 25, 30) }, // Dla œrednich czasów
            { "Long", (25, 30, 35, 40) }    // Dla d³ugich czasów
        };


        fuzzyLogicHandler.Rules.Add(new FuzzyLogicHandler.FuzzyRule { Condition = "CarCount:Low.QueueLength:Small", Output = "Short" });
        fuzzyLogicHandler.Rules.Add(new FuzzyLogicHandler.FuzzyRule { Condition = "CarCount:Low.QueueLength:Medium", Output = "Short" });
        fuzzyLogicHandler.Rules.Add(new FuzzyLogicHandler.FuzzyRule { Condition = "CarCount:Low.QueueLength:Big", Output = "Medium" });
        fuzzyLogicHandler.Rules.Add(new FuzzyLogicHandler.FuzzyRule { Condition = "CarCount:Medium.QueueLength:Small", Output = "Medium" });
        fuzzyLogicHandler.Rules.Add(new FuzzyLogicHandler.FuzzyRule { Condition = "CarCount:Medium.QueueLength:Medium", Output = "Medium" });
        fuzzyLogicHandler.Rules.Add(new FuzzyLogicHandler.FuzzyRule { Condition = "CarCount:Medium.QueueLength:Big", Output = "Long" });
        fuzzyLogicHandler.Rules.Add(new FuzzyLogicHandler.FuzzyRule { Condition = "CarCount:High.QueueLength:Small", Output = "Medium" });
        fuzzyLogicHandler.Rules.Add(new FuzzyLogicHandler.FuzzyRule { Condition = "CarCount:High.QueueLength:Medium", Output = "Long" });
        fuzzyLogicHandler.Rules.Add(new FuzzyLogicHandler.FuzzyRule { Condition = "CarCount:High.QueueLength:Big", Output = "Long" });

        fuzzyLogicHandler.InitializeTrapezoidalMembershipFunctions(carCountMemberships, queueLengthMemberships, greenLightDurationMemberships);

    }
}