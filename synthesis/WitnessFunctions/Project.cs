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
        [WitnessFunction(nameof(Semantics.Project), 0)]
        internal DisjunctiveDoubleFilteredTableSpec WitnessProject1(GrammarRule rule, ExampleSpec spec)
        {
            return DisjunctiveDoubleFilteredTableSpec.FromConcrete(spec);
        }

        //for Project, checking to see that the example satisfies the spec results in a set of possible mappings. We yield this set.
        [WitnessFunction(nameof(Semantics.Project), 1, DependsOnParameters = new[] { 0 }, Verify = true)]
        internal DisjunctiveExamplesSpec WitnessProject2(GrammarRule rule, ExampleSpec spec, ExampleSpec leftValue)
        {
            var result = new Dictionary<State, IEnumerable<object>>();
            foreach (var example in spec.Examples)
            {
                State inputState = example.Key;
                var possiblemappings = new DisjunctiveDoubleFilteredTableSpec(new Dictionary<State, List<string[][]>[]>{{
                    inputState,
                    new List<string[][]>[]{
                        DisjunctiveDoubleFilteredTableSpec.ConvertConcrete(example.Value as List<string[]>)
                    }
                }}).extractPossibleColumnMappings(inputState, leftValue.Examples[inputState]);
                if (possiblemappings.Count == 0) return null;
                result[inputState] = possiblemappings.Cast<object>();
            }
            return new DisjunctiveExamplesSpec(result);
        }
    }
}
