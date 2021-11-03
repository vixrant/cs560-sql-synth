using System.Collections.Generic;

namespace ProseTutorial
{
    public static class Semantics {
        public static List<string[]> Named(List<List<string[]>> inputs,int selector) {
            return inputs[selector];
        }
        public static List<string[]> Project(List<string[]> downstream,List<int> projection) {
            return downstream;
        }



        public static List<string[]> Order(List<string[]> downstream) {
            return downstream;
        }
        public static List<string[]> Select(List<string[]> downstream) {
            return downstream;
        }
        public static List<string[]> Join(List<string[]> a,int a_col,List<string[]> b,int b_col) {
            return a;
        }
        public static List<string[]> LeftJoin(List<string[]> a,int a_col,List<string[]> b,int b_col) {
            return a;
        }
        public static List<string[]> Group(List<string[]> a) {
            return a;
        }



        public static List<string[]> N1(List<string[]> a) {return a;}
        public static List<string[]> N2(List<string[]> a) {return a;}
        public static List<string[]> N3(List<string[]> a) {return a;}
        public static List<string[]> N4(List<string[]> a) {return a;}
    }
}