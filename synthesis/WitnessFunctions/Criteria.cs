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
        [WitnessFunction(nameof(Semantics.One), 0)]
        internal DisjunctiveExamplesSpec WitnessOne1(GrammarRule rule, DisjunctiveCriteriaSatSpec spec)
        {
            var result = new Dictionary<State, IEnumerable<object>>();
            foreach (var example in spec.SatExamples)
            {
                State inputState = example.Key;
                var possiblecriterion = spec.GetSatisfiers(inputState);//ew List<Tuple<int,int,int>>{new Tuple<int,int,int>(0,0,0)};//
                if (possiblecriterion.Count == 0) return null;
                result[inputState] = possiblecriterion.Cast<object>();
            }
            return new DisjunctiveExamplesSpec(result);
        }

        // [WitnessFunction(nameof(Semantics.More), 0)]
        // internal DisjunctiveExamplesSpec WitnessMore1(GrammarRule rule, DisjunctiveCriteriaSatSpec spec) {
        //     Console.Out.WriteLine("Witness MORE left");
        //     var result = new Dictionary<State, IEnumerable<object>>();
        //     foreach (var example in spec.SatExamples) {
        //         State inputState = example.Key;
        //         var possiblecriterion = spec.GetSatisfiers(inputState);
        //         if (possiblecriterion.Count == 0) return null;
        //         result[inputState] = possiblecriterion.Cast<object>();
        //     }
        //     return new DisjunctiveExamplesSpec(result);
        // }

        // [WitnessFunction(nameof(Semantics.More), 1, DependsOnParameters = new[] { 0 })]
        // internal DisjunctiveCriteriaSatSpec WitnessMore2(GrammarRule rule, DisjunctiveCriteriaSatSpec spec, ExampleSpec leftValue) {
        //     Console.Out.WriteLine("Witness MORE right");
        //     var result1 = new Dictionary<State, Tuple<int,int,int>>();
        //     var result2 = new Dictionary<State, List<List<string[]>>[]>();
        //     foreach (var example in spec.SatExamples) {
        //         State inputState = example.Key;
        //         result2[inputState] = example.Value;
        //         result1[inputState] = leftValue.Examples[inputState] as Tuple<int,int,int>;
        //     }
        //     return new DisjunctiveCriteriaSatSpec(result1,result2);
        // }
    }
}
