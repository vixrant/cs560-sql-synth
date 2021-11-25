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
        [WitnessFunction(nameof(Semantics.Select), 0)]
        internal DisjunctiveDoubleFilteredTableSpec WitnessSelect1(GrammarRule rule, DisjunctiveDoubleFilteredTableSpec spec)
        {
            return spec;
        }

        //For select, we use the concrete value of the table on the left to spin off a different type of spec object on the right.
        [WitnessFunction(nameof(Semantics.Select), 1, DependsOnParameters = new[] { 0 })]
        internal DisjunctiveCriteriaSatSpec WitnessSelect2(GrammarRule rule, DisjunctiveDoubleFilteredTableSpec spec, ExampleSpec leftValue)
        {
            var result = new Dictionary<State, List<List<string[]>>[]>();
            foreach (var example in spec.CustomTableExamples)
            {
                State inputState = example.Key;
                result[inputState] = spec.GetFilterCriteria(inputState, leftValue.Examples[inputState]);
            }
            return new DisjunctiveCriteriaSatSpec(null, result);
        }
    }
}
