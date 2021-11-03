using System.Collections.Generic;

namespace ProseTutorial
{    public static class Semantics {
        private static int sqlcompare(string a, string b) {
            if (!double.TryParse(a, out double u)||!double.TryParse(b, out double v)) return 0;
            return u.CompareTo(v);
        }
        public static List<string[]> Named(List<List<string[]>> inputs,int selector) {
            return inputs[selector];
        }
        public static List<string[]> Project(List<string[]> subq,List<int> projection) {
            var result = new List<string[]>();
            for (int i=0;i<subq.Count;i++) {
                var newrow = new string[projection.Count];
                for (int j=0;j<projection.Count;j++) newrow[j] = subq[i][projection[j]];
                result.Add(newrow);
            }
            return result;
        }
        public static List<string[]> Order(List<string[]> subq,List<int> keys) {
            subq.Sort(delegate(string[] c1, string[] c2) {
                foreach (int k in keys) {
                    var key = k;
                    if (key<0) {
                        key=1-key;
                        var cmp = sqlcompare(c1[key],c2[key]);
                        if (cmp==0) continue;
                        return cmp;
                    } else {
                        var cmp = sqlcompare(c1[key],c2[key]);
                        if (cmp==0) continue;
                        return -cmp;
                    }
                }
                return 0;
            });
            return subq;
        }
        public static List<string[]> Select(List<string[]> subq) {
            return subq;
        }
        public static List<string[]> Join(List<string[]> subq1,int a_col,List<string[]> subq2,int b_col) {
            var result = new List<string[]>();
            for (int a=0;a<subq1.Count;a++) {
                for (int b=0;b<subq2.Count;b++) {
                    if (subq1[a][a_col]==subq2[b][b_col]) {
                        var z = new string[subq1[a].Length+subq2[b].Length-1];
                        subq1[a].CopyTo(z, 0);
                        for (int y=0;y<subq2[b].Length;y++) {
                            if (y<b_col) z[y+subq1[a].Length]=subq2[b][y];
                            if (y>b_col) z[y+subq1[a].Length-1]=subq2[b][y];
                        }
                        result.Add(z);
                    }
                }
            }
            return result;
        }
        public static List<string[]> LeftJoin(List<string[]> subq1,int a_col,List<string[]> subq2,int b_col) {
            var result = new List<string[]>();
            for (int a=0;a<subq1.Count;a++) {
                var foundone = false;
                for (int b=0;b<subq2.Count;b++) {
                    if (subq1[a][a_col]==subq2[b][b_col]) {
                        var z = new string[subq1[a].Length+subq2[b].Length-1];
                        subq1[a].CopyTo(z, 0);
                        for (int y=0;y<subq2[b].Length;y++) {
                            if (y<b_col) z[y+subq1[a].Length]=subq2[b][y];
                            if (y>b_col) z[y+subq1[a].Length-1]=subq2[b][y];
                        }
                        foundone = true;
                        result.Add(z);
                    }
                }
                if (!foundone) {
                    var z = new string[subq1[a].Length+subq2[0].Length-1];
                    subq1[a].CopyTo(z, 0);
                    for (int y=0;y<subq2[0].Length;y++) {
                        if (y<b_col) z[y+subq1[a].Length]="";
                        if (y>b_col) z[y+subq1[a].Length-1]="";
                    }
                    foundone = true;
                    result.Add(z);
                }
            }
            return result;
        }
        public static List<string[]> Group(List<string[]> subq,List<int> groupby,List<int> aggregations) {
            var result = new List<string[]>();
            for (int row=0;row<subq.Count;row++) {
                var found = false;
                for (int lessrow=0;lessrow<result.Count;lessrow++) {
                    var samerec = true;
                    foreach (int factor in groupby) {
                        if (result[lessrow][factor]!=subq[row][factor]) samerec=false;
                    }
                    if (samerec) {
                        found = true;
                        var firstit = true;//there's some weird niche SQL semantics that i'm emulating here
                        for (int a=aggregations.Count-1;a>=0;a--) {
                            var ag = aggregations[a];
                            var reversed = 1;
                            if (ag<0) {
                                reversed = -1;
                                ag=1-ag;
                            }
                            if (sqlcompare(result[lessrow][ag],subq[row][ag])*reversed<0) {
                                if (firstit) {
                                    result[lessrow] = subq[row].Clone() as string[];
                                } else {
                                    result[lessrow][ag] = subq[row][ag];
                                }
                            }
                            firstit = false;
                        }
                        break;
                    }
                }
                if (!found) {
                    result.Add(subq[row]);
                }
            }
            return result;
        }



        public static List<string[]> N1(List<string[]> a) {return a;}
        public static List<string[]> N2(List<string[]> a) {return a;}
        public static List<string[]> N3(List<string[]> a) {return a;}
        public static List<string[]> N4(List<string[]> a) {return a;}
    }
}