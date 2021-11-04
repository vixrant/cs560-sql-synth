using System;
// using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Xml.Linq;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.Learning;
using Microsoft.ProgramSynthesis.Rules;
using Microsoft.ProgramSynthesis.Specifications;
using Microsoft.ProgramSynthesis.Specifications.Serialization;

public class UniqueQueue<T> {
    private HashSet<T> hashSet;
    private Queue<T> queue;
    public UniqueQueue() {
        hashSet = new HashSet<T>();
        queue = new Queue<T>();
    }
    public int Count { get {return hashSet.Count;} }
    public void Enqueue(T item) { if (hashSet.Add(item)) queue.Enqueue(item); }//pretty much O(1)
    public T Dequeue() {//pretty much O(1)
        T item = queue.Dequeue();
        hashSet.Remove(item);
        return item;
    }
}





abstract class CustomSpec : Spec {
    public IDictionary<State, object> CustomTableExamples;
    public CustomSpec(IDictionary<State, object> CustomTableExamples): base(CustomTableExamples.Keys) {
        this.CustomTableExamples = CustomTableExamples;
    }
    protected override int GetHashCodeOnInput(State state) {
        return this.CustomTableExamples[state].GetHashCode();
    }
    protected override Spec TransformInputs(Func<State, State> f) {
			var result = new Dictionary<State, object>();
			foreach (var input in this.CustomTableExamples.Keys) {
				result[f(input)] = this.CustomTableExamples[input];
			}
			return new DoubleFilteredTableSpec(result);
    }
    protected override bool EqualsOnInput(State state,Spec spec) {
        throw new NotImplementedException();
    }
    protected override XElement SerializeImpl(Dictionary<object, int> statespace, SpecSerializationContext context) {
        throw new NotImplementedException();
    }
    protected override XElement InputToXML(State state,Dictionary<object, int> statespace) {
        throw new NotImplementedException();
    }
}

// Microsoft.ProgramSynthesis.Specifications.OutputNotNullSpec



//This checks to see if there exists a mapping from space's rows to candidate's rows, and
//space's columns to candidate's columns, such that for each element inside of space (row,column,value),
//this holds:  candidate[rowmapping[row]][columnmapping[column]]==value
//
//This spec is chosen with the understanding that rows may be filtered out of candidate (by FILTER or GROUP), and the order of the rows may be changed (by ORDER),
//and also that a few columns will be selected, with their order changed as well (by PROJECT)
class DoubleFilteredTableSpec : CustomSpec {
    public DoubleFilteredTableSpec(IDictionary<State, object> CustomTableExamples): base(CustomTableExamples) {
        this.CustomTableExamples = CustomTableExamples;
    }
    private List<(int Row,int Col)>[,] GetSettlePoint(State state,object obj) {
        var space = this.CustomTableExamples[state] as List<string[]>;
        var candidate = obj as List<string[]>;
        var governhash = new Dictionary<string,List<(int Row,int Col)>>();
        Console.Out.WriteLine("I WAS CALLED!");
        for (int crow=0;crow<candidate.Count;crow++) {
            for (int ccol=0;ccol<candidate[0].Length;ccol++) {
                List<(int Row,int Col)> value = null;
                var vval = candidate[crow][ccol];
                if (governhash.TryGetValue(vval,out value)) {
                    value.Add((crow,ccol));
                } else {
                    governhash[vval] = new List<(int Row,int Col)>{(crow,ccol)};
                }
            }
        }
        int rowcount = space.Count;
        int colcount = space[0].Length;
        var intermed = new List<(int Row,int Col)>[rowcount,colcount];
        for (int row=0;row<rowcount;row++) {
            for (int col=0;col<colcount;col++) {
                List<(int Row,int Col)> govr = null;
                if (!governhash.TryGetValue(space[0][col],out govr)) return null;
                intermed[row,col] = govr.ConvertAll(x=>x);
            }
        }
        var columnWork = new UniqueQueue<int>();
        var rowWork = new UniqueQueue<int>();
        for (int row=0;row<rowcount;row++) rowWork.Enqueue(row);
        for (int col=0;col<colcount;col++) columnWork.Enqueue(col);
        while (columnWork.Count>0 || rowWork.Count>0) {
            while (columnWork.Count>0) {
                var col = columnWork.Dequeue();
                var intersection = new HashSet<int>();
                foreach ((int Row,int Col) in intermed[0,col]) intersection.Add(Col);
                for (int row=1;row<rowcount;row++) {
                    var next = new HashSet<int>();
                    foreach ((int Row,int Col) in intermed[row,col]) {
                        if (intersection.Contains(Col)) next.Add(Col);
                    }
                    if (next.Count==0) return null;
                    intersection = next;
                }
                for (int row=0;row<rowcount;row++) {
                    var djlist = intermed[row,col];
                    for (int i=djlist.Count-1;i>=0;i--) {
                        if (!intersection.Contains(djlist[i].Col)) {
                            rowWork.Enqueue(djlist[i].Row);
                            djlist[i]=djlist[djlist.Count-1];//removes in O(1), sacrificing order stability
                            djlist.RemoveAt(djlist.Count-1);
                        }
                    }
                }
            }
            while (rowWork.Count>0) {
                var row = rowWork.Dequeue();
                var intersection = new HashSet<int>();
                foreach ((int Row,int Col) in intermed[row,0]) intersection.Add(Row);
                for (int col=1;col<colcount;col++) {
                    var next = new HashSet<int>();
                    foreach ((int Row,int Col) in intermed[row,col]) {
                        if (intersection.Contains(Row)) next.Add(Row);
                    }
                    if (next.Count==0) return null;
                    intersection = next;
                }
                for (int col=0;col<colcount;col++) {
                    var djlist = intermed[row,col];
                    for (int i=djlist.Count-1;i>=0;i--) {
                        if (!intersection.Contains(djlist[i].Row)) {
                            columnWork.Enqueue(djlist[i].Col);
                            djlist[i]=djlist[djlist.Count-1];//removes in O(1), sacrificing order stability
                            djlist.RemoveAt(djlist.Count-1);
                        }
                    }
                }
            }
        }
        return intermed;
    }
    protected override bool CorrectOnProvided(State state,object obj) {
        return GetSettlePoint(state,obj)!=null;
    }
    public List<List<int>> extractPossibleColumnMappings(State state,object obj) {
        var settle = GetSettlePoint(state,obj);
        int colcount = settle.GetLength(1);
        var result = new List<List<int>>{new List<int>{}};
        for (int col=0;col<colcount;col++) {
            var intersection = new HashSet<int>();//eliminates duplicates
            foreach ((int Row,int Col) in settle[0,col]) intersection.Add(Col);
            var next = new List<List<int>>{};
            foreach (List<int> partial in result) {
                foreach (int pos in intersection) {
                    var newpartial = partial.ConvertAll(x=>x);
                    newpartial.Add(pos);
                    next.Add(newpartial);
                }
            }
            result = next;
        }
        return result;
    }
}















namespace ProseTutorial {
    public class WitnessFunctions : DomainLearningLogic {
        public WitnessFunctions(Grammar grammar) : base(grammar) { }


        [WitnessFunction(nameof(Semantics.Project), 0)]
        internal DoubleFilteredTableSpec WitnessProject1(GrammarRule rule, ExampleSpec spec) {
            Console.Out.WriteLine("WITNESSING PROJECT!");
            var result = new Dictionary<State, object>();
            foreach (var example in spec.Examples) result[example.Key] = example.Value;
            return new DoubleFilteredTableSpec(result);
        }




        [WitnessFunction(nameof(Semantics.Project), 1, DependsOnParameters = new[] { 0 })]
        internal DisjunctiveExamplesSpec WitnessProject2(GrammarRule rule, ExampleSpec spec, ExampleSpec vanish) {
            var result = new Dictionary<State, IEnumerable<object>>();
            foreach (var example in spec.Examples) {
                State inputState = example.Key;
                var possiblemappings = new DoubleFilteredTableSpec(new Dictionary<State, object>{{inputState,example.Value}})
                    .extractPossibleColumnMappings(inputState,vanish.Examples[inputState]);
                if (possiblemappings.Count == 0) return null;
                result[inputState] = possiblemappings.Cast<object>();
            }
            return new DisjunctiveExamplesSpec(result);
        }



        [WitnessFunction(nameof(Semantics.Named), 1, Verify=true)]
        internal DisjunctiveExamplesSpec WitnessNamed2(GrammarRule rule, DoubleFilteredTableSpec spec) {
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


        [WitnessFunction(nameof(Semantics.N1), 0)]
        internal DoubleFilteredTableSpec WitnessN1(GrammarRule rule, DoubleFilteredTableSpec spec) {return spec;}
        [WitnessFunction(nameof(Semantics.N2), 0)]
        internal DoubleFilteredTableSpec WitnessN2(GrammarRule rule, DoubleFilteredTableSpec spec) {return spec;}
        [WitnessFunction(nameof(Semantics.N3), 0)]
        internal DoubleFilteredTableSpec WitnessN3(GrammarRule rule, DoubleFilteredTableSpec spec) {return spec;}
        [WitnessFunction(nameof(Semantics.N4), 0)]
        internal DoubleFilteredTableSpec WitnessN4(GrammarRule rule, DoubleFilteredTableSpec spec) {return spec;}

    }
}








