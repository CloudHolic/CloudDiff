using System;
using System.Collections.Generic;
using System.Linq;

using CloudDiff.Structures;
using CloudDiff.Utils;

// ReSharper disable InconsistentNaming
namespace CloudDiff.Processor
{
    public class PatternAnalyzer
    {
        public List<Note>[] Notes { get; }

        public List<LongNote>[] LNs { get; }

        public Dictionary<int, List<Tuple<int, int>>> Times { get; }

        public int Key { get; }

        public int Count { get; }

        public bool SpecialStyle { get; }

        //  Jack Cut-off points
        private readonly double Jack = BpmConverter.BpmToMilliseconds(140);
        private readonly double OnlyJack = BpmConverter.BpmToMilliseconds(160);
        private readonly double SpecialJack = BpmConverter.BpmToMilliseconds(180);

        //  Vibro Cut-off point
        private readonly double Vibro = BpmConverter.BpmToMilliseconds(170);

        public PatternAnalyzer(List<Note> notes, List<LongNote> lns, int key, bool specialstyle)
        {
            Key = key;
            Count = notes.Count + lns.Count;
            SpecialStyle = specialstyle;

            Notes = new List<Note>[key];
            LNs = new List<LongNote>[key];
            Times = new Dictionary<int, List<Tuple<int, int>>>();

            var tempDic = new Dictionary<int, List<Tuple<int, int>>>();

            for (var i = 0; i < key; i++)
            {
                Notes[i] = new List<Note>();
                LNs[i] = new List<LongNote>();
            }

            foreach (var cur in notes)
            {
                Notes[cur.Lane].Add(cur);

                if(!tempDic.ContainsKey(cur.Time))
                    tempDic.Add(cur.Time, new List<Tuple<int, int>>());
                tempDic[cur.Time].Add(Tuple.Create(cur.Lane, 0));
            }

            foreach (var cur in lns)
            {
                LNs[cur.Lane].Add(cur);

                if (!tempDic.ContainsKey(cur.Time))
                    tempDic.Add(cur.Time, new List<Tuple<int, int>>());
                tempDic[cur.Time].Add(Tuple.Create(cur.Lane, cur.Endtime));
            }

            var varList = tempDic.Keys.ToList();
            varList.Sort();
            foreach (var cur in varList)
            {
                var temp = tempDic[cur];
                temp.Sort(new TupleComparer());
                Times.Add(cur, temp);
            }
        }

        public double GetJackRatio()
        {
            var jackCount = 0;
            var sectionList = new List<int>();

            //  Jack: 3 or more notes which is in same lane with gap <= 107ms (140BPM, 1/4 snap).
            for (var i = 0; i < Notes.Length; i++)
            {
                for (var j = 0; j < Notes[i].Count - 2; j++)
                {
                    if (j + 2 > Notes[i].Count)
                        break;

                    //  Find a new jack section.
                    var gap1 = Notes[i][j + 1].Time - Notes[i][j].Time;
                    var gap2 = Notes[i][j + 2].Time - Notes[i][j + 1].Time;

                    //  If it's a scratch in 7+1k style, then 107ms => 84ms (180BPM, 1/4 snap).
                    if (SpecialStyle && i== 0 && (gap1 > SpecialJack || gap2 > SpecialJack))
                        continue;

                    if (gap1 > Jack || gap2 > Jack)
                        continue;

                    sectionList.Clear();
                    sectionList.Add(Notes[i][j].Time);
                    sectionList.Add(Notes[i][j + 1].Time);
                    sectionList.Add(Notes[i][j + 2].Time);
                    for (var k = j + 2; k < Notes[i].Count - 1; k++)
                    {
                        var gap = Notes[i][k + 1].Time - Notes[i][k].Time;
                        if ((SpecialStyle && gap > SpecialJack) || (!SpecialStyle && gap > Jack))
                        {
                            j = k;
                            break;
                        }

                        sectionList.Add(Notes[i][k + 1].Time);
                    }

                    //  Check if there're any other notes except jack
                    var onlyJack = true;
                    if (!(SpecialStyle && i == 0))
                    {
                        for (var k = 0; k < Notes.Length; k++)
                        {
                            if (k == i)
                                continue;

                            for (var l = 0; l < Notes[k].Count; l++)
                            {
                                if (Notes[k][l].Time < sectionList[0])
                                    continue;

                                if (Notes[k][l].Time > sectionList[0])
                                {
                                    onlyJack = onlyJack && Notes[k][l].Time > sectionList[sectionList.Count - 1];
                                    break;
                                }

                                var temp = true;
                                for (var m = 1; m < sectionList.Count; m++)
                                {
                                    if ((l + m < Notes[k].Count) && (Notes[k][l + m].Time == sectionList[m]))
                                        continue;

                                    temp = false;
                                    break;
                                }

                                onlyJack = onlyJack && temp;
                                break;
                            }

                            for (var l = 0; l < LNs[k].Count; l++)
                            {
                                if (LNs[k][l].Endtime <= sectionList[0])
                                    continue;
                                onlyJack = onlyJack && LNs[k][l].Time > sectionList[sectionList.Count - 1];
                                break;
                            }
                        }
                    }

                    //  If onlyJack, then 107ms => 94ms (160BPM, 1/4 snap).
                    if (!(SpecialStyle && i == 0) && onlyJack)
                    {
                        for (var k = 0; k < sectionList.Count - 2; k++)
                        {
                            if (k + 2 > sectionList.Count)
                                break;

                            var newGap1 = sectionList[k + 1] - sectionList[k];
                            var newGap2 = sectionList[k + 2] - sectionList[k + 1];

                            if (newGap1 > OnlyJack || newGap2 > OnlyJack)
                                continue;

                            jackCount++;
                            for (var l = k + 2; l < sectionList.Count - 1; l++)
                            {
                                if (sectionList[l + 1] - sectionList[l] > OnlyJack)
                                {
                                    k = l;
                                    break;
                                }

                                jackCount++;
                            }
                        }
                    }
                    else
                        jackCount += sectionList.Count - 2;
                }
            }

            return (double)jackCount / Count;
        }

        public double GetVibroRatio()
        {
            return 0.0;
        }

        public double GetSpamRatio()
        {
            var spamCount = 0;
            var sectionLines = new List<int>();
            var sectionList = new List<int>();

            //  Spam : 3 or more timings that contains notes in same lane (>= 3) consecutively, regardless of time.
            foreach (var cur in Times)
            {
                //  Find a new spam section.
                if (sectionLines.Count == 0 && sectionList.Count == 0)
                {
                    if (cur.Value.Count < 3)
                        continue;

                    sectionLines.AddRange(cur.Value.Select(x => x.Item1));
                    sectionList.Add(cur.Key);
                }
                else
                {
                    var temp = new List<int>();
                    temp.AddRange(cur.Value.Select(x => x.Item1));

                    var LNEnd = cur.Value.Aggregate(true, (current, t) => current && (cur.Value[0].Item2 == t.Item2));

                    var Lines = temp.Count == sectionLines.Count;
                    for (var i = 0; i < temp.Count; i++)
                        Lines = Lines && temp[i] == sectionLines[i];

                    if (LNEnd && Lines)
                        sectionList.Add(cur.Key);
                    else
                    {
                        if (sectionList.Count >= 3)
                            spamCount += sectionLines.Count * sectionList.Count;

                        sectionLines.Clear();
                        sectionList.Clear();

                        if (cur.Value.Count < 3)
                            continue;

                        sectionLines.AddRange(cur.Value.Select(x => x.Item1));
                        sectionList.Add(cur.Key);
                    }
                }
            }

            if(sectionList.Count >= 3)
                spamCount += sectionLines.Count * sectionList.Count;

            return (double)spamCount / Count;
        }
    }
}