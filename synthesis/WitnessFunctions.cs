using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Xml.Linq;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.Learning;
using Microsoft.ProgramSynthesis.Rules;
using Microsoft.ProgramSynthesis.Specifications;
using Microsoft.ProgramSynthesis.Specifications.Serialization;
using System.Collections.ObjectModel;

// Version of specification to use
using Rest560SpecV1;

namespace Rest560
{

    public class WitnessFunctions : DomainLearningLogic {
        public WitnessFunctions(Grammar grammar) : base(grammar) { }

        //-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
        //Closing the spec object on the left is typically easier.
        //-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
        [WitnessFunction(nameof(Semantics.Project), 0)]
        internal DisjunctiveDoubleFilteredTableSpec WitnessProject1(GrammarRule rule, ExampleSpec spec) {
            return DisjunctiveDoubleFilteredTableSpec.FromConcrete(spec);
        }
        [WitnessFunction(nameof(Semantics.Order), 0)]
        internal DisjunctiveDoubleFilteredTableSpec WitnessOrder1(GrammarRule rule, DisjunctiveDoubleFilteredTableSpec spec) {
            return spec;
        }
        [WitnessFunction(nameof(Semantics.Select), 0)]
        internal DisjunctiveDoubleFilteredTableSpec WitnessSelect1(GrammarRule rule, DisjunctiveDoubleFilteredTableSpec spec) {
            return spec;
        }
        [WitnessFunction(nameof(Semantics.Join), 0)]
        internal DisjunctiveDoubleFilteredTableSpec WitnessJoin1(GrammarRule rule, DisjunctiveDoubleFilteredTableSpec spec) {
            return spec.SplitColumnwise();
        }
        // [WitnessFunction(nameof(Semantics.Group), 0)]
        // internal DisjunctiveDoubleFilteredTableSpec WitnessGroup1(GrammarRule rule, DisjunctiveDoubleFilteredTableSpec spec) {
        //     return spec;
        // }



        //-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
        //Closing the spec object on the right is harder and always depends on the value on the left (for our application)
        //-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
        //for Project, checking to see that the example satisfies the spec results in a set of possible mappings. We yield this set.
        [WitnessFunction(nameof(Semantics.Project), 1, DependsOnParameters = new[] { 0 }, Verify = true)]
        internal DisjunctiveExamplesSpec WitnessProject2(GrammarRule rule, ExampleSpec spec, ExampleSpec leftValue) {
            var result = new Dictionary<State, IEnumerable<object>>();
            foreach (var example in spec.Examples) {
                State inputState = example.Key;
                var possiblemappings = new DisjunctiveDoubleFilteredTableSpec(new Dictionary<State, List<string[][]>[]>{{
                    inputState,
                    new List<string[][]>[]{
                        DisjunctiveDoubleFilteredTableSpec.ConvertConcrete(example.Value as List<string[]>)
                    }
                }}).extractPossibleColumnMappings(inputState,leftValue.Examples[inputState]);
                if (possiblemappings.Count == 0) return null;
                result[inputState] = possiblemappings.Cast<object>();
            }
            return new DisjunctiveExamplesSpec(result);
        }

        //For select, we use the concrete value of the table on the left to spin off a different type of spec object on the right.
        [WitnessFunction(nameof(Semantics.Select), 1, DependsOnParameters = new[] { 0 })]
        internal DisjunctiveCriteriaSatSpec WitnessSelect2(GrammarRule rule, DisjunctiveDoubleFilteredTableSpec spec, ExampleSpec leftValue) {
            var result = new Dictionary<State, List<List<string[]>>[]>();
            foreach (var example in spec.CustomTableExamples) {
                State inputState = example.Key;
                result[inputState] = spec.GetFilterCriteria(inputState,leftValue.Examples[inputState]);
            }
            return new DisjunctiveCriteriaSatSpec(null,result);
        }
        //Ordering is similar to selecting.
        [WitnessFunction(nameof(Semantics.Order), 1, DependsOnParameters = new[] { 0 })]
        internal PossibleOrderingsSpec WitnessOrder2(GrammarRule rule, DisjunctiveDoubleFilteredTableSpec spec, ExampleSpec leftValue) {
            Console.Out.WriteLine("Witness ORDER right");
            var result = new Dictionary<State, (List<List<string[]>>[] Examples,HashSet<int> Available)>();
            foreach (var example in spec.CustomTableExamples) {
                State inputState = example.Key;
                result[inputState] = spec.prepareOrderingSpec(inputState,leftValue.Examples[inputState]);
                if (result[inputState].Item1.Length==0) {
                    // Console.Out.WriteLine("a little worrying?");
                    return null;
                }
                //  else {
                //     Console.Out.WriteLine("well, one passed");
                // }
            }
            return new PossibleOrderingsSpec(result);
        }


        [WitnessFunction(nameof(Semantics.One), 0)]
        internal DisjunctiveExamplesSpec WitnessOne1(GrammarRule rule, DisjunctiveCriteriaSatSpec spec) {
            var result = new Dictionary<State, IEnumerable<object>>();
            foreach (var example in spec.SatExamples) {
                State inputState = example.Key;
                var possiblecriterion = spec.GetSatisfiers(inputState);//ew List<Tuple<int,int,int>>{new Tuple<int,int,int>(0,0,0)};//
                if (possiblecriterion.Count == 0) return null;
                result[inputState] = possiblecriterion.Cast<object>();
            }
            return new DisjunctiveExamplesSpec(result);
        }
        // [WitnessFunction(nameof(Semantics.More), 0)]
        // internal DisjunctiveExamplesSpec WitnessMore1(GrammarRule rule, DisjunctiveCriteriaSatSpec spec) {
        //     Console.Out.WriteLine("Witness MORE left");
        //     var result = new Dictionary<State, IEnumerable<object>>();
        //     foreach (var example in spec.SatExamples) {
        //         State inputState = example.Key;
        //         var possiblecriterion = spec.GetSatisfiers(inputState);
        //         if (possiblecriterion.Count == 0) return null;
        //         result[inputState] = possiblecriterion.Cast<object>();
        //     }
        //     return new DisjunctiveExamplesSpec(result);
        // }
        // [WitnessFunction(nameof(Semantics.More), 1, DependsOnParameters = new[] { 0 })]
        // internal DisjunctiveCriteriaSatSpec WitnessMore2(GrammarRule rule, DisjunctiveCriteriaSatSpec spec, ExampleSpec leftValue) {
        //     Console.Out.WriteLine("Witness MORE right");
        //     var result1 = new Dictionary<State, Tuple<int,int,int>>();
        //     var result2 = new Dictionary<State, List<List<string[]>>[]>();
        //     foreach (var example in spec.SatExamples) {
        //         State inputState = example.Key;
        //         result2[inputState] = example.Value;
        //         result1[inputState] = leftValue.Examples[inputState] as Tuple<int,int,int>;
        //     }
        //     return new DisjunctiveCriteriaSatSpec(result1,result2);
        // }




        [WitnessFunction(nameof(Semantics.OneKey), 0)]
        internal DisjunctiveExamplesSpec WitnessOneKey1(GrammarRule rule, PossibleOrderingsSpec spec) {
            Console.Out.WriteLine("Witness ONEKey left");
            var result = new Dictionary<State, IEnumerable<object>>();
            foreach (var example in spec.OrdExamples) {
                State inputState = example.Key;
                var possiblecriterion = spec.GetSatisfiers(inputState);//ew List<Tuple<int,int,int>>{new Tuple<int,int,int>(0,0,0)};//
                if (possiblecriterion.Count == 0) return null;
                result[inputState] = possiblecriterion.Cast<object>();
            }
            return new DisjunctiveExamplesSpec(result);
        }

        [WitnessFunction(nameof(Semantics.OneKey), 0)]
        internal DisjunctiveExamplesSpec WitnessMoreKey1(GrammarRule rule, PossibleOrderingsSpec spec) {
            Console.Out.WriteLine("Witness MOREKey left");
            var result = new Dictionary<State, IEnumerable<object>>();
            foreach (var example in spec.OrdExamples) {
                State inputState = example.Key;
                var possiblecriterion = spec.GetSatisfiers(inputState);//ew List<Tuple<int,int,int>>{new Tuple<int,int,int>(0,0,0)};//
                if (possiblecriterion.Count == 0) return null;
                result[inputState] = possiblecriterion.Cast<object>();
            }
            return new DisjunctiveExamplesSpec(result);
        }
        [WitnessFunction(nameof(Semantics.MoreKey), 1, DependsOnParameters = new[] { 0 })]
        internal PossibleOrderingsSpec WitnessMoreKey2(GrammarRule rule, PossibleOrderingsSpec spec, ExampleSpec leftValue) {
            Console.Out.WriteLine("Witness MOREKey right");
            var result = new Dictionary<State, (List<List<string[]>>[] Examples,HashSet<int> Available)>();
            foreach (var example in spec.OrdExamples) {
                State inputState = example.Key;
                result[inputState] = spec.sortBy(inputState,leftValue.Examples[inputState] as Tuple<int,bool>);
            }
            return new PossibleOrderingsSpec(result);
        }








        //for the right side of join, a lot must be performed.
        //A simplified description is this: First, we take the example from the left and figure out how many columns it may satisfy.
        //Then, we take the remaining columns and append a column that corresponds to the joining column, and yeild that as the spec.
        [WitnessFunction(nameof(Semantics.Join), 2, DependsOnParameters = new[] { 0,1 })]
        internal DisjunctiveDoubleFilteredTableSpec WitnessJoin3(GrammarRule rule, DisjunctiveDoubleFilteredTableSpec spec, ExampleSpec leftValue,ExampleSpec firstcolumn) {
            var result = new Dictionary<State, List<string[][]>[]>();
            foreach (var example in spec.CustomTableExamples) {
                State inputState = example.Key;
                int leftJoinColumn = (int)(firstcolumn.Examples[inputState] as int?);
                result[inputState] = spec.addJoiningColumnToSpec(inputState,leftValue.Examples[inputState],leftJoinColumn);
                if (result[inputState].Length==0) return null;
            }
            return new DisjunctiveDoubleFilteredTableSpec(result);
        }
        // //the right column of join is retrieved in a similar fashion to WitnessProject2.
        [WitnessFunction(nameof(Semantics.Join), 3, DependsOnParameters = new[] { 0,1,2 })]
        internal DisjunctiveExamplesSpec WitnessJoin4(GrammarRule rule, DisjunctiveDoubleFilteredTableSpec spec, ExampleSpec leftValue,ExampleSpec firstcolumn,ExampleSpec rightValue) {
            var result = new Dictionary<State, IEnumerable<object>>();
            foreach (var example in spec.CustomTableExamples) {
                State inputState = example.Key;
                int leftJoinColumn = (int)(firstcolumn.Examples[inputState] as int?);
                var ks = new DisjunctiveDoubleFilteredTableSpec(new Dictionary<State, List<string[][]>[]> {
                    {inputState,spec.addJoiningColumnToSpec(inputState,leftValue.Examples[inputState],leftJoinColumn)}
                }).extractPossibleSingularLastColumnMappings(inputState,rightValue.Examples[inputState]);
                if (ks.Count == 0) return null;
                result[inputState] = ks.Cast<object>();
            }
            return new DisjunctiveExamplesSpec(result);
        }


        //-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
        //these are trivial cases where you just yield a static set of values and check their applicability later
        //-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
        [WitnessFunction(nameof(Semantics.Named), 1, Verify=true)]//this one is checked by verify=True
        internal DisjunctiveExamplesSpec WitnessNamed2(GrammarRule rule, DisjunctiveDoubleFilteredTableSpec spec) {
            var result = new Dictionary<State, IEnumerable<object>>();
            foreach (var example in spec.CustomTableExamples) {
                State inputState = example.Key;
                var ks = new List<int>();
                var x = inputState[rule.Body[0]] as List<List<string[]>>;
                for (int h=0;h<x.Count;h++) ks.Add(h);
                if (ks.Count == 0) return null;
                result[inputState] = ks.Cast<object>();
            }
            return new DisjunctiveExamplesSpec(result);
        }
        [WitnessFunction(nameof(Semantics.Named), 1, Verify=true)]//this one is checked by verify=True
        internal DisjunctiveExamplesSpec WitnessNamed2(GrammarRule rule, ExampleSpec spec) {
            var result = new Dictionary<State, IEnumerable<object>>();
            foreach (var example in spec.Examples) {
                State inputState = example.Key;
                var ks = new List<int>();
                var x = inputState[rule.Body[0]] as List<List<string[]>>;
                for (int h=0;h<x.Count;h++) ks.Add(h);
                if (ks.Count == 0) return null;
                result[inputState] = ks.Cast<object>();
            }
            return new DisjunctiveExamplesSpec(result);
        }
        //for the left column of join, we just yield each possible column, and the answer gets checked by witness functions 3 and 4 of join.
        [WitnessFunction(nameof(Semantics.Join), 1, DependsOnParameters = new[] { 0 })]
        internal DisjunctiveExamplesSpec WitnessJoin2(GrammarRule rule, DisjunctiveDoubleFilteredTableSpec spec, ExampleSpec leftValue) {
            var result = new Dictionary<State, IEnumerable<object>>();
            foreach (var example in spec.CustomTableExamples) {
                State inputState = example.Key;
                var ks = new List<int>();
                var x = (leftValue.Examples[inputState] as List<string[]>)[0].Length;
                for (int h=0;h<x;h++) ks.Add(h);
                if (ks.Count == 0) return null;
                result[inputState] = ks.Cast<object>();
            }
            return new DisjunctiveExamplesSpec(result);
        }





        //these are the cases where each tier just falls through to the next tier.
        [WitnessFunction(nameof(Semantics.N1), 0)]
        internal DisjunctiveDoubleFilteredTableSpec WitnessN1(GrammarRule rule, DisjunctiveDoubleFilteredTableSpec spec) {return spec;}
        [WitnessFunction(nameof(Semantics.N2), 0)]
        internal DisjunctiveDoubleFilteredTableSpec WitnessN2(GrammarRule rule, DisjunctiveDoubleFilteredTableSpec spec) {return spec;}
        [WitnessFunction(nameof(Semantics.N3), 0)]
        internal DisjunctiveDoubleFilteredTableSpec WitnessN3(GrammarRule rule, DisjunctiveDoubleFilteredTableSpec spec) {return spec;}
        [WitnessFunction(nameof(Semantics.N4), 0)]
        internal DisjunctiveDoubleFilteredTableSpec WitnessN4(GrammarRule rule, DisjunctiveDoubleFilteredTableSpec spec) {return spec;}

        [WitnessFunction(nameof(Semantics.N1), 0)]
        internal ExampleSpec WitnessN1(GrammarRule rule, ExampleSpec spec) {return spec;}
        [WitnessFunction(nameof(Semantics.N2), 0)]
        internal ExampleSpec WitnessN2(GrammarRule rule, ExampleSpec spec) {return spec;}
        [WitnessFunction(nameof(Semantics.N3), 0)]
        internal ExampleSpec WitnessN3(GrammarRule rule, ExampleSpec spec) {return spec;}
        [WitnessFunction(nameof(Semantics.N4), 0)]
        internal ExampleSpec WitnessN4(GrammarRule rule, ExampleSpec spec) {return spec;}
    }
}
