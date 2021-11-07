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
using System.Collections.ObjectModel;
// using Debug;


//need to memo and dedup


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






//This checks to see if there exists a mapping from space's rows to candidate's rows, and
//space's columns to candidate's columns, such that for each element inside of space (row,column,value),
//this holds:  candidate[rowmapping[row]][columnmapping[column]]==value
//
//This spec is chosen with the understanding that rows may be filtered out of candidate (by FILTER or GROUP), and the order of the rows may be changed (by ORDER),
//and also that a few columns will be selected, with their order changed as well (by PROJECT)
class DisjunctiveDoubleFilteredTableSpec : Spec {
    public IDictionary<State, List<string[][]>[]> CustomTableExamples;
    public DisjunctiveDoubleFilteredTableSpec(IDictionary<State, List<string[][]>[]> CustomTableExamples): base(CustomTableExamples.Keys) {
        // int colLen = -1;
        foreach (var w in CustomTableExamples) {
            if (w.Value.Length==0) throw new ArgumentException("No possibilities- null should be returned instead of creating a spec.");
            foreach (var v in w.Value) {
                if (v.Count==0) throw new ArgumentException("No rows- this isn't supported and shouldn't arise from well-formed examples.");
                foreach (var u in v) {
                    if (u.Length==0) throw new ArgumentException("No columns- this isn't supported and shouldn't arise from well-formed examples.");
                    foreach (var j in u) {
                        if (j.Length==0) throw new ArgumentException("A row/column pair supports exactly 0 possibilities- the possibility shouldn't have been yielded.");
                    }
                }
            }
        }
        // foreach (var w in CustomTableExamples) {
        //     foreach (var v in w.Value) {
        //         foreach (var u in v) {
        //             if (u.Length!=colLen) throw new ArgumentException("Different disjunct possibilities have a different number of columns- this shouldn't arise.");
        //         }
        //     }
        // }
        this.CustomTableExamples = CustomTableExamples;
    }
    public DisjunctiveDoubleFilteredTableSpec SplitColumnwise() {
        var inner = new Dictionary<State, List<string[][]>[]>();
        foreach (var example in CustomTableExamples) {
            State inputState = example.Key;
            var fullTables = example.Value as List<string[][]>[];
            int total=0;
            foreach (var possib in fullTables) total+=possib[0].Length;
            var newtables = new List<string[][]>[total];
            int dest=0;
            for (int k=0;k<fullTables.Length;k++) {
                for (int i=0;i<fullTables[k][0].Length;i++) {
                    var newtable = new List<string[][]>();
                    newtables[dest++] = newtable;
                    for (int j=0;j<fullTables[k].Count;j++) newtable.Add(new string[][]{fullTables[k][j][i]});
                }
            }
            inner[inputState] = newtables;
        }
        return new DisjunctiveDoubleFilteredTableSpec(inner);
    }
    public static DisjunctiveDoubleFilteredTableSpec FromConcrete(ExampleSpec spec) {
        var result = new Dictionary<State, List<string[][]>[]>();
        foreach (var example in spec.Examples) result[example.Key] = new List<string[][]>[]{
            DisjunctiveDoubleFilteredTableSpec.ConvertConcrete(example.Value as List<string[]>)
        };
        return new DisjunctiveDoubleFilteredTableSpec(result);
    }
    public static List<string[][]> ConvertConcrete(List<string[]> table) {
        var outp = new List<string[][]>();
        foreach (string[] row in table) {
            var newrow = new string[row.Length][];
            for (int i=0;i<row.Length;i++) newrow[i] = new string[]{row[i]};
            outp.Add(newrow);
        }
        return outp;
    }
    private List<List<(int Row,int Col)>[,]> GetSettleInitial(State state,object obj,bool abortOnEmpty) {
        Console.Out.WriteLine("GetSettleInitial WAS CALLED!");
        var result = new List<List<(int Row,int Col)>[,]>();
        foreach (List<string[][]> space in this.CustomTableExamples[state]) {
            var candidate = obj as List<string[]>;
            var governhash = new Dictionary<string,List<(int Row,int Col)>>();
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
            bool fullbreak = false;
            for (int row=0;row<rowcount;row++) {
                for (int col=0;col<colcount;col++) {
                    var imd = new List<(int Row,int Col)>();
                    intermed[row,col] = imd;
                    foreach (string pos in space[0][col]) {
                        List<(int Row,int Col)> govr = null;
                        if (governhash.TryGetValue(pos,out govr)) {
                            foreach ((int,int) bs in govr) imd.Add(bs);
                        }
                    }
                    if (abortOnEmpty && imd.Count==0) {fullbreak=true;break;}
                }
                if (fullbreak) break;
            }
            if (fullbreak) continue;
            result.Add(intermed);
        }
        return result;
    }
    private static bool GetSettlePoint(List<(int Row,int Col)>[,] intermed) {
        int rowcount = intermed.GetLength(0);
        int colcount = intermed.GetLength(1);
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
                    if (next.Count==0) return false;
                    intersection = next;
                }
                for (int row=0;row<rowcount;row++) {
                    var djlist = intermed[row,col];
                    for (int i=djlist.Count-1;i>=0;i--) {
                        if (!intersection.Contains(djlist[i].Col)) {
                            rowWork.Enqueue(row);
                            djlist[i]=djlist[djlist.Count-1];//removes in O(1), sacrificing order stability
                            djlist.RemoveAt(djlist.Count-1);
                        }
                    }
                }
            }
            while (rowWork.Count>0) {
                var row = rowWork.Dequeue();
                var intersection = new HashSet<int>();
                foreach ((int Row,int Col) in intermed[row,0]) {intersection.Add(Row);}
                for (int col=1;col<colcount;col++) {
                    var next = new HashSet<int>();
                    foreach ((int Row,int Col) in intermed[row,col]) {
                        if (intersection.Contains(Row)) next.Add(Row);
                    }
                    if (next.Count==0) return false;
                    intersection = next;
                }
                for (int col=0;col<colcount;col++) {
                    var djlist = intermed[row,col];
                    for (int i=djlist.Count-1;i>=0;i--) {
                        if (!intersection.Contains(djlist[i].Row)) {
                            columnWork.Enqueue(col);
                            djlist[i]=djlist[djlist.Count-1];//removes in O(1), sacrificing order stability
                            djlist.RemoveAt(djlist.Count-1);
                        }
                    }
                }
            }
        }
        return true;
    }
    private List<List<(int Row,int Col)>[,]> GetSettlePoints(State state,object obj) {
        var result = new List<List<(int Row,int Col)>[,]>();
        foreach (List<(int Row,int Col)>[,] intermed in GetSettleInitial(state,obj,true)) {
            if (GetSettlePoint(intermed)) result.Add(intermed);
        }
        return result;
    }
    private List<List<(int Row,int Col)>[,]> GetColumnwiseSettlePoints(State state,object obj) {
        var result = new List<List<(int Row,int Col)>[,]>();
        Console.Out.WriteLine("calling GetColumnwiseSettlePoints....................................");
        foreach (List<(int Row,int Col)>[,] intermed in GetSettleInitial(state,obj,false)) {
            Console.Out.WriteLine("yielding one");
            int rowcount = intermed.GetLength(0);
            int colcount = intermed.GetLength(1);
            for (int col=0;col<colcount;col++) {
                var intersection = new HashSet<int>();
                foreach ((int Row,int Col) in intermed[0,col]) intersection.Add(Col);
                for (int row=1;row<rowcount;row++) {
                    var next = new HashSet<int>();
                    foreach ((int Row,int Col) in intermed[row,col]) {
                        if (intersection.Contains(Col)) next.Add(Col);
                    }
                    intersection = next;
                }
                for (int row=0;row<rowcount;row++) {
                    var djlist = intermed[row,col];
                    for (int i=djlist.Count-1;i>=0;i--) {
                        if (!intersection.Contains(djlist[i].Col)) {
                            djlist[i]=djlist[djlist.Count-1];//removes in O(1), sacrificing order stability
                            djlist.RemoveAt(djlist.Count-1);
                        }
                    }
                }
            }
            result.Add(intermed);
        }
        return result;
    }


    protected override bool CorrectOnProvided(State state,object obj) {
        return GetSettlePoints(state,obj).Count>0;
    }
    public List<int> extractPossibleSingularLastColumnMappings(State state,object obj) {
        var result = new List<int>{};
        foreach (var settle in GetSettlePoints(state,obj)) {
            int colcount = settle.GetLength(1);
            foreach ((int Row,int Col) in settle[0,colcount-1]) result.Add(Col);
        }
        return result;
    }
    public List<List<int>> extractPossibleColumnMappings(List<(int Row,int Col)>[,] settle) {
        int colcount = settle.GetLength(1);
        var interim = new List<List<int>>{new List<int>{}};
        for (int col=0;col<colcount;col++) {
            var intersection = new HashSet<int>();//eliminates duplicates
            foreach ((int Row,int Col) in settle[0,col]) intersection.Add(Col);
            var next = new List<List<int>>{};
            foreach (List<int> partial in interim) {
                foreach (int pos in intersection) {
                    var newpartial = partial.ConvertAll(x=>x);
                    newpartial.Add(pos);
                    next.Add(newpartial);
                }
            }
            interim = next;
        }
        return interim;
    }
    public List<List<int>> extractPossibleColumnMappings(State state,object obj) {
        var result = new List<List<int>>{};
        foreach (var settle in GetSettlePoints(state,obj)) {
            result.AddRange(extractPossibleColumnMappings(settle));
        }
        return result;
    }
    public List<List<(string[][] Remaining,HashSet<int> RowAssignments)>> getRowAssignmentsToSatisfyRest(State state,object obj) {
        var result = new List<List<(string[][] Remaining,HashSet<int> RowAssignments)>>();
        var columnwiseGroup = GetColumnwiseSettlePoints(state,obj);
        for(int columnwiseIndex=0;columnwiseIndex<columnwiseGroup.Count;columnwiseIndex++) {
            var columnwise = columnwiseGroup[columnwiseIndex];
            var children = new Dictionary<int,List<int>>();
            var sideways = new Dictionary<int,List<int>>();
            var roots = new List<int>();
            int rowcount = columnwise.GetLength(0);
            int colcount = columnwise.GetLength(1);
            var thinned = new HashSet<int>[rowcount,colcount];
            var nulled = new HashSet<int>();
            for (int col=0;col<colcount;col++) {
                for (int row=0;row<rowcount;row++) {
                    var intersection = new HashSet<int>();//eliminates duplicates
                    foreach ((int Row,int Col) in columnwise[row,col]) intersection.Add(Row);
                    if (intersection.Count==0) nulled.Add(col);
                    thinned[row,col]=intersection;
                }
            }
            var implicationMatrix = new bool[colcount,colcount];
            for (int x=0;x<colcount;x++) {
                if (nulled.Contains(x)) continue;
                for (int y=0;y<colcount;y++) {
                    if (nulled.Contains(y)) continue;
                    var allimp = true;
                    for (int row=0;row<rowcount;row++) {
                        if (!thinned[row,x].IsSubsetOf(thinned[row,y])) {allimp=false;break;}
                    }
                    implicationMatrix[x,y] = allimp;
                }
            }
            var initialassignment = new Dictionary<int, bool>();
            foreach (int nullval in nulled) initialassignment[nullval] = false;
            var assignments = new List<Dictionary<int,bool>>{initialassignment};
            for (int col=0;col<colcount;col++) {
                if (nulled.Contains(col)) continue;
                var next = new List<Dictionary<int,bool>>();
                foreach (Dictionary<int,bool> partial in assignments) {
                    if (partial.ContainsKey(col)) {
                        next.Add(partial);continue;
                    }
                    var workQueue = new Queue<int>();
                    var truepartial = new Dictionary<int,bool>(partial);
                    workQueue.Enqueue(col);
                    var truepartialvalid = true;
                    while (workQueue.Count>0) {
                        var ncol = workQueue.Dequeue();
                        if (truepartial.ContainsKey(ncol)) {
                            if (truepartial[ncol]) continue;
                            truepartialvalid = false; break;
                        }
                        truepartial[ncol] = true;
                        for (int a=0;a<colcount;a++) {
                            if (implicationMatrix[ncol,a]) workQueue.Enqueue(a);
                        }
                    }
                    if (truepartialvalid) next.Add(truepartial);
                    var falsepartial = partial;
                    workQueue.Enqueue(col);
                    var falsepartialvalid = true;
                    while (workQueue.Count>0) {
                        var ncol = workQueue.Dequeue();
                        if (falsepartial.ContainsKey(ncol)) {
                            if (!falsepartial[ncol]) continue;
                            falsepartialvalid = false; break;
                        }
                        falsepartial[ncol] = false;
                        for (int a=0;a<colcount;a++) {
                            if (implicationMatrix[a,ncol]) workQueue.Enqueue(a);
                        }
                    }
                    if (falsepartialvalid) next.Add(falsepartial);
                }
                assignments = next;
            }
            Console.Out.WriteLine("begin printing assignments:");
            foreach (var assignment in assignments) {
                var lines = assignment.Select(kvp => kvp.Key + ": " + kvp.Value.ToString());
                Console.Out.WriteLine("assignment: {0}",string.Join(",", lines));
            }

            var lastphase = new List<(HashSet<int> Remaining,List<HashSet<int>> RowAssignments)>();
            foreach (var assignment in assignments) {
                int total = 0;
                foreach(var pair in assignment) {if (pair.Value) {total++;}}
                if (total==0) continue;
                var newinitial = new List<(int Row, int Col)>[rowcount,total];
                int i=0;
                var re = new HashSet<int>();
                foreach(var pair in assignment) {
                    if (pair.Value) {
                        for (int row=0;row<rowcount;row++) {
                            newinitial[row,i] = columnwise[row,pair.Key];
                        }
                        i++;
                    } else {
                        re.Add(pair.Key);
                    }
                }
                if (GetSettlePoint(newinitial)) {
                    var ra = new List<HashSet<int>>();
                    for (int row=0;row<rowcount;row++) {
                        var intersection = new HashSet<int>();//eliminates duplicates
                        foreach ((int Row,int Col) in newinitial[row,0]) intersection.Add(Col);
                        ra.Add(intersection);
                    }
                    for (int j=0;j<lastphase.Count;j++) {
                        if (lastphase[j].RowAssignments.Count!=ra.Count) continue;
                        var eqa = true;
                        for (int k=0;k<ra.Count;k++) {
                            if (!lastphase[j].RowAssignments[k].SetEquals(ra[k])) {eqa=false;break;}
                        }
                        if (eqa) {lastphase[j].Remaining.IntersectWith(re);break;}
                    }
                    lastphase.Add((re,ra));
                }
            }
            var thistable = CustomTableExamples[state][columnwiseIndex];
            foreach ((HashSet<int> Remaining,List<HashSet<int>> RowAssignments) in lastphase) {
                var OrderedRemain = new List<int>();
                foreach (int ind in Remaining) OrderedRemain.Add(ind);
                var subresult = new List<(string[][] Remaining,HashSet<int> RowAssignments)>();
                for (int row=0;row<rowcount;row++) {
                    var newrow = new string[OrderedRemain.Count()][];
                    for (int c=0;c<OrderedRemain.Count();c++) {
                        newrow[c] = thistable[row][OrderedRemain[c]];
                    }
                    subresult.Add((newrow,RowAssignments[row]));
                }
                result.Add(subresult);
            }
        }
        return result;
    }


    public List<string[][]>[] addJoiningColumnToSpec(State state,object obj,int colId) {
        var reras = getRowAssignmentsToSatisfyRest(state,obj);
        var candidate = obj as List<string[]>;
        var result = new List<string[][]>[reras.Count];
        for (int ri=0;ri<reras.Count;ri++) {
            var subresult = new List<string[][]>();
            foreach ((string[][] Remaining,HashSet<int> RowAssignments) in reras[ri]) {
                var z = new string[Remaining.Length + 1][];
                Remaining.CopyTo(z,0);
                var w = new string[RowAssignments.Count];
                int i=0;
                foreach (var ra in RowAssignments) {
                    w[i]=candidate[ra][colId];i++;
                }
                z[Remaining.Length] = w;
                subresult.Add(z);
            }
            result[ri]=subresult;
        }
        return result;
    }


    protected override int GetHashCodeOnInput(State state) {
        return this.CustomTableExamples[state].GetHashCode();
    }
    protected override Spec TransformInputs(Func<State, State> f) {
        var result = new Dictionary<State, List<string[][]>[]>();
        foreach (var input in this.CustomTableExamples.Keys) {
            result[f(input)] = this.CustomTableExamples[input];
        }
        return new DisjunctiveDoubleFilteredTableSpec(result);
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















namespace ProseTutorial {
    public class WitnessFunctions : DomainLearningLogic {
        public WitnessFunctions(Grammar grammar) : base(grammar) { }

        //-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
        //Closing the spec object on the left is typically easier.
        //-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
        [WitnessFunction(nameof(Semantics.Project), 0)]
        internal DisjunctiveDoubleFilteredTableSpec WitnessProject1(GrammarRule rule, ExampleSpec spec) {
            Console.Out.WriteLine("WITNESS PROJECT LEFT");
            return DisjunctiveDoubleFilteredTableSpec.FromConcrete(spec);
        }
        [WitnessFunction(nameof(Semantics.Order), 0)]
        internal DisjunctiveDoubleFilteredTableSpec WitnessOrder1(GrammarRule rule, DisjunctiveDoubleFilteredTableSpec spec) {
            Console.Out.WriteLine("WITNESS ORDER LEFT");
            return spec;
        }
        [WitnessFunction(nameof(Semantics.Select), 0)]
        internal DisjunctiveDoubleFilteredTableSpec WitnessSelect1(GrammarRule rule, DisjunctiveDoubleFilteredTableSpec spec) {
            Console.Out.WriteLine("WITNESS SELECT LEFT");
            return spec;
        }
        [WitnessFunction(nameof(Semantics.Join), 0)]
        internal DisjunctiveDoubleFilteredTableSpec WitnessJoin1(GrammarRule rule, DisjunctiveDoubleFilteredTableSpec spec) {
            Console.Out.WriteLine("WITNESS JOIN LEFT");
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
        [WitnessFunction(nameof(Semantics.Project), 1, DependsOnParameters = new[] { 0 })]
        internal DisjunctiveExamplesSpec WitnessProject2(GrammarRule rule, ExampleSpec spec, ExampleSpec leftValue) {
            Console.Out.WriteLine("WITNESS PROJECT RIGHT");
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
        //for the right side of join, a lot must be performed.
        //A simplified description is this: First, we take the example from the left and figure out how many columns it may satisfy.
        //Then, we take the remaining columns and append a column that corresponds to the joining column, and yeild that as the spec.
        [WitnessFunction(nameof(Semantics.Join), 2, DependsOnParameters = new[] { 0,1 })]
        internal DisjunctiveDoubleFilteredTableSpec WitnessJoin3(GrammarRule rule, DisjunctiveDoubleFilteredTableSpec spec, ExampleSpec leftValue,ExampleSpec firstcolumn) {
            Console.Out.WriteLine("WITNESS JOIN RIGHT");
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
            Console.Out.WriteLine("WITNESS JOIN RIGHT COLUMN");
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
            Console.Out.WriteLine("WITNESS NAMED");
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
        //for the left column of join, we just yield each possible column, and the answer gets checked by witness functions 3 and 4 of join.
        [WitnessFunction(nameof(Semantics.Join), 1, DependsOnParameters = new[] { 0 })]
        internal DisjunctiveExamplesSpec WitnessJoin2(GrammarRule rule, DisjunctiveDoubleFilteredTableSpec spec, ExampleSpec leftValue) {
            Console.Out.WriteLine("WITNESS JOIN LEFT COLUMN");
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

    }
}








