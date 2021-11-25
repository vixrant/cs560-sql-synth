using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.Learning;
using Microsoft.ProgramSynthesis.Rules;
using Microsoft.ProgramSynthesis.Specifications;

using Rest560SpecV1;

namespace Rest560
{
    public partial class WitnessFunctions : DomainLearningLogic
    {
        [WitnessFunction(nameof(Semantics.OneKey), 0)]
        internal DisjunctiveExamplesSpec WitnessOneKey1(GrammarRule rule, PossibleOrderingsSpec spec)
        {
            Console.Out.WriteLine("Witness ONEKey left");
            var result = new Dictionary<State, IEnumerable<object>>();
            foreach (var example in spec.OrdExamples)
            {
                State inputState = example.Key;
                var possiblecriterion = spec.GetSatisfiers(inputState);//ew List<Tuple<int,int,int>>{new Tuple<int,int,int>(0,0,0)};//
                if (possiblecriterion.Count == 0) return null;
                result[inputState] = possiblecriterion.Cast<object>();
            }
            return new DisjunctiveExamplesSpec(result);
        }

        [WitnessFunction(nameof(Semantics.OneKey), 0)]
        internal DisjunctiveExamplesSpec WitnessMoreKey1(GrammarRule rule, PossibleOrderingsSpec spec)
        {
            Console.Out.WriteLine("Witness MOREKey left");
            var result = new Dictionary<State, IEnumerable<object>>();
            foreach (var example in spec.OrdExamples)
            {
                State inputState = example.Key;
                var possiblecriterion = spec.GetSatisfiers(inputState);//ew List<Tuple<int,int,int>>{new Tuple<int,int,int>(0,0,0)};//
                if (possiblecriterion.Count == 0) return null;
                result[inputState] = possiblecriterion.Cast<object>();
            }
            return new DisjunctiveExamplesSpec(result);
        }

        [WitnessFunction(nameof(Semantics.MoreKey), 1, DependsOnParameters = new[] { 0 })]
        internal PossibleOrderingsSpec WitnessMoreKey2(GrammarRule rule, PossibleOrderingsSpec spec, ExampleSpec leftValue)
        {
            Console.Out.WriteLine("Witness MOREKey right");
            var result = new Dictionary<State, (List<List<string[]>>[] Examples, HashSet<int> Available)>();
            foreach (var example in spec.OrdExamples)
            {
                State inputState = example.Key;
                result[inputState] = spec.sortBy(inputState, leftValue.Examples[inputState] as Tuple<int, bool>);
            }
            return new PossibleOrderingsSpec(result);
        }
    }
}
