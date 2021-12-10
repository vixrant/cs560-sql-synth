using System;
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
        [WitnessFunction(nameof(Semantics.Order), 0)]
        internal DisjunctiveDoubleFilteredTableSpec WitnessOrder1(GrammarRule rule, DisjunctiveDoubleFilteredTableSpec spec)
        {
            return spec;
        }

        //Ordering is similar to selecting.
        [WitnessFunction(nameof(Semantics.Order), 1, DependsOnParameters = new[] { 0 })]
        internal PossibleOrderingsSpec WitnessOrder2(GrammarRule rule, DisjunctiveDoubleFilteredTableSpec spec, ExampleSpec leftValue)
        {
            Console.Out.WriteLine("One passed order");
            var result = new Dictionary<State, (List<List<string[]>>[] Examples, HashSet<int> Available)>();
            foreach (var example in spec.CustomTableExamples)
            {
                State inputState = example.Key;
                result[inputState] = spec.prepareOrderingSpec(inputState, leftValue.Examples[inputState]);
                if (result[inputState].Item1.Length == 0) return null;
            }
            return new PossibleOrderingsSpec(result);
        }
    }
}
