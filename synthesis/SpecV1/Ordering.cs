using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml.Linq;
using Microsoft.ProgramSynthesis;
using Microsoft.ProgramSynthesis.Specifications;
using Microsoft.ProgramSynthesis.Specifications.Serialization;

namespace Rest560SpecV1
{
    class PossibleOrderingsSpec : Spec
    {
        public IDictionary<State, (List<List<string[]>>[] Examples, HashSet<int> Available)> OrdExamples;

        public PossibleOrderingsSpec(IDictionary<State, (List<List<string[]>>[] Examples, HashSet<int> Available)> OrdExamples) : base(OrdExamples.Keys)
        {
            this.OrdExamples = OrdExamples;
            foreach (var w in OrdExamples)
            {
                if (w.Value.Item1.Length == 0) throw new ArgumentException("No possibilities- null should be returned instead of creating a spec.");
                foreach (var v in w.Value.Item1)
                {
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

        public (List<List<string[]>>[] Examples, HashSet<int> Available) sortBy(State state, Tuple<int, bool> candidate)
        {
            var dup = new HashSet<int>(OrdExamples[state].Available);
            dup.Remove(candidate.Item1);
            var result = new List<List<string[]>>[OrdExamples[state].Examples.Length];
            for (int p = 0; p < result.Length; p++)
            {
                var semiresult = new List<List<string[]>>();
                foreach (var component in OrdExamples[state].Examples[p])
                {
                    var semisemiresult = new List<string[]>();
                    for (int row = 0; row < component.Count - 1; row++)
                    {
                        semisemiresult.Add(component[row]);
                        int cmp = sqlordcompare(component[row][candidate.Item1], component[row + 1][candidate.Item1]);
                        if (cmp != 0)
                        {
                            if (semiresult.Count > 1) semiresult.Add(semisemiresult);
                            semisemiresult = new List<string[]>();
                        }
                    }
                    semisemiresult.Add(component[component.Count - 1]);
                    if (semiresult.Count > 1) semiresult.Add(semisemiresult);
                }
                result[p] = semiresult;
            }
            return (result, dup);
        }
        private static int sqlordcompare(string a, string b)
        {
            if (!double.TryParse(a, out double u) || !double.TryParse(b, out double v)) return a.CompareTo(b);
            return u.CompareTo(v);
        }
        public List<Tuple<int, bool>> GetSatisfiers(State state)
        {
            HashSet<(int, bool)> union = new HashSet<(int, bool)>();
            foreach (var possib in OrdExamples[state].Examples)
            {
                HashSet<(int, bool)> intersection = new HashSet<(int, bool)>();
                foreach (int col in OrdExamples[state].Available)
                {
                    intersection.Add((col, true));
                    intersection.Add((col, false));
                }
                foreach (var component in possib)
                {
                    for (int row = 0; row < component.Count - 1; row++)
                    {
                        for (int col = 0; col < component[row].Length; col++)
                        {
                            int cmp = sqlordcompare(component[row][col], component[row + 1][col]);
                            if (cmp < 0) { intersection.Remove((col, true)); }
                            if (cmp > 0) { intersection.Remove((col, false)); }
                        }
                    }
                }
                union.UnionWith(intersection);
            }
            var newunion = new List<Tuple<int, bool>>();
            foreach ((var a, var b) in union)
            {
                var j = new Tuple<int, bool>(a, b);
                newunion.Add(j);
            }
            Console.Out.WriteLine("found this many satisfiers: {0}", newunion.Count);
            return newunion;
        }
        protected override bool CorrectOnProvided(State state, object obj)
        {
            var candidate = obj as List<Tuple<int, bool>>;
            foreach ((int col, bool b) in candidate)
            {
                if (!OrdExamples[state].Available.Contains(col)) return false;
            }
            foreach (var possib in OrdExamples[state].Examples)
            {
                bool isok = true;
                foreach (var component in possib)
                {
                    for (int row = 0; row < component.Count - 1; row++)
                    {
                        foreach ((int col, bool dir) in candidate)
                        {
                            int cmp = sqlordcompare(component[row][col], component[row + 1][col]);
                            if (cmp < 0 && dir) { return false; }
                            if (cmp > 0 && !dir) { return false; }
                            if (cmp != 0) break;
                        }
                    }
                }
                if (isok) return true;
            }
            return false;
        }

        protected override int GetHashCodeOnInput(State state)
        {
            return this.OrdExamples[state].GetHashCode();
        }
        protected override Spec TransformInputs(Func<State, State> f)
        {
            var result = new Dictionary<State, (List<List<string[]>>[] Examples, HashSet<int> Available)>();
            foreach (var input in this.OrdExamples.Keys)
            {
                result[f(input)] = this.OrdExamples[input];
            }
            return new PossibleOrderingsSpec(result);
        }
        protected override bool EqualsOnInput(State state, Spec spec)
        {
            var other = spec as PossibleOrderingsSpec;
            if (other == null) return false;
            if (!OrdExamples[state].Item2.SetEquals(other.OrdExamples[state].Item2)) return false;
            if (OrdExamples[state].Item1.Length != other.OrdExamples[state].Item1.Length) return false;
            for (int v = 0; v < OrdExamples[state].Item1.Length; v++)
            {//each possibility
                if (OrdExamples[state].Item1[v].Count != other.OrdExamples[state].Item1[v].Count) return false;
                for (int u = 0; u < OrdExamples[state].Item1[v].Count; u++)
                {//each component
                    if (OrdExamples[state].Item1[v][u].Count != other.OrdExamples[state].Item1[v][u].Count) return false;
                    for (int w = 0; w < OrdExamples[state].Item1[v][u].Count; w++)
                    {//each option
                        if (!OrdExamples[state].Item1[v][u][w].SequenceEqual(other.OrdExamples[state].Item1[v][u][w])) return false;
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