﻿using Microsoft.ProgramSynthesis;
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
            return a+b-1;
        }
        [FeatureCalculator(nameof(Semantics.Project))]
        public static double Project(double a,double b) {
            return a-1;
        }



        [FeatureCalculator(nameof(Semantics.Order))]
        public static double Order(double a,double u) {
            return a;
        }
        [FeatureCalculator(nameof(Semantics.Select))]
        public static double Select(double a) {
            return a;
        }
        [FeatureCalculator(nameof(Semantics.Join))]
        public static double Join(double a,double wa,double b,double wb) {
            return a+b-1;
        }
        // [FeatureCalculator(nameof(Semantics.LeftJoin))]
        // public static double LeftJoin(double a,double wa,double b,double wb) {
        //     return a+b;
        // }
        [FeatureCalculator(nameof(Semantics.Group))]
        public static double Group(double a,double u,double v) {
            return a+u+v;
        }



        [FeatureCalculator(nameof(Semantics.N1))]
        public static double N1(double a) {return a;}
        [FeatureCalculator(nameof(Semantics.N2))]
        public static double N2(double a) {return a;}
        [FeatureCalculator(nameof(Semantics.N3))]
        public static double N3(double a) {return a;}
        [FeatureCalculator(nameof(Semantics.N4))]
        public static double N4(double a) {return a;}






        [FeatureCalculator("tableIndex", Method = CalculationMethod.FromLiteral)]
        public static double tableIndex(int index) { return 0; }

        [FeatureCalculator("projectionList")]
        public static double projectionList() { return 0; }
        [FeatureCalculator("joiningLeftColumn")]
        public static double joiningLeftColumn() { return 0; }
        [FeatureCalculator("joiningRightColumn")]
        public static double joiningRightColumn() { return 0; }

        [FeatureCalculator("sortingCriteria")]
        public static double sortingCriteria() { return 0; }
        [FeatureCalculator("groupingColumns")]
        public static double groupingColumns() { return 0; }
        [FeatureCalculator("groupingAggregations")]
        public static double groupingAggregations() { return 0; }
    }
}