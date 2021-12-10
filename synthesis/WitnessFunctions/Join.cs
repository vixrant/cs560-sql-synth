using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.AST;
using Microsoft.ProgramSynthesis.Compiler;
using Microsoft.ProgramSynthesis.Learning;
using Microsoft.ProgramSynthesis.Learning.Strategies;
using Microsoft.ProgramSynthesis.Specifications;
using Microsoft.ProgramSynthesis.VersionSpace;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.ProgramSynthesis.Rules;

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
            // Console.Out.WriteLine("join right");
            var result = new Dictionary<State, List<string[][]>[]>();
            foreach (var example in spec.CustomTableExamples)
            {
                State inputState = example.Key;
                int leftJoinColumn = (int)(firstcolumn.Examples[inputState] as int?);
                result[inputState] = spec.addJoiningColumnToSpec(inputState, leftValue.Examples[inputState], leftJoinColumn).Item1;
                if (result[inputState].Length == 0) {
                    // Console.Out.WriteLine("pass?");
                    return null;
                } else {
                    // Console.Out.WriteLine("failure?");
                }
            }
            return new DisjunctiveDoubleFilteredTableSpec(result);
        }
        // //the right column of join is retrieved in a similar fashion to WitnessProject2.
        [WitnessFunction(nameof(Semantics.Join), 3, DependsOnParameters = new[] { 0, 1, 2 })]
        internal DisjunctiveExamplesSpec WitnessJoin4(GrammarRule rule, DisjunctiveDoubleFilteredTableSpec spec, ExampleSpec leftValue, ExampleSpec firstcolumn, ExampleSpec rightValue)
        {
            var halfresult = new Dictionary<State, HashSet<int>>();
            HashSet<int> conjunctiveDisqualify = null;
            foreach (var example in spec.CustomTableExamples) {
                State inputState = example.Key;

                int leftJoinColumn = (int)(firstcolumn.Examples[inputState] as int?);
                var (ajcts,checkmap) = spec.addJoiningColumnToSpec(inputState,leftValue.Examples[inputState],leftJoinColumn);

                var ks = new DisjunctiveDoubleFilteredTableSpec(new Dictionary<State, List<string[][]>[]> {
                    {inputState,ajcts}
                }).extractPossibleSingularLastColumnMappings(inputState, rightValue.Examples[inputState]);
                if (ks.Count == 0) return null;
                var candidate1 = leftValue.Examples[inputState] as List<string[]>;
                var candidate2 = rightValue.Examples[inputState] as List<string[]>;
                var mks = new HashSet<int>();
                var disqualify = new HashSet<int>();

                foreach (var (fcol,omap) in ks) {
                    mks.Add(fcol);
                    var shouldadd = false;
                    if (candidate1.Count!=candidate2.Count) shouldadd=true;
                    if (!shouldadd) {
                        // var encountered = new HashSet<String>();
                        for (int i=0;i<candidate1.Count;i++) {
                            var a = candidate2[i][fcol];
                            var b = candidate1[i][leftJoinColumn];
                            if (a != b) {
                                shouldadd = true;
                                break;
                            }
                            // if (encountered.Contains(a)) {
                            //     shouldadd=true;
                            //     break;
                            // } else {
                            //     encountered.Add(a);
                            // }
                        }
                    }
                    if (omap==null) shouldadd=true;
                    if (!shouldadd) {
                        shouldadd = true;
                        foreach (var chckm in checkmap) {
                            var is_subset = true;
                            for (int i=0;i<chckm.Length;i++) {
                                if (!omap[i].IsSubsetOf(chckm[i])) {
                                    is_subset=false;
                                    break;
                                }
                            }
                            if (is_subset) {
                                shouldadd = false;
                                break;
                            }
                        }
                    }
                    if (!shouldadd) {
                        disqualify.Add(fcol);
                    }
                }
                if (conjunctiveDisqualify==null) {
                    conjunctiveDisqualify = disqualify;
                } else {
                    conjunctiveDisqualify.IntersectWith(disqualify);
                }
                halfresult[inputState] = mks;//.Cast<object>();
            }
            var result = new Dictionary<State, IEnumerable<object>>();
            foreach (var example in halfresult) {
                State inputState = example.Key;
                Console.Out.WriteLine("here are the excepted values: {0}",String.Join(", ",halfresult[inputState].Intersect(conjunctiveDisqualify)));
                halfresult[inputState].ExceptWith(conjunctiveDisqualify);
                result[inputState] = example.Value.Cast<object>();
            }
            return new DisjunctiveExamplesSpec(result);
        }
    }
}
