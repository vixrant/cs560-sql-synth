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


//need to memo and dedup




namespace Rest560SpecV1 {
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


    class PossibleOrderingsSpec : Spec {
        public IDictionary<State, (List<List<string[]>>[] Examples,HashSet<int> Available)> OrdExamples;
        public PossibleOrderingsSpec(IDictionary<State, (List<List<string[]>>[] Examples,HashSet<int> Available)> OrdExamples): base(OrdExamples.Keys) {
            this.OrdExamples = OrdExamples;
            foreach (var w in OrdExamples) {
                if (w.Value.Item1.Length==0) throw new ArgumentException("No possibilities- null should be returned instead of creating a spec.");
                foreach (var v in w.Value.Item1) {
                    foreach (var u in v) {
                        if (u.Count==0) throw new ArgumentException("No rows in the component- this isn't supported and shouldn't arise from well-formed examples.");
                        foreach (var j in u) {
                            if (j.Length==0) throw new ArgumentException("No columns in the component- this isn't supported and shouldn't arise from well-formed examples.");
                        }
                    }
                }
            }
        }
        public (List<List<string[]>>[] Examples,HashSet<int> Available) sortBy(State state,Tuple<int,bool> candidate) {
            var dup = new HashSet<int>(OrdExamples[state].Available);
            dup.Remove(candidate.Item1);
            var result = new List<List<string[]>>[OrdExamples[state].Examples.Length];
            for (int p=0;p<result.Length;p++) {
                var semiresult = new List<List<string[]>>();
                foreach (var component in OrdExamples[state].Examples[p]) {
                    var semisemiresult = new List<string[]>();
                    for (int row=0;row<component.Count-1;row++) {
                        semisemiresult.Add(component[row]);
                        int cmp = sqlordcompare(component[row][candidate.Item1],component[row+1][candidate.Item1]);
                        if (cmp!=0) {
                            if (semiresult.Count>1) semiresult.Add(semisemiresult);
                            semisemiresult = new List<string[]>();
                        }
                    }
                    semisemiresult.Add(component[component.Count-1]);
                    if (semiresult.Count>1) semiresult.Add(semisemiresult);
                }
                result[p] = semiresult;
            }
            return (result,dup);
        }
        private static int sqlordcompare(string a, string b) {
            if (!double.TryParse(a, out double u)||!double.TryParse(b, out double v)) return a.CompareTo(b);
            return u.CompareTo(v);
        }
        public List<Tuple<int,bool>> GetSatisfiers(State state) {
            HashSet<(int,bool)> union = new HashSet<(int,bool)>();
            foreach (var possib in OrdExamples[state].Examples) {
                HashSet<(int,bool)> intersection = new HashSet<(int, bool)>();
                foreach (int col in OrdExamples[state].Available) {
                    intersection.Add((col,true));
                    intersection.Add((col,false));
                }
                foreach (var component in possib) {
                    for (int row=0;row<component.Count-1;row++) {
                        for (int col=0;col<component[row].Length;col++) {
                            int cmp = sqlordcompare(component[row][col],component[row+1][col]);
                            if (cmp<0) {intersection.Remove((col,true));}
                            if (cmp>0) {intersection.Remove((col,false));}
                        }
                    }
                }
                union.UnionWith(intersection); 
            }
            var newunion = new List<Tuple<int,bool>>();
            foreach ((var a,var b) in union) {
                var j = new Tuple<int,bool>(a,b);
                newunion.Add(j);
            }
            return newunion;
        }
        protected override bool CorrectOnProvided(State state,object obj) {
            var candidate = obj as List<Tuple<int,bool>>;
            foreach ((int col,bool b) in candidate) {
                if (!OrdExamples[state].Available.Contains(col)) return false;
            }
            foreach (var possib in OrdExamples[state].Examples) {
                bool isok=true;
                foreach (var component in possib) {
                    for (int row=0;row<component.Count-1;row++) {
                        foreach ((int col,bool dir) in candidate) {
                            int cmp = sqlordcompare(component[row][col],component[row+1][col]);
                            if (cmp<0 && dir) {return false;}
                            if (cmp>0 && !dir) {return false;}
                            if (cmp!=0) break;
                        }
                    }
                }
                if (isok) return true;
            }
            return false;
        }



        protected override int GetHashCodeOnInput(State state) {
            return this.OrdExamples[state].GetHashCode();
        }
        protected override Spec TransformInputs(Func<State, State> f) {
            var result = new Dictionary<State, (List<List<string[]>>[] Examples,HashSet<int> Available)>();
            foreach (var input in this.OrdExamples.Keys) {
                result[f(input)] = this.OrdExamples[input];
            }
            return new PossibleOrderingsSpec(result);
        }
        protected override bool EqualsOnInput(State state,Spec spec) {
            var other = spec as PossibleOrderingsSpec;
            if (other==null) return false;
            if (!OrdExamples[state].Item2.SetEquals(other.OrdExamples[state].Item2)) return false;
            if (OrdExamples[state].Item1.Length!=other.OrdExamples[state].Item1.Length) return false;
            for (int v=0;v<OrdExamples[state].Item1.Length;v++) {//each possibility
                if (OrdExamples[state].Item1[v].Count!=other.OrdExamples[state].Item1[v].Count) return false;
                for (int u=0;u<OrdExamples[state].Item1[v].Count;u++) {//each component
                    if (OrdExamples[state].Item1[v][u].Count!=other.OrdExamples[state].Item1[v][u].Count) return false;
                    for (int w=0;w<OrdExamples[state].Item1[v][u].Count;w++) {//each option
                        if (!OrdExamples[state].Item1[v][u][w].SequenceEqual(other.OrdExamples[state].Item1[v][u][w])) return false;
                    }
                }
            }
            return true;
        }
        protected override XElement SerializeImpl(Dictionary<object, int> statespace, SpecSerializationContext context) {
            throw new NotImplementedException();
        }
        protected override XElement InputToXML(State state,Dictionary<object, int> statespace) {
            throw new NotImplementedException();
        }
    }


    class DisjunctiveCriteriaSatSpec : Spec {
        public IDictionary<State, Tuple<int,int,int>> LastTuple;
        public IDictionary<State, List<List<string[]>>[]> SatExamples;
        public DisjunctiveCriteriaSatSpec(IDictionary<State, Tuple<int,int,int>> LastTuple,IDictionary<State, List<List<string[]>>[]> SatExamples): base(SatExamples.Keys) {
            this.SatExamples = SatExamples;
            this.LastTuple = LastTuple;
            foreach (var w in SatExamples) {
                if (w.Value.Length==0) throw new ArgumentException("No possibilities- null should be returned instead of creating a spec.");
                foreach (var v in w.Value) {
                    if (v.Count==0) throw new ArgumentException("No components- this isn't supported and shouldn't arise from well-formed examples.");
                    foreach (var u in v) {
                        if (u.Count==0) throw new ArgumentException("No rows in the component- this isn't supported and shouldn't arise from well-formed examples.");
                        foreach (var j in u) {
                            if (j.Length==0) throw new ArgumentException("No columns in the component- this isn't supported and shouldn't arise from well-formed examples.");
                        }
                    }
                }
            }
        }
        private static int sqlcompare(string a, string b) {
            if (!double.TryParse(a, out double u)||!double.TryParse(b, out double v)) return 0;
            return u.CompareTo(v);
        }
        public List<Tuple<int,int,int>> GetSatisfiers(State state) {
            HashSet<(int,int,int)> union = new HashSet<(int,int,int)>();
            foreach (var possib in SatExamples[state]) {
                HashSet<(int,int,int)> intersection = null;
                foreach (var component in possib) {
                    HashSet<(int,int,int)> miniunion = new HashSet<(int,int,int)>();
                    foreach (var row in component) {
                        for (int x=0;x<row.Length;x++) {
                            for (int y=x+1;y<row.Length;y++) {
                                // Eq=0,
                                // Neq=1
                                // Lt=2,
                                // Lteq=3,
                                if (row[x]==row[y]) {
                                    miniunion.Add((0,x,y));
                                    // miniunion.Add((3,x,y));
                                    // miniunion.Add((3,y,x));
                                } else {
                                    miniunion.Add((1,x,y));
                                    // if (sqlcompare(row[x],row[y])<0) miniunion.Add((2,x,y));
                                    // else if (sqlcompare(row[y],row[x])<0)miniunion.Add((2,y,x));
                                }
                            }
                        }
                    }
                    if (intersection==null) intersection=miniunion;
                    else intersection.IntersectWith(miniunion);
                }
                union.UnionWith(intersection); 
            }
            var newunion = new List<Tuple<int,int,int>>();
            foreach ((var a,var b,var c) in union) {
                var j = new Tuple<int,int,int>(a,b,c);
                if (LastTuple!=null) {
                    if ((LastTuple[state] as IComparable).CompareTo(j)<=0) continue;
                }
                newunion.Add(j);
            }
            Console.Out.WriteLine("found this many satisfiers: {0}",newunion.Count);
            // Tuple<int,int,int> whatever = null;
            // foreach (var hah in newunion) {
            //     Console.Out.WriteLine("\t {0}",hah);
            //     whatever=hah;
            // }
            // return new List<Tuple<int,int,int>>{whatever};
            return newunion;
        }
        protected override bool CorrectOnProvided(State state,object obj) {
            var lela = LastTuple==null?null:LastTuple[state];
            var candidate = obj as List<Tuple<int,int,int>>;
            foreach (var cand in candidate) {
                if (lela!=null && (lela as IComparable).CompareTo(cand)<=0) return false;
                lela = cand;
            }
            foreach (List<List<string[]>> possib in SatExamples[state]) {
                var allsat = true;
                foreach (List<string[]> component in possib) {
                    var onesat = false;
                    foreach (string[] row in component) {
                        var rowworks = true;
                        foreach (Tuple<int,int,int> criteria in candidate) {
                            bool truth;
                            switch (criteria.Item1) {
                                case 0: truth = (row[criteria.Item2]==row[criteria.Item3]);break;// Eq=0,
                                case 1: truth = (row[criteria.Item2]!=row[criteria.Item3]);break;// Neq=1,
                                case 2: truth = (sqlcompare(row[criteria.Item2],row[criteria.Item3])<0);break;// Lt=2,
                                case 3: truth = (sqlcompare(row[criteria.Item2],row[criteria.Item3])<0 || row[criteria.Item2]==row[criteria.Item3]);break;// Lteq=3
                                default: throw new ArgumentException("invalid");
                            }
                            if (!truth) {rowworks=false;break;}
                        }
                        if (rowworks) {onesat=true;break;}
                    }
                    if (!onesat) {allsat=false;break;}
                }
                if (allsat) return true;
            }
            return false;
        }



        protected override int GetHashCodeOnInput(State state) {
            if (LastTuple==null)
            return this.SatExamples[state].GetHashCode();
            return (this.SatExamples[state],this.LastTuple[state]).GetHashCode();
        }
        protected override Spec TransformInputs(Func<State, State> f) {
            var result = new Dictionary<State, List<List<string[]>>[]>();
            foreach (var input in this.SatExamples.Keys) {
                result[f(input)] = this.SatExamples[input];
            }
            return new DisjunctiveCriteriaSatSpec(LastTuple,result);
        }
        protected override bool EqualsOnInput(State state,Spec spec) {
            var other = spec as DisjunctiveCriteriaSatSpec;
            if (other==null) return false;
            if ((LastTuple==null)!=(other.LastTuple==null)) return false;
            if (LastTuple!=null) {
                if (LastTuple[state]!=other.LastTuple[state]) return false;
            }
            if (SatExamples[state].Length!=other.SatExamples[state].Length) return false;
            for (int v=0;v<SatExamples[state].Length;v++) {//each possibility
                if (SatExamples[state][v].Count!=other.SatExamples[state][v].Count) return false;
                for (int u=0;u<SatExamples[state][v].Count;u++) {//each component
                    if (SatExamples[state][v][u].Count!=other.SatExamples[state][v][u].Count) return false;
                    for (int w=0;w<SatExamples[state][v][u].Count;w++) {//each option
                        if (!SatExamples[state][v][u][w].SequenceEqual(other.SatExamples[state][v][u][w])) return false;
                    }
                }
            }
            return true;
        }
        protected override XElement SerializeImpl(Dictionary<object, int> statespace, SpecSerializationContext context) {
            throw new NotImplementedException();
        }
        protected override XElement InputToXML(State state,Dictionary<object, int> statespace) {
            throw new NotImplementedException();
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
            this.CustomTableExamples = CustomTableExamples;
        }
        public List<List<string[]>>[] GetFilterCriteria(State state,object obj) {
            var candidate = obj as List<string[]>;
            var settlepoints = GetSettlePoints(state,obj);
            var result = new List<List<string[]>>[settlepoints.Count];
            for (int i=0;i<settlepoints.Count;i++) {
                var settle=settlepoints[i];
                var rowcount = settle.GetLength(0);
                var colcount = settle.GetLength(1);
                var clumps = new List<List<string[]>>();
                for  (int row=0;row<rowcount;row++) {
                    var intersection = new HashSet<int>();//eliminates duplicates
                    foreach ((int Row,int Col) in settle[row,0]) intersection.Add(Row);
                    var pos = new List<string[]>();
                    foreach (var ints in intersection) {
                        pos.Add(Array.ConvertAll(candidate[ints],x=>x));
                    }
                    clumps.Add(pos);
                }
                result[i]=clumps;
            }
            return result;
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
                        foreach (string pos in space[row][col]) {
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
            foreach (List<(int Row,int Col)>[,] intermed in GetSettleInitial(state,obj,false)) {
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


        public List<List<int>> extractSingleRowMaps(State state,object obj) {
            var result = new List<List<int>>();
            foreach (var settle in GetSettlePoints(state,obj)) {
                int rowcount = settle.GetLength(0);
                int colcount = settle.GetLength(1);
                if (rowcount!=(obj as List<string[]>).Count) continue;
                // for (int row=0;row<rowcount;row++) {
                //     for (int col=0;col<colcount;col++) {
                //         Console.Out.Write("{0}|",String.Join(",",settle[row,col]));
                //     }
                //     Console.Out.WriteLine();
                // }
                HashSet<int>[] rowconv = new HashSet<int>[rowcount];
                var workqueue = new UniqueQueue<int>();
                for (int i=0;i<rowcount;i++) {
                    workqueue.Enqueue(i);
                    rowconv[i] = new HashSet<int>();
                    // Console.Out.WriteLine("{0} {1}",i,String.Join(",",settle[i,0]));
                    foreach ((int Row,int Col) in settle[i,0]) rowconv[i].Add(Row);
                }
                Console.Out.WriteLine();
                bool ok = true;
                while (workqueue.Count>0) {
                    while (workqueue.Count>0) {
                        var i = workqueue.Dequeue();
                        // Console.Out.WriteLine(String.Join(",",rowconv[i]));
                        // continue;
                        if (rowconv[i].Count!=1) continue;
                        int spec = rowconv[i].First();
                        for (int j=0;j<rowcount;j++) {
                            if (j==i) continue;
                            if (rowconv[j].Remove(spec)) {
                                if (rowconv[j].Count==0) ok=false;
                                if (rowconv[j].Count==1) workqueue.Enqueue(j);
                            }
                        }
                    } 
                    // ok=false;
                    if (!ok) break;

                    var dualworkqueue = new UniqueQueue<int>();
                    for (int i=0;i<rowcount;i++) {
                        if (rowconv[i].Count!=1) {
                            // Console.Out.WriteLine(String.Join(",",rowconv[i]));
                            foreach (int cand in rowconv[i]) dualworkqueue.Enqueue(cand);
                        }
                    }

                    while (dualworkqueue.Count>0) {
                        var i = dualworkqueue.Dequeue();
                        int total = 0;
                        int onefound = -1;
                        for (int j=0;j<rowcount;j++) {
                            if (rowconv[j].Contains(i)) {
                                onefound=j;
                                total+=1;
                            }
                        }
                        if (total==0) ok=false;
                        if (total==1) {
                            rowconv[onefound].RemoveWhere(x=>{
                                if (x!=i) {
                                    dualworkqueue.Enqueue(x);
                                    return true;
                                }
                                return false;
                            });
                        }
                    }
                    if (!ok) break;
                    for (int i=0;i<rowcount;i++) {
                        if (rowconv[i].Count!=1) {
                            //pick a winner arbitrarily:
                            //   this is theoretically unsound but rectifying it would involve making the ordering spec much more complicated
                            //   it only arises in cases where you're ordering by a column that wasn't selected, and there are also duplicate rows in the expected table.
                            //   it's basically never going to arise in normal usage of this tool, but i'd fix it if this were a longer term project.
                            rowconv[i] = new HashSet<int>{rowconv[i].Min()};
                            workqueue.Enqueue(i);
                            break;
                        }
                    }
                }
                if (!ok) continue;
                Console.Out.WriteLine("well, one was deemed ok");
                var semiresult = new List<int>();
                foreach (var conv in rowconv) semiresult.Add(conv.First());
                result.Add(semiresult);
            }
            return result;
        }


        public (List<List<string[]>>[] Examples,HashSet<int> Available) prepareOrderingSpec(State state,object obj) {
            var singleRowMaps = extractSingleRowMaps(state,obj);
            var av = new HashSet<int>();
            var candidate = obj as List<string[]>;
            for (int i=0;i<candidate[0].Length;i++) av.Add(i);
            var examples = new List<List<string[]>>[singleRowMaps.Count];
            for (int ex=0;ex<examples.Length;ex++) {
                var innerlist = new List<string[]>();
                foreach (int ax in singleRowMaps[ex]) innerlist.Add(candidate[ax]);
                examples[ex] = new List<List<string[]>>{innerlist};
            }
            return (examples,av);
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
}
