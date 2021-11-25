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
        [WitnessFunction(nameof(Semantics.Join), 0)]
        internal DisjunctiveDoubleFilteredTableSpec WitnessJoin1(GrammarRule rule, DisjunctiveDoubleFilteredTableSpec spec)
        {
            return spec.SplitColumnwise();
        }

        //for the left column of join, we just yield each possible column, and the answer gets checked by witness functions 3 and 4 of join.
        [WitnessFunction(nameof(Semantics.Join), 1, DependsOnParameters = new[] { 0 })]
        internal DisjunctiveExamplesSpec WitnessJoin2(GrammarRule rule, DisjunctiveDoubleFilteredTableSpec spec, ExampleSpec leftValue)
        {
            var result = new Dictionary<State, IEnumerable<object>>();
            foreach (var example in spec.CustomTableExamples)
            {
                State inputState = example.Key;
                var ks = new List<int>();
                var x = (leftValue.Examples[inputState] as List<string[]>)[0].Length;
                for (int h = 0; h < x; h++) ks.Add(h);
                if (ks.Count == 0) return null;
                result[inputState] = ks.Cast<object>();
            }
            return new DisjunctiveExamplesSpec(result);
        }

        //for the right side of join, a lot must be performed.
        //A simplified description is this: First, we take the example from the left and figure out how many columns it may satisfy.
        //Then, we take the remaining columns and append a column that corresponds to the joining column, and yeild that as the spec.
        [WitnessFunction(nameof(Semantics.Join), 2, DependsOnParameters = new[] { 0, 1 })]
        internal DisjunctiveDoubleFilteredTableSpec WitnessJoin3(GrammarRule rule, DisjunctiveDoubleFilteredTableSpec spec, ExampleSpec leftValue, ExampleSpec firstcolumn)
        {
            var result = new Dictionary<State, List<string[][]>[]>();
            foreach (var example in spec.CustomTableExamples)
            {
                State inputState = example.Key;
                int leftJoinColumn = (int)(firstcolumn.Examples[inputState] as int?);
                result[inputState] = spec.addJoiningColumnToSpec(inputState, leftValue.Examples[inputState], leftJoinColumn);
                if (result[inputState].Length == 0) return null;
            }
            return new DisjunctiveDoubleFilteredTableSpec(result);
        }
        // //the right column of join is retrieved in a similar fashion to WitnessProject2.
        [WitnessFunction(nameof(Semantics.Join), 3, DependsOnParameters = new[] { 0, 1, 2 })]
        internal DisjunctiveExamplesSpec WitnessJoin4(GrammarRule rule, DisjunctiveDoubleFilteredTableSpec spec, ExampleSpec leftValue, ExampleSpec firstcolumn, ExampleSpec rightValue)
        {
            var result = new Dictionary<State, IEnumerable<object>>();
            foreach (var example in spec.CustomTableExamples)
            {
                State inputState = example.Key;
                int leftJoinColumn = (int)(firstcolumn.Examples[inputState] as int?);
                var ks = new DisjunctiveDoubleFilteredTableSpec(new Dictionary<State, List<string[][]>[]> {
                    {inputState,spec.addJoiningColumnToSpec(inputState,leftValue.Examples[inputState],leftJoinColumn)}
                }).extractPossibleSingularLastColumnMappings(inputState, rightValue.Examples[inputState]);
                if (ks.Count == 0) return null;
                result[inputState] = ks.Cast<object>();
            }
            return new DisjunctiveExamplesSpec(result);
        }
    }
}
