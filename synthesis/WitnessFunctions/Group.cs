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
        [WitnessFunction(nameof(Semantics.Group), 0)]
        internal DisjunctiveDoubleFilteredTableSpec WitnessGroup1(GrammarRule rule, DisjunctiveDoubleFilteredTableSpec spec)
        {
            return spec;
        }

        [WitnessFunction(nameof(Semantics.Group), 1, DependsOnParameters = new[] { 0 })]
        internal DisjunctiveExamplesSpec WitnessGroup2(GrammarRule rule, DisjunctiveDoubleFilteredTableSpec spec, ExampleSpec leftValue) {
            var allowed = new HashSet<int>();
            foreach (var example in leftValue.Examples) {
                List<string[]> candidate = example.Value as List<string[]>;
                for (int i=0;i<candidate[0].Length;i++) {
                    var encountered = new HashSet<string>();
                    foreach (var row in candidate) {
                        if (encountered.Contains(row[i])) {
                            allowed.Add(i);
                            break;
                        } else {
                            encountered.Add(row[i]);
                        }
                    }
                }
            }
            var result = new Dictionary<State, IEnumerable<object>>();
            foreach (var example in spec.CustomTableExamples) {
                State inputState = example.Key;
                var adj = new List<List<int>>();
                foreach (var al in allowed) {
                    int prevlen = adj.Count;
                    for (int i=0;i<prevlen;i++) {
                        var newl = new List<int>(adj[i]);
                        newl.Add(al);
                        adj.Add(newl);
                    }
                    adj.Add(new List<int>{al});
                }
                if (adj.Count==0) return null;
                Console.Out.WriteLine("oaisdfj {0}",adj.Count);
                result[inputState] = adj.Cast<object>();
            }
            return new DisjunctiveExamplesSpec(result);
        }


        [WitnessFunction(nameof(Semantics.Group), 2, DependsOnParameters = new[] { 0, 1 }, Verify = true)]
        internal DisjunctiveExamplesSpec WitnessGroup3(GrammarRule rule, DisjunctiveDoubleFilteredTableSpec spec, ExampleSpec leftValue, ExampleSpec centerval)
        {
            var result = new Dictionary<State, IEnumerable<object>>();
            foreach (var example in spec.CustomTableExamples)
            {
                State inputState = example.Key;
                var ks = new List<Tuple<int, bool>>();
                var x_l = centerval.Examples[inputState] as List<int>;
                var x = (leftValue.Examples[inputState] as List<string[]>)[0].Length;
                for (int h = 0; h < x; h++) {
                    if (x_l.Contains(h)) continue;
                    ks.Add(new Tuple<int, bool>(h,true));
                    ks.Add(new Tuple<int, bool>(h,false));
                }
                if (ks.Count == 0) return null;
                result[inputState] = ks.Cast<object>();
            }
            return new DisjunctiveExamplesSpec(result);
        }

    }
}
