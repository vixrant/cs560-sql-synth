using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml.Linq;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.Specifications;
using Microsoft.ProgramSynthesis.Specifications.Serialization;

namespace Rest560SpecV1
{

    /**
     This checks to see if there exists a mapping from space's rows to candidate's rows, and
     space's columns to candidate's columns, such that for each element inside of space (row,column,value),
     this holds:  candidate[rowmapping[row]][columnmapping[column]]==value
     
     This spec is chosen with the understanding that rows may be filtered out of candidate (by FILTER or GROUP), and the order of the rows may be changed (by ORDER),
     and also that a few columns will be selected, with their order changed as well (by PROJECT)
     */
    class DisjunctiveDoubleFilteredTableSpec : Spec
    {
        public IDictionary<State, List<string[][]>[]> CustomTableExamples;
        public DisjunctiveDoubleFilteredTableSpec(IDictionary<State, List<string[][]>[]> CustomTableExamples) : base(CustomTableExamples.Keys)
        {
            foreach (var w in CustomTableExamples)
            {
                if (w.Value.Length == 0) throw new ArgumentException("No possibilities- null should be returned instead of creating a spec.");
                foreach (var v in w.Value)
                {
                    if (v.Count == 0) throw new ArgumentException("No rows- this isn't supported and shouldn't arise from well-formed examples.");
                    foreach (var u in v)
                    {
                        if (u.Length == 0) throw new ArgumentException("No columns- this isn't supported and shouldn't arise from well-formed examples.");
                        foreach (var j in u)
                        {
                            if (j.Length == 0) throw new ArgumentException("A row/column pair supports exactly 0 possibilities- the possibility shouldn't have been yielded.");
                        }
                    }
                }
            }
            this.CustomTableExamples = CustomTableExamples;
        }
        public List<List<string[]>>[] GetFilterCriteria(State state, object obj)
        {
            var candidate = obj as List<string[]>;
            var settlepoints = GetSettlePoints(state, obj);
            var result = new List<List<string[]>>[settlepoints.Count];
            for (int i = 0; i < settlepoints.Count; i++)
            {
                var settle = settlepoints[i];
                var rowcount = settle.GetLength(0);
                var colcount = settle.GetLength(1);
                var clumps = new List<List<string[]>>();
                for (int row = 0; row < rowcount; row++)
                {
                    var intersection = new HashSet<int>();//eliminates duplicates
                    foreach ((int Row, int Col) in settle[row, 0]) intersection.Add(Row);
                    var pos = new List<string[]>();
                    foreach (var ints in intersection)
                    {
                        pos.Add(Array.ConvertAll(candidate[ints], x => x));
                    }
                    clumps.Add(pos);
                }
                result[i] = clumps;
            }
            return result;
        }
        public DisjunctiveDoubleFilteredTableSpec SplitColumnwise()
        {
            var inner = new Dictionary<State, List<string[][]>[]>();
            foreach (var example in CustomTableExamples)
            {
                State inputState = example.Key;
                var fullTables = example.Value as List<string[][]>[];
                int total = 0;
                foreach (var possib in fullTables) total += possib[0].Length;
                var newtables = new List<string[][]>[total];
                int dest = 0;
                for (int k = 0; k < fullTables.Length; k++)
                {
                    for (int i = 0; i < fullTables[k][0].Length; i++)
                    {
                        var newtable = new List<string[][]>();
                        newtables[dest++] = newtable;
                        for (int j = 0; j < fullTables[k].Count; j++) newtable.Add(new string[][] { fullTables[k][j][i] });
                    }
                }
                inner[inputState] = newtables;
            }
            return new DisjunctiveDoubleFilteredTableSpec(inner);
        }
        public static DisjunctiveDoubleFilteredTableSpec FromConcrete(ExampleSpec spec)
        {
            var result = new Dictionary<State, List<string[][]>[]>();
            foreach (var example in spec.Examples) result[example.Key] = new List<string[][]>[]{
                DisjunctiveDoubleFilteredTableSpec.ConvertConcrete(example.Value as List<string[]>)
            };
            return new DisjunctiveDoubleFilteredTableSpec(result);
        }
        public static List<string[][]> ConvertConcrete(List<string[]> table)
        {
            var outp = new List<string[][]>();
            foreach (string[] row in table)
            {
                var newrow = new string[row.Length][];
                for (int i = 0; i < row.Length; i++) newrow[i] = new string[] { row[i] };
                outp.Add(newrow);
            }
            return outp;
        }
        private List<List<(int Row, int Col)>[,]> GetSettleInitial(State state, object obj, bool abortOnEmpty)
        {
            var result = new List<List<(int Row, int Col)>[,]>();
            foreach (List<string[][]> space in this.CustomTableExamples[state])
            {
                var candidate = obj as List<string[]>;
                var governhash = new Dictionary<string, List<(int Row, int Col)>>();
                for (int crow = 0; crow < candidate.Count; crow++)
                {
                    for (int ccol = 0; ccol < candidate[0].Length; ccol++)
                    {
                        List<(int Row, int Col)> value = null;
                        var vval = candidate[crow][ccol];
                        if (governhash.TryGetValue(vval, out value))
                        {
                            value.Add((crow, ccol));
                        }
                        else
                        {
                            governhash[vval] = new List<(int Row, int Col)> { (crow, ccol) };
                        }
                    }
                }
                int rowcount = space.Count;
                int colcount = space[0].Length;
                var intermed = new List<(int Row, int Col)>[rowcount, colcount];
                bool fullbreak = false;
                for (int row = 0; row < rowcount; row++)
                {
                    for (int col = 0; col < colcount; col++)
                    {
                        var imd = new List<(int Row, int Col)>();
                        intermed[row, col] = imd;
                        foreach (string pos in space[row][col])
                        {
                            List<(int Row, int Col)> govr = null;
                            if (governhash.TryGetValue(pos, out govr))
                            {
                                foreach ((int, int) bs in govr) imd.Add(bs);
                            }
                        }
                        if (abortOnEmpty && imd.Count == 0) { fullbreak = true; break; }
                    }
                    if (fullbreak) break;
                }
                if (fullbreak) continue;

                result.Add(intermed);
            }
            return result;
        }
        private static bool GetSettlePoint(List<(int Row, int Col)>[,] intermed)
        {
            int rowcount = intermed.GetLength(0);
            int colcount = intermed.GetLength(1);
            var columnWork = new UniqueQueue<int>();
            var rowWork = new UniqueQueue<int>();
            for (int row = 0; row < rowcount; row++) rowWork.Enqueue(row);
            for (int col = 0; col < colcount; col++) columnWork.Enqueue(col);
            while (columnWork.Count > 0 || rowWork.Count > 0)
            {
                while (columnWork.Count > 0)
                {
                    var col = columnWork.Dequeue();
                    var intersection = new HashSet<int>();
                    foreach ((int Row, int Col) in intermed[0, col]) intersection.Add(Col);
                    for (int row = 1; row < rowcount; row++)
                    {
                        var next = new HashSet<int>();
                        foreach ((int Row, int Col) in intermed[row, col])
                        {
                            if (intersection.Contains(Col)) next.Add(Col);
                        }
                        if (next.Count == 0) return false;
                        intersection = next;
                    }
                    for (int row = 0; row < rowcount; row++)
                    {
                        var djlist = intermed[row, col];
                        for (int i = djlist.Count - 1; i >= 0; i--)
                        {
                            if (!intersection.Contains(djlist[i].Col))
                            {
                                rowWork.Enqueue(row);
                                djlist[i] = djlist[djlist.Count - 1];//removes in O(1), sacrificing order stability
                                djlist.RemoveAt(djlist.Count - 1);
                            }
                        }
                    }
                }
                while (rowWork.Count > 0)
                {
                    var row = rowWork.Dequeue();
                    var intersection = new HashSet<int>();
                    foreach ((int Row, int Col) in intermed[row, 0]) { intersection.Add(Row); }
                    for (int col = 1; col < colcount; col++)
                    {
                        var next = new HashSet<int>();
                        foreach ((int Row, int Col) in intermed[row, col])
                        {
                            if (intersection.Contains(Row)) next.Add(Row);
                        }
                        if (next.Count == 0) return false;
                        intersection = next;
                    }
                    for (int col = 0; col < colcount; col++)
                    {
                        var djlist = intermed[row, col];
                        for (int i = djlist.Count - 1; i >= 0; i--)
                        {
                            if (!intersection.Contains(djlist[i].Row))
                            {
                                columnWork.Enqueue(col);
                                djlist[i] = djlist[djlist.Count - 1];//removes in O(1), sacrificing order stability
                                djlist.RemoveAt(djlist.Count - 1);
                            }
                        }
                    }
                }
            }
            return true;
        }
        private List<List<(int Row, int Col)>[,]> GetSettlePoints(State state, object obj)
        {
            var result = new List<List<(int Row, int Col)>[,]>();
            foreach (List<(int Row, int Col)>[,] intermed in GetSettleInitial(state, obj, true))
            {
                if (GetSettlePoint(intermed)) result.Add(intermed);
            }
            return result;
        }
        private List<List<(int Row, int Col)>[,]> GetColumnwiseSettlePoints(State state, object obj)
        {
            var result = new List<List<(int Row, int Col)>[,]>();
            foreach (List<(int Row, int Col)>[,] intermed in GetSettleInitial(state, obj, false))
            {
                int rowcount = intermed.GetLength(0);
                int colcount = intermed.GetLength(1);
                for (int col = 0; col < colcount; col++)
                {
                    var intersection = new HashSet<int>();
                    foreach ((int Row, int Col) in intermed[0, col]) intersection.Add(Col);
                    for (int row = 1; row < rowcount; row++)
                    {
                        var next = new HashSet<int>();
                        foreach ((int Row, int Col) in intermed[row, col])
                        {
                            if (intersection.Contains(Col)) next.Add(Col);
                        }
                        intersection = next;
                    }
                    for (int row = 0; row < rowcount; row++)
                    {
                        var djlist = intermed[row, col];
                        for (int i = djlist.Count - 1; i >= 0; i--)
                        {
                            if (!intersection.Contains(djlist[i].Col))
                            {
                                djlist[i] = djlist[djlist.Count - 1];//removes in O(1), sacrificing order stability
                                djlist.RemoveAt(djlist.Count - 1);
                            }
                        }
                    }
                }
                result.Add(intermed);
            }
            return result;
        }


        protected override bool CorrectOnProvided(State state, object obj)
        {
            return GetSettlePoints(state, obj).Count > 0;
        }


        public List<List<int>> extractSingleRowMaps(State state, object obj)
        {
            var result = new List<List<int>>();
            foreach (var settle in GetSettlePoints(state, obj))
            {
                int rowcount = settle.GetLength(0);
                int colcount = settle.GetLength(1);
                if (rowcount != (obj as List<string[]>).Count) continue;
                HashSet<int>[] rowconv = new HashSet<int>[rowcount];
                var workqueue = new UniqueQueue<int>();
                for (int i = 0; i < rowcount; i++)
                {
                    workqueue.Enqueue(i);
                    rowconv[i] = new HashSet<int>();
                    // Console.Out.WriteLine("{0} {1}",i,String.Join(",",settle[i,0]));
                    foreach ((int Row, int Col) in settle[i, 0]) rowconv[i].Add(Row);
                }
                // Console.Out.WriteLine();
                bool ok = true;
                while (workqueue.Count > 0)
                {
                    while (workqueue.Count > 0)
                    {
                        var i = workqueue.Dequeue();
                        // Console.Out.WriteLine(String.Join(",",rowconv[i]));
                        // continue;
                        if (rowconv[i].Count != 1) continue;
                        int spec = rowconv[i].First();
                        for (int j = 0; j < rowcount; j++)
                        {
                            if (j == i) continue;
                            if (rowconv[j].Remove(spec))
                            {
                                if (rowconv[j].Count == 0) ok = false;
                                if (rowconv[j].Count == 1) workqueue.Enqueue(j);
                            }
                        }
                    }
                    // ok=false;
                    if (!ok) break;

                    var dualworkqueue = new UniqueQueue<int>();
                    for (int i = 0; i < rowcount; i++)
                    {
                        if (rowconv[i].Count != 1)
                        {
                            // Console.Out.WriteLine(String.Join(",",rowconv[i]));
                            foreach (int cand in rowconv[i]) dualworkqueue.Enqueue(cand);
                        }
                    }

                    while (dualworkqueue.Count > 0)
                    {
                        var i = dualworkqueue.Dequeue();
                        int total = 0;
                        int onefound = -1;
                        for (int j = 0; j < rowcount; j++)
                        {
                            if (rowconv[j].Contains(i))
                            {
                                onefound = j;
                                total += 1;
                            }
                        }
                        if (total == 0) ok = false;
                        if (total == 1)
                        {
                            rowconv[onefound].RemoveWhere(x =>
                            {
                                if (x != i)
                                {
                                    dualworkqueue.Enqueue(x);
                                    return true;
                                }
                                return false;
                            });
                        }
                    }
                    if (!ok) break;
                    for (int i = 0; i < rowcount; i++)
                    {
                        if (rowconv[i].Count != 1)
                        {
                            //pick a winner arbitrarily:
                            //   this is theoretically unsound but rectifying it would involve making the ordering spec much more complicated
                            //   it only arises in cases where you're ordering by a column that wasn't selected, and there are also duplicate rows in the expected table.
                            //   it's basically never going to arise in normal usage of this tool, but i'd fix it if this were a longer term project.
                            rowconv[i] = new HashSet<int> { rowconv[i].Min() };
                            workqueue.Enqueue(i);
                            break;
                        }
                    }
                }
                if (!ok) continue;
                // Console.Out.WriteLine("well, one was deemed ok");
                var semiresult = new List<int>();
                foreach (var conv in rowconv) semiresult.Add(conv.First());
                result.Add(semiresult);
            }
            return result;
        }


        public (List<List<string[]>>[] Examples, HashSet<int> Available) prepareOrderingSpec(State state, object obj)
        {
            var singleRowMaps = extractSingleRowMaps(state, obj);
            var av = new HashSet<int>();
            var candidate = obj as List<string[]>;
            for (int i = 0; i < candidate[0].Length; i++) av.Add(i);
            var examples = new List<List<string[]>>[singleRowMaps.Count];
            for (int ex = 0; ex < examples.Length; ex++)
            {
                var innerlist = new List<string[]>();
                foreach (int ax in singleRowMaps[ex]) innerlist.Add(candidate[ax]);
                examples[ex] = new List<List<string[]>> { innerlist };
            }
            return (examples, av);
        }

        public List<int> extractPossibleSingularLastColumnMappings(State state, object obj)
        {
            var result = new List<int> { };
            foreach (var settle in GetSettlePoints(state, obj))
            {
                int colcount = settle.GetLength(1);
                foreach ((int Row, int Col) in settle[0, colcount - 1]) result.Add(Col);
            }
            return result;
        }
        public List<List<int>> extractPossibleColumnMappings(List<(int Row, int Col)>[,] settle)
        {
            int colcount = settle.GetLength(1);
            var interim = new List<List<int>> { new List<int> { } };
            for (int col = 0; col < colcount; col++)
            {
                var intersection = new HashSet<int>();//eliminates duplicates
                foreach ((int Row, int Col) in settle[0, col]) intersection.Add(Col);
                var next = new List<List<int>> { };
                foreach (List<int> partial in interim)
                {
                    foreach (int pos in intersection)
                    {
                        var newpartial = partial.ConvertAll(x => x);
                        newpartial.Add(pos);
                        next.Add(newpartial);
                    }
                }
                interim = next;
            }
            return interim;
        }
        public List<List<int>> extractPossibleColumnMappings(State state, object obj)
        {
            var result = new List<List<int>> { };
            foreach (var settle in GetSettlePoints(state, obj))
            {
                result.AddRange(extractPossibleColumnMappings(settle));
            }
            return result;
        }
        public List<List<(string[][] Remaining, HashSet<int> RowAssignments)>> getRowAssignmentsToSatisfyRest(State state, object obj)
        {
            var result = new List<List<(string[][] Remaining, HashSet<int> RowAssignments)>>();
            var columnwiseGroup = GetColumnwiseSettlePoints(state, obj);
            for (int columnwiseIndex = 0; columnwiseIndex < columnwiseGroup.Count; columnwiseIndex++)
            {
                var columnwise = columnwiseGroup[columnwiseIndex];
                var children = new Dictionary<int, List<int>>();
                var sideways = new Dictionary<int, List<int>>();
                var roots = new List<int>();
                int rowcount = columnwise.GetLength(0);
                int colcount = columnwise.GetLength(1);
                var thinned = new HashSet<int>[rowcount, colcount];
                var nulled = new HashSet<int>();
                for (int col = 0; col < colcount; col++)
                {
                    for (int row = 0; row < rowcount; row++)
                    {
                        var intersection = new HashSet<int>();//eliminates duplicates
                        foreach ((int Row, int Col) in columnwise[row, col]) intersection.Add(Row);
                        if (intersection.Count == 0) nulled.Add(col);
                        thinned[row, col] = intersection;
                    }
                }
                var implicationMatrix = new bool[colcount, colcount];
                for (int x = 0; x < colcount; x++)
                {
                    if (nulled.Contains(x)) continue;
                    for (int y = 0; y < colcount; y++)
                    {
                        if (nulled.Contains(y)) continue;
                        var allimp = true;
                        for (int row = 0; row < rowcount; row++)
                        {
                            if (!thinned[row, x].IsSubsetOf(thinned[row, y])) { allimp = false; break; }
                        }
                        implicationMatrix[x, y] = allimp;
                    }
                }
                var initialassignment = new Dictionary<int, bool>();
                foreach (int nullval in nulled) initialassignment[nullval] = false;
                var assignments = new List<Dictionary<int, bool>> { initialassignment };
                for (int col = 0; col < colcount; col++)
                {
                    if (nulled.Contains(col)) continue;
                    var next = new List<Dictionary<int, bool>>();
                    foreach (Dictionary<int, bool> partial in assignments)
                    {
                        if (partial.ContainsKey(col))
                        {
                            next.Add(partial); continue;
                        }
                        var workQueue = new Queue<int>();
                        var truepartial = new Dictionary<int, bool>(partial);
                        workQueue.Enqueue(col);
                        var truepartialvalid = true;
                        while (workQueue.Count > 0)
                        {
                            var ncol = workQueue.Dequeue();
                            if (truepartial.ContainsKey(ncol))
                            {
                                if (truepartial[ncol]) continue;
                                truepartialvalid = false; break;
                            }
                            truepartial[ncol] = true;
                            for (int a = 0; a < colcount; a++)
                            {
                                if (implicationMatrix[ncol, a]) workQueue.Enqueue(a);
                            }
                        }
                        if (truepartialvalid) next.Add(truepartial);
                        var falsepartial = partial;
                        workQueue.Enqueue(col);
                        var falsepartialvalid = true;
                        while (workQueue.Count > 0)
                        {
                            var ncol = workQueue.Dequeue();
                            if (falsepartial.ContainsKey(ncol))
                            {
                                if (!falsepartial[ncol]) continue;
                                falsepartialvalid = false; break;
                            }
                            falsepartial[ncol] = false;
                            for (int a = 0; a < colcount; a++)
                            {
                                if (implicationMatrix[a, ncol]) workQueue.Enqueue(a);
                            }
                        }
                        if (falsepartialvalid) next.Add(falsepartial);
                    }
                    assignments = next;
                }
                var lastphase = new List<(HashSet<int> Remaining, List<HashSet<int>> RowAssignments)>();
                foreach (var assignment in assignments)
                {
                    int total = 0;
                    foreach (var pair in assignment) { if (pair.Value) { total++; } }
                    if (total == 0) continue;
                    var newinitial = new List<(int Row, int Col)>[rowcount, total];
                    int i = 0;
                    var re = new HashSet<int>();
                    foreach (var pair in assignment)
                    {
                        if (pair.Value)
                        {
                            for (int row = 0; row < rowcount; row++)
                            {
                                newinitial[row, i] = columnwise[row, pair.Key];
                            }
                            i++;
                        }
                        else
                        {
                            re.Add(pair.Key);
                        }
                    }
                    if (GetSettlePoint(newinitial))
                    {
                        var ra = new List<HashSet<int>>();
                        for (int row = 0; row < rowcount; row++)
                        {
                            var intersection = new HashSet<int>();//eliminates duplicates
                            foreach ((int Row, int Col) in newinitial[row, 0]) intersection.Add(Col);
                            ra.Add(intersection);
                        }
                        for (int j = 0; j < lastphase.Count; j++)
                        {
                            if (lastphase[j].RowAssignments.Count != ra.Count) continue;
                            var eqa = true;
                            for (int k = 0; k < ra.Count; k++)
                            {
                                if (!lastphase[j].RowAssignments[k].SetEquals(ra[k])) { eqa = false; break; }
                            }
                            if (eqa) { lastphase[j].Remaining.IntersectWith(re); break; }
                        }
                        lastphase.Add((re, ra));
                    }
                }
                var thistable = CustomTableExamples[state][columnwiseIndex];
                foreach ((HashSet<int> Remaining, List<HashSet<int>> RowAssignments) in lastphase)
                {
                    var OrderedRemain = new List<int>();
                    foreach (int ind in Remaining) OrderedRemain.Add(ind);
                    var subresult = new List<(string[][] Remaining, HashSet<int> RowAssignments)>();
                    for (int row = 0; row < rowcount; row++)
                    {
                        var newrow = new string[OrderedRemain.Count()][];
                        for (int c = 0; c < OrderedRemain.Count(); c++)
                        {
                            newrow[c] = thistable[row][OrderedRemain[c]];
                        }
                        subresult.Add((newrow, RowAssignments[row]));
                    }
                    result.Add(subresult);
                }
            }
            return result;
        }


        public List<string[][]>[] addJoiningColumnToSpec(State state, object obj, int colId)
        {
            var reras = getRowAssignmentsToSatisfyRest(state, obj);
            var candidate = obj as List<string[]>;
            var result = new List<string[][]>[reras.Count];
            for (int ri = 0; ri < reras.Count; ri++)
            {
                var subresult = new List<string[][]>();
                foreach ((string[][] Remaining, HashSet<int> RowAssignments) in reras[ri])
                {
                    var z = new string[Remaining.Length + 1][];
                    Remaining.CopyTo(z, 0);
                    var w = new string[RowAssignments.Count];
                    int i = 0;
                    foreach (var ra in RowAssignments)
                    {
                        w[i] = candidate[ra][colId]; i++;
                    }
                    z[Remaining.Length] = w;
                    subresult.Add(z);
                }
                result[ri] = subresult;
            }
            return result;
        }


        protected override int GetHashCodeOnInput(State state)
        {
            return this.CustomTableExamples[state].GetHashCode();
        }
        protected override Spec TransformInputs(Func<State, State> f)
        {
            var result = new Dictionary<State, List<string[][]>[]>();
            foreach (var input in this.CustomTableExamples.Keys)
            {
                result[f(input)] = this.CustomTableExamples[input];
            }
            return new DisjunctiveDoubleFilteredTableSpec(result);
        }
        protected override bool EqualsOnInput(State state, Spec spec)
        {
            throw new NotImplementedException();
        }
        protected override XElement SerializeImpl(Dictionary<object, int> statespace, SpecSerializationContext context)
        {
            throw new NotImplementedException();
        }
        protected override XElement InputToXML(State state, Dictionary<object, int> statespace)
        {
            throw new NotImplementedException();
        }
    }
}
