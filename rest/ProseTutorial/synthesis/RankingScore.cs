using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.AST;
using Microsoft.ProgramSynthesis.Features;
using System.Collections.Generic;


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
        [FeatureCalculator(nameof(Semantics.Project))]
        public static double Project(double a,double b) {
            return a+1;
        }
        [FeatureCalculator("tableIndex", Method = CalculationMethod.FromLiteral)]
        public static double tableIndex(int index) { return index; }

        [FeatureCalculator("projectionList")]
        public static double projectionList() { return 0; }
    }
}