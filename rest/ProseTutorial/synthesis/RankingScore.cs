using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.AST;
using Microsoft.ProgramSynthesis.Features;

namespace ProseTutorial
{
    public class RankingScore : Feature<double>
    {
        public RankingScore(Grammar grammar) : base(grammar, "Score")
        {
        }

        protected override double GetFeatureValueForVariable(VariableNode variable) { return 0; }

        [FeatureCalculator(nameof(Semantics.Named))]
        public static double Named(double a,double b) {
            return a+b+1;
        }
        [FeatureCalculator("tableindex", Method = CalculationMethod.FromLiteral)]
        public static double tableindex(int index) { return index; }
    }
}