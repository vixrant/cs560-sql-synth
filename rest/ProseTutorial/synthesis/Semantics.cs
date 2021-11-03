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
    }
}