using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml.Linq;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.Specifications;
using Microsoft.ProgramSynthesis.Specifications.Serialization;

using Rest560;

namespace Rest560SpecV1
{
    class DisjunctiveCriteriaSatSpec : Spec
    {
        public HashSet<(int,int,int)> Allowed;
        public IDictionary<State, Tuple<int, int, int>> LastTuple;
        public IDictionary<State, List<List<string[]>>[]> SatExamples;

        public DisjunctiveCriteriaSatSpec(HashSet<(int,int,int)> Allowed,IDictionary<State, Tuple<int, int, int>> LastTuple, IDictionary<State, List<List<string[]>>[]> SatExamples) : base(SatExamples.Keys)
        {
            this.Allowed = Allowed;
            this.SatExamples = SatExamples;
            this.LastTuple = LastTuple;
            foreach (var w in SatExamples)
            {
                if (w.Value.Length == 0) throw new ArgumentException("No possibilities- null should be returned instead of creating a spec.");
                foreach (var v in w.Value)
                {
                    if (v.Count == 0) throw new ArgumentException("No components- this isn't supported and shouldn't arise from well-formed examples.");
                    foreach (var u in v)
                    {
                        if (u.Count == 0) throw new ArgumentException("No rows in the component- this isn't supported and shouldn't arise from well-formed examples.");
                        foreach (var j in u)
                        {
                            if (j.Length == 0) throw new ArgumentException("No columns in the component- this isn't supported and shouldn't arise from well-formed examples.");
                        }
                    }
                }
            }
        }

        private static int sqlcompare(string a, string b)
        {
            if (!double.TryParse(a, out double u) || !double.TryParse(b, out double v)) return 0;
            return u.CompareTo(v);
        }
        public static HashSet<(int,int,int)> GetInverseSatisfiers(List<List<string[]>> adj) {
            var result = new HashSet<(int,int,int)>();
            foreach (var stage in adj) {
                foreach (var row in stage)
                    {
                        for (int x = 0; x < row.Length; x++)
                        {
                            for (int y = x + 1; y < row.Length; y++)
                            {
                                // Eq=0,
                                // Neq=1
                                // Lt=2,
                                // Lteq=3,
                                if (row[x] == row[y])
                                {
                                    result.Add((1, x, y));
                                    // result.Add((3,x,y));
                                    // result.Add((3,y,x));
                                }
                                else
                                {
                                    result.Add((0, x, y));
                                    // if (sqlcompare(row[x],row[y])<0) result.Add((2,x,y));
                                    // else if (sqlcompare(row[y],row[x])<0)result.Add((2,y,x));
                                }
                            }
                        }
                    }
            }
            return result;
        }
        public List<Tuple<int, int, int>> GetSatisfiers(State state)
        {
            HashSet<(int, int, int)> union = new HashSet<(int, int, int)>();
            foreach (var possib in SatExamples[state])
            {
                HashSet<(int, int, int)> intersection = null;
                foreach (var component in possib)
                {
                    HashSet<(int, int, int)> miniunion = new HashSet<(int, int, int)>();
                    foreach (var row in component)
                    {
                        for (int x = 0; x < row.Length; x++)
                        {
                            for (int y = x + 1; y < row.Length; y++)
                            {
                                // Eq=0,
                                // Neq=1
                                // Lt=2,
                                // Lteq=3,
                                if (row[x] == row[y])
                                {
                                    miniunion.Add((0, x, y));
                                    // miniunion.Add((3,x,y));
                                    // miniunion.Add((3,y,x));
                                }
                                else
                                {
                                    miniunion.Add((1, x, y));
                                    // if (sqlcompare(row[x],row[y])<0) miniunion.Add((2,x,y));
                                    // else if (sqlcompare(row[y],row[x])<0)miniunion.Add((2,y,x));
                                }
                            }
                        }
                    }
                    if (intersection == null) intersection = miniunion;
                    else intersection.IntersectWith(miniunion);
                }
                union.UnionWith(intersection);
            }
            // union.IntersectWith(Allowed);
            var newunion = new List<Tuple<int, int, int>>();
            foreach ((var a, var b, var c) in union)
            {
                var j = new Tuple<int, int, int>(a, b, c);
                if (LastTuple != null)
                {
                    if ((LastTuple[state] as IComparable).CompareTo(j) <= 0) continue;
                }
                newunion.Add(j);
            }
            // Console.Out.WriteLine("found this many satisfiers: {0}",newunion.Count);
            // // Tuple<int,int,int> whatever = null;
            // foreach (var hah in newunion) {
            //     Console.Out.WriteLine("\t {0}",hah);
            //     // whatever=hah;
            // }
            // return new List<Tuple<int,int,int>>{whatever};
            return newunion;
        }

        protected override bool CorrectOnProvided(State state, object obj)
        {
            var lela = LastTuple == null ? null : LastTuple[state];
            var candidate = obj as List<Tuple<int, int, int>>;
            foreach (var cand in candidate)
            {
                if (lela != null && (lela as IComparable).CompareTo(cand) <= 0) return false;
                lela = cand;
            }
            
            foreach (List<List<string[]>> possib in SatExamples[state])
            {
                var allsat = true;
                foreach (List<string[]> component in possib)
                {
                    var onesat = false;
                    foreach (string[] row in component)
                    {
                        var rowworks = true;
                        foreach (Tuple<int, int, int> criteria in candidate)
                        {
                            bool truth;
                            switch (criteria.Item1)
                            {
                                case BinOp.Eq: truth = (row[criteria.Item2] == row[criteria.Item3]); break;
                                case BinOp.Neq: truth = (row[criteria.Item2] != row[criteria.Item3]); break;
                                case BinOp.Lt: truth = (sqlcompare(row[criteria.Item2], row[criteria.Item3]) < 0); break;
                                case BinOp.Lteq: truth = (sqlcompare(row[criteria.Item2], row[criteria.Item3]) < 0 || row[criteria.Item2] == row[criteria.Item3]); break;
                                default: throw new ArgumentException("invalid");
                            }
                            if (!truth) { rowworks = false; break; }
                        }
                        if (rowworks) { onesat = true; break; }
                    }
                    if (!onesat) { allsat = false; break; }
                }
                if (allsat) return true;
            }
            return false;
        }

        protected override int GetHashCodeOnInput(State state)
        {
            if (LastTuple == null)
                return this.SatExamples[state].GetHashCode();
            return (this.SatExamples[state], this.LastTuple[state]).GetHashCode();
        }

        protected override Spec TransformInputs(Func<State, State> f)
        {
            var result = new Dictionary<State, List<List<string[]>>[]>();
            foreach (var input in this.SatExamples.Keys)
            {
                result[f(input)] = this.SatExamples[input];
            }
            return new DisjunctiveCriteriaSatSpec(Allowed, LastTuple, result);
        }

        protected override bool EqualsOnInput(State state, Spec spec)
        {
            var other = spec as DisjunctiveCriteriaSatSpec;
            if (other == null) return false;
            if ((LastTuple == null) != (other.LastTuple == null)) return false;
            if (LastTuple != null)
            {
                if (LastTuple[state] != other.LastTuple[state]) return false;
            }
            if (SatExamples[state].Length != other.SatExamples[state].Length) return false;
            for (int v = 0; v < SatExamples[state].Length; v++)
            {//each possibility
                if (SatExamples[state][v].Count != other.SatExamples[state][v].Count) return false;
                for (int u = 0; u < SatExamples[state][v].Count; u++)
                {//each component
                    if (SatExamples[state][v][u].Count != other.SatExamples[state][v][u].Count) return false;
                    for (int w = 0; w < SatExamples[state][v][u].Count; w++)
                    {//each option
                        if (!SatExamples[state][v][u][w].SequenceEqual(other.SatExamples[state][v][u][w])) return false;
                    }
                }
            }
            return true;
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
