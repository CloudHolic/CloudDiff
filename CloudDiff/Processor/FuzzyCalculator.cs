using System;
using System.Collections.Generic;
using System.Linq;
using FLS;
using FLS.Rules;

namespace CloudDiff.Processor
{
    public static class FuzzyCalculator
    {
        public static void Test()
        {
            var water = new LinguisticVariable("Water");
            var cold = water.MembershipFunctions.AddTrapezoid("Cold", 0, 0, 20, 40);
            var warm = water.MembershipFunctions.AddTriangle("Warm", 30, 50, 70);
            var hot = water.MembershipFunctions.AddTrapezoid("Hot", 50, 80, 100, 100);

            var power = new LinguisticVariable("Power");
            var low = power.MembershipFunctions.AddTriangle("Low", 0, 25, 50);
            var high = power.MembershipFunctions.AddTriangle("High", 25, 50, 75);

            var fuzzyEngine = new FuzzyEngineFactory().Default();

            var rule1 = Rule.If(water.Is(cold).Or(water.Is(warm))).Then(power.Is(high));
            var rule2 = Rule.If(water.Is(hot)).Then(power.Is(low));
            fuzzyEngine.Rules.Add(rule1, rule2);

            var result = fuzzyEngine.Defuzzify(new { Water = 10 });
            Console.WriteLine(result);
            Console.ReadLine();
        }

        public static double CalcApproxDefuzzifiedDensity(List<double> densities)
        {
            var density = new LinguisticVariable("Density");
            // TODO: Density's membership functions here.

            var relation = new LinguisticVariable("Relationship");
            // TODO: Relationship's membership functions here.

            var fuzzyEngine = new FuzzyEngineFactory().Create(FuzzyEngineType.CoG);

            // TODO: Fuzzy rules here.
            fuzzyEngine.Rules.Add();

            var result = 0.0;
            foreach(var cur in densities)
                result += cur * fuzzyEngine.Defuzzify(new { Density = cur });
            
            return result / densities.Count;
        }

        public static double CalcDefuzzifiedDensity(List<double> densities)
        {
            var maxden = densities.Max();

            var density = new LinguisticVariable("Density");
            var veryLow = density.MembershipFunctions.AddTrapezoid("VeryLow", 0, 0, 0.2 * maxden, 0.4 * maxden);
            var low = density.MembershipFunctions.AddTriangle("Low", 0.2 * maxden, 0.3 * maxden, 0.4 * maxden);
            var middle = density.MembershipFunctions.AddTrapezoid("Middle", 0.2 * maxden, 0.4 * maxden, 0.6 * maxden, 0.8 * maxden);
            var high = density.MembershipFunctions.AddTriangle("High", 0.6 * maxden, 0.7 * maxden, 0.8 * maxden);
            var verHigh = density.MembershipFunctions.AddTrapezoid("VeryHigh", 0.6 * maxden, 0.8 * maxden, maxden, maxden);

            var relation = new LinguisticVariable("Relationship");
            var linear = relation.MembershipFunctions.AddTriangle("Linear", 0, 1, 1);

            var fuzzyEngine = new FuzzyEngineFactory().Create(FuzzyEngineType.CoG);

            // TODO: Fuzzy rules here.
            fuzzyEngine.Rules.Add();

            var result = 0.0;
            foreach (var cur in densities)
                result += cur * fuzzyEngine.Defuzzify(new { Density = cur });

            return result / densities.Count;
        }
    }
}
