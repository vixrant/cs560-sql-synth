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
        //-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
        //these are trivial cases where you just yield a static set of values and check their applicability later
        //-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
        [WitnessFunction(nameof(Semantics.Named), 1, Verify = true)]//this one is checked by verify=True
        internal DisjunctiveExamplesSpec WitnessNamed2(GrammarRule rule, DisjunctiveDoubleFilteredTableSpec spec)
        {
            var result = new Dictionary<State, IEnumerable<object>>();
            foreach (var example in spec.CustomTableExamples)
            {
                State inputState = example.Key;
                var ks = new List<int>();
                var x = inputState[rule.Body[0]] as List<List<string[]>>;
                for (int h = 0; h < x.Count; h++) ks.Add(h);
                if (ks.Count == 0) return null;
                result[inputState] = ks.Cast<object>();
            }
            return new DisjunctiveExamplesSpec(result);
        }

        [WitnessFunction(nameof(Semantics.Named), 1, Verify = true)]//this one is checked by verify=True
        internal DisjunctiveExamplesSpec WitnessNamed2(GrammarRule rule, ExampleSpec spec)
        {
            var result = new Dictionary<State, IEnumerable<object>>();
            foreach (var example in spec.Examples)
            {
                State inputState = example.Key;
                var ks = new List<int>();
                var x = inputState[rule.Body[0]] as List<List<string[]>>;
                for (int h = 0; h < x.Count; h++) ks.Add(h);
                if (ks.Count == 0) return null;
                result[inputState] = ks.Cast<object>();
            }
            return new DisjunctiveExamplesSpec(result);
        }
    }
}
