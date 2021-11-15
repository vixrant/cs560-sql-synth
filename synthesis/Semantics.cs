﻿using System.Collections.Generic;
using System;

namespace Rest560 {

    public static class Semantics {
        public enum BinopCode {
            Eq=0,
            Neq=1,
            Lt=2,
            Lteq=3,
        }
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
            var subq2 = subq.ConvertAll(x=>x);
            subq2.Sort(delegate(string[] c1, string[] c2) {
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
            return subq2;
        }
        public static List<string[]> Select(List<string[]> subq,List<Tuple<int,int,int>> filters) {
            // Console.Out.WriteLine("SEMANTICS FOR SELECT CALLED");
            var subq2 = new List<string[]>();
            foreach (var row in subq) {
                var rowworks = true;
                foreach (var criteria in filters) {
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
                if (rowworks) subq2.Add(row);
            }
            return subq2;
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
        // public static List<string[]> LeftJoin(List<string[]> subq1,int a_col,List<string[]> subq2,int b_col) {
        //     var result = new List<string[]>();
        //     for (int a=0;a<subq1.Count;a++) {
        //         var foundone = false;
        //         for (int b=0;b<subq2.Count;b++) {
        //             if (subq1[a][a_col]==subq2[b][b_col]) {
        //                 var z = new string[subq1[a].Length+subq2[b].Length-1];
        //                 subq1[a].CopyTo(z, 0);
        //                 for (int y=0;y<subq2[b].Length;y++) {
        //                     if (y<b_col) z[y+subq1[a].Length]=subq2[b][y];
        //                     if (y>b_col) z[y+subq1[a].Length-1]=subq2[b][y];
        //                 }
        //                 foundone = true;
        //                 result.Add(z);
        //             }
        //         }
        //         if (!foundone) {
        //             var z = new string[subq1[a].Length+subq2[0].Length-1];
        //             subq1[a].CopyTo(z, 0);
        //             for (int y=0;y<subq2[0].Length;y++) {
        //                 if (y<b_col) z[y+subq1[a].Length]="";
        //                 if (y>b_col) z[y+subq1[a].Length-1]="";
        //             }
        //             foundone = true;
        //             result.Add(z);
        //         }
        //     }
        //     return result;
        // }
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

        public static List<Tuple<int,int,int>> One(Tuple<int,int,int> a) {return new List<Tuple<int,int,int>>{a};}
        public static List<Tuple<int,int,int>> More(Tuple<int,int,int> a,List<Tuple<int,int,int>> b) {b.Add(a);return b;}

    }
}