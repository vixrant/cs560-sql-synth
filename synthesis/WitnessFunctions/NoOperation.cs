using Microsoft.ProgramSynthesis.Learning;
using Microsoft.ProgramSynthesis.Rules;
using Microsoft.ProgramSynthesis.Specifications;

using Rest560SpecV1;

namespace Rest560
{
    public partial class WitnessFunctions : DomainLearningLogic
    {
        //these are the cases where each tier just falls through to the next tier.
        [WitnessFunction(nameof(Semantics.N1), 0)]
        internal DisjunctiveDoubleFilteredTableSpec WitnessN1(GrammarRule rule, DisjunctiveDoubleFilteredTableSpec spec) { return spec; }
        [WitnessFunction(nameof(Semantics.N2), 0)]
        internal DisjunctiveDoubleFilteredTableSpec WitnessN2(GrammarRule rule, DisjunctiveDoubleFilteredTableSpec spec) { return spec; }
        [WitnessFunction(nameof(Semantics.N3), 0)]
        internal DisjunctiveDoubleFilteredTableSpec WitnessN3(GrammarRule rule, DisjunctiveDoubleFilteredTableSpec spec) { return spec; }
        [WitnessFunction(nameof(Semantics.N4), 0)]
        internal DisjunctiveDoubleFilteredTableSpec WitnessN4(GrammarRule rule, DisjunctiveDoubleFilteredTableSpec spec) { return spec; }

        [WitnessFunction(nameof(Semantics.N1), 0)]
        internal ExampleSpec WitnessN1(GrammarRule rule, ExampleSpec spec) { return spec; }
        [WitnessFunction(nameof(Semantics.N2), 0)]
        internal ExampleSpec WitnessN2(GrammarRule rule, ExampleSpec spec) { return spec; }
        [WitnessFunction(nameof(Semantics.N3), 0)]
        internal ExampleSpec WitnessN3(GrammarRule rule, ExampleSpec spec) { return spec; }
        [WitnessFunction(nameof(Semantics.N4), 0)]
        internal ExampleSpec WitnessN4(GrammarRule rule, ExampleSpec spec) { return spec; }
    }
}
