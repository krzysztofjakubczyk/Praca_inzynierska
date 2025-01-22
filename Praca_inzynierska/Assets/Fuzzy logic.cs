using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FuzzyLogicHandler
{
    public class LinguisticVariable
    {
        public string Name { get; set; }
        public Dictionary<string, (double a, double b, double c, double d)> MembershipFunctions { get; set; } = new Dictionary<string, (double a, double b, double c, double d)>();
    }

    public class FuzzyRule
    {
        public string Condition { get; set; }
        public string Output { get; set; }
    }

    public List<LinguisticVariable> InputVariables { get; set; } = new List<LinguisticVariable>();
    public LinguisticVariable OutputVariable { get; set; }
    public List<FuzzyRule> Rules { get; set; } = new List<FuzzyRule>();

    public void InitializeTrapezoidalMembershipFunctions(
        Dictionary<string, (double a, double b, double c, double d)> carCountMemberships,
        Dictionary<string, (double a, double b, double c, double d)> queueLengthMemberships,
        Dictionary<string, (double a, double b, double c, double d)> greenLightDurationMemberships)
    {
        InputVariables.Add(new LinguisticVariable
        {
            Name = "CarCount",
            MembershipFunctions = carCountMemberships
        });

        InputVariables.Add(new LinguisticVariable
        {
            Name = "QueueLength",
            MembershipFunctions = queueLengthMemberships
        });

        OutputVariable = new LinguisticVariable
        {
            Name = "GreenLightDuration",
            MembershipFunctions = greenLightDurationMemberships
        };
    }

    public Dictionary<string, Dictionary<string,double>> Fuzzify(double carCount, double queueLength)
    {
        var fuzzifiedInputs = new Dictionary<string, Dictionary<string, double>>
        {
            { "CarCount", CalculateMemberships(carCount, "CarCount") },
            { "QueueLength", CalculateMemberships(queueLength, "QueueLength") }
        };

        return fuzzifiedInputs;
    }

    private Dictionary<string, double> CalculateMemberships(double value, string variableName)
    {
        var variable = InputVariables.Find(v => v.Name == variableName);
        if (variable == null)
        {
            Debug.LogError($"Variable {variableName} not found.");
            return new Dictionary<string, double>();
        }

        var memberships = new Dictionary<string, double>();

        foreach (var mf in variable.MembershipFunctions)
        {
            memberships[mf.Key] = CalculateTrapezoidalMembership(value, mf.Value);
        }

        return memberships;
    }

    private double CalculateTrapezoidalMembership(double x, (double a, double b, double c, double d) parameters)
    {
        double a = parameters.a, b = parameters.b, c = parameters.c, d = parameters.d;

        if (x < a || x > d)
            return 0;

        if (x >= a && x < b)
            return (x - a) / (b - a);

        if (x >= b && x <= c)
            return 1;

        if (x > c && x <= d)
            return (d - x) / (d - c);

        return 0;
    }

    public Dictionary<string, double> ApplyRules(Dictionary<string, Dictionary<string, double>> fuzzifiedInputs)
    {
        var aggregatedOutputs = new Dictionary<string, double>();

        foreach (var rule in Rules)
        {
            var conditions = rule.Condition.Split('.');
            double ruleStrength = double.MaxValue;

            foreach (var condition in conditions)
            {
                var splitCondition = condition.Split(':');

                if (splitCondition.Length != 2)
                {
                    Debug.LogError($"Invalid condition format: {condition}");
                    ruleStrength = 0;
                    break;
                }

                var variableName = splitCondition[0];
                var membershipName = splitCondition[1];

                if (!fuzzifiedInputs.ContainsKey(variableName))
                {
                    Debug.LogError($"Variable {variableName} not found in fuzzified inputs.");
                    ruleStrength = 0;
                    break;
                }

                if (!fuzzifiedInputs[variableName].ContainsKey(membershipName))
                {
                    Debug.LogError($"Membership {membershipName} not found for variable {variableName}.");
                    ruleStrength = 0;
                    break;
                }

                ruleStrength = Mathf.Min((float)ruleStrength, (float)fuzzifiedInputs[variableName][membershipName]);
            }

            if (ruleStrength > 0)
            {
                if (!aggregatedOutputs.ContainsKey(rule.Output))
                {
                    aggregatedOutputs[rule.Output] = ruleStrength;
                }
                else
                {
                    aggregatedOutputs[rule.Output] = Mathf.Max((float)aggregatedOutputs[rule.Output], (float)ruleStrength);
                }
            }
        }

        return aggregatedOutputs;
    }

    public double Defuzzify(Dictionary<string, double> aggregatedOutputs, string method = "centroid")
    {
        double result = 0;

        if (method == "centroid")
        {
            double numerator = 0;
            double denominator = 0;

            foreach (var output in aggregatedOutputs)
            {
                var membershipFunction = OutputVariable.MembershipFunctions[output.Key];
                double a = membershipFunction.a, b = membershipFunction.b, c = membershipFunction.c, d = membershipFunction.d;

                for (double x = a; x <= d; x += 0.1)
                {
                    double membership = CalculateTrapezoidalMembership(x, membershipFunction);
                    numerator += x * Math.Min(membership, output.Value);
                    denominator += Math.Min(membership, output.Value);
                }
            }

            if (denominator > 0)
            {
                result = numerator / denominator;
            }
            else
            {
                Debug.LogError("Denominator is zero during defuzzification.");
            }
        }
        else if (method == "bisector")
        {
            double totalArea = 0;
            foreach (var output in aggregatedOutputs)
            {
                var membershipFunction = OutputVariable.MembershipFunctions[output.Key];
                double a = membershipFunction.a, b = membershipFunction.b, c = membershipFunction.c, d = membershipFunction.d;

                for (double x = a; x <= d; x += 0.1)
                {
                    totalArea += Math.Min(CalculateTrapezoidalMembership(x, membershipFunction), output.Value) * 0.1;
                }
            }

            double halfArea = totalArea / 2;
            double cumulativeArea = 0;

            foreach (var output in aggregatedOutputs)
            {
                var membershipFunction = OutputVariable.MembershipFunctions[output.Key];
                double a = membershipFunction.a, b = membershipFunction.b, c = membershipFunction.c, d = membershipFunction.d;

                for (double x = a; x <= d; x += 0.1)
                {
                    double area = Math.Min(CalculateTrapezoidalMembership(x, membershipFunction), output.Value) * 0.1;
                    cumulativeArea += area;

                    if (cumulativeArea >= halfArea)
                    {
                        result = x;
                        break;
                    }
                }

                if (cumulativeArea >= halfArea)
                {
                    break;
                }
            }
        }
        else if (method == "mom") // Mean of Maximum
        {
            double maxMembership = 0;
            var maxValues = new List<double>();

            foreach (var output in aggregatedOutputs)
            {
                var membershipFunction = OutputVariable.MembershipFunctions[output.Key];
                double a = membershipFunction.a, b = membershipFunction.b, c = membershipFunction.c, d = membershipFunction.d;

                for (double x = a; x <= d; x += 0.1)
                {
                    double membership = CalculateTrapezoidalMembership(x, membershipFunction);
                    if (membership > maxMembership)
                    {
                        maxMembership = membership;
                        maxValues.Clear();
                    }

                    if (membership == maxMembership)
                    {
                        maxValues.Add(x);
                    }
                }
            }

            if (maxValues.Count > 0)
            {
                result = maxValues.Average();
            }
        }
        else if (method == "lom") // Largest of Maximum
        {
            double maxMembership = 0;

            foreach (var output in aggregatedOutputs)
            {
                var membershipFunction = OutputVariable.MembershipFunctions[output.Key];
                double a = membershipFunction.a, b = membershipFunction.b, c = membershipFunction.c, d = membershipFunction.d;

                for (double x = a; x <= d; x += 0.1)
                {
                    double membership = CalculateTrapezoidalMembership(x, membershipFunction);
                    if (membership >= maxMembership)
                    {
                        maxMembership = membership;
                        result = x;
                    }
                }
            }
        }
        else if (method == "som") // Smallest of Maximum
        {
            double maxMembership = 0;

            foreach (var output in aggregatedOutputs)
            {
                var membershipFunction = OutputVariable.MembershipFunctions[output.Key];
                double a = membershipFunction.a, b = membershipFunction.b, c = membershipFunction.c, d = membershipFunction.d;

                for (double x = a; x <= d; x += 0.1)
                {
                    double membership = CalculateTrapezoidalMembership(x, membershipFunction);
                    if (membership > maxMembership)
                    {
                        maxMembership = membership;
                    }
                }

                for (double x = a; x <= d; x += 0.1)
                {
                    double membership = CalculateTrapezoidalMembership(x, membershipFunction);
                    if (membership == maxMembership)
                    {
                        result = x;
                        break;
                    }
                }
            }
        }
        else
        {
            Debug.LogError($"Defuzzification method {method} is not supported.");
        }

        return result;
    }
}
