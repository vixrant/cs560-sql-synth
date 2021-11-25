using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.Learning;

namespace Rest560
{
    public partial class WitnessFunctions : DomainLearningLogic
    {
        public WitnessFunctions(Grammar grammar) : base(grammar) { }
        /*
        Closing the spec object on the left is typically easier.
            Done for: join1, project1, order1, select1
        Closing the spec object on the right is harder and always depends on the value on the left (for our application)
            Done for: all the rest
        */
        
        // [WitnessFunction(nameof(Semantics.Group), 0)]
        // internal DisjunctiveDoubleFilteredTableSpec WitnessGroup1(GrammarRule rule, DisjunctiveDoubleFilteredTableSpec spec) {
        //     return spec;
        // }
    }
}
