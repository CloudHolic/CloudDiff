using System;
using System.Collections.Generic;
using System.Linq;
using CloudDiff.Comparer;
using CloudDiff.Structures;
using CloudDiff.Utils;

// ReSharper disable InconsistentNaming
namespace CloudDiff.Processor
{
    public class PatternAnalyzer
    {
        public int Key { get; }

        public int Count { get; }

        public bool SpecialStyle { get; }

        public List<List<INote>> JackSectionList { get; }

        private readonly List<INote>[] TotalNotes;

        private readonly Dictionary<int, List<Tuple<int, int>>> Times;

        //  Jack Cut-off points
        private readonly List<Tuple<double, double>> NewJacks;

        //  Vibro Cut-off point
        private readonly double Vibro = BpmConverter.BpmToMilliseconds(170);

        public PatternAnalyzer(List<Note> notes, List<LongNote> lns, int key, List<Tuple<double, double>> bpms, bool specialstyle)
        {
            Key = key;
            Count = notes.Count + lns.Count;
            SpecialStyle = specialstyle;
            
            TotalNotes = new List<INote>[key];
            Times = new Dictionary<int, List<Tuple<int, int>>>();
            JackSectionList = new List<List<INote>>();

            NewJacks = bpms.Select(cur => Tuple.Create(BpmConverter.BpmToMilliseconds(cur.Item1, 1), cur.Item2)).ToList();

            var tempDic = new Dictionary<int, List<Tuple<int, int>>>();

            for (var i = 0; i < key; i++)
                TotalNotes[i] = new List<INote>();

            foreach (var cur in notes)
            {
                TotalNotes[cur.Lane].Add(cur);

                if(!tempDic.ContainsKey(cur.Time))
                    tempDic.Add(cur.Time, new List<Tuple<int, int>>());
                tempDic[cur.Time].Add(Tuple.Create(cur.Lane, 0));
            }

            foreach (var cur in lns)
            {
                TotalNotes[cur.Lane].Add(cur);

                if (!tempDic.ContainsKey(cur.Time))
                    tempDic.Add(cur.Time, new List<Tuple<int, int>>());
                tempDic[cur.Time].Add(Tuple.Create(cur.Lane, cur.EndTime));
            }

            foreach (var cur in TotalNotes)
                cur.Sort(new NoteComparer());
            
            var varList = tempDic.Keys.ToList();
            varList.Sort();
            foreach (var cur in varList)
            {
                var temp = tempDic[cur];
                temp.Sort(new TupleComparer());
                Times.Add(cur, temp);
            }

            GetJackSections();
        }

        public static bool IsSpecialStyle(List<Note> notes, List<LongNote> lns)
        {
            var firstLineNotes = 0;
            var Count = notes.Count + lns.Count;

            foreach (var cur in notes)
                if (cur.Lane == 0)
                    firstLineNotes++;

            foreach (var cur in lns)
                if (cur.Lane == 0)
                    firstLineNotes++;

            return (double)firstLineNotes / Count < 0.06;
        }

        private void GetJackSections()
        {
            var tempList = new List<INote>();

            //  Just find all the 2 or more notes which is in same lane with gap <= (Current BPM, 1/4 snap)
            foreach (var curLane in TotalNotes)
            {
                for (var i = 0; i < curLane.Count - 1; i++)
                {
                    if (i + 1 > curLane.Count)
                        break;

                    //  Find a new jack section.
                    var gap = curLane[i + 1].Time - curLane[i].Time;

                    if (gap > GetJackCutOff(curLane[i].Time))
                        continue;

                    tempList.Clear();
                    tempList.Add(curLane[i]);
                    tempList.Add(curLane[i + 1]);

                    for (var j = i + 1; j < curLane.Count - 1; j++, i = j)
                    {
                        gap = curLane[j + 1].Time - curLane[j].Time;
                        if (gap > GetJackCutOff(curLane[j].Time))
                            break;

                        tempList.Add(curLane[j + 1]);
                    }

                    JackSectionList.Add(new List<INote>(tempList));
                }
            }

            JackSectionList.Sort(new JackComparer());
        }

        public double GetVibroRatio()
        {
            var vibroCount = 0;
            var sectionList = new List<int>();

            //  Vibro : 12 or more notes which is in same lane with gap <= 88ms (170BPM, 1/4 snap)
            foreach (var curLane in TotalNotes)
            {
                for (var i = 0; i < curLane.Count - 1; i++)
                {
                    if (i + 1 > curLane.Count)
                        break;

                    //  Find a new vibro section.
                    var gap = curLane[i + 1].Time - curLane[i].Time;

                    if (gap > Vibro)
                        continue;

                    sectionList.Clear();
                    sectionList.Add(curLane[i].Time);
                    sectionList.Add(curLane[i + 1].Time);

                    for (var j = i + 1; j < curLane.Count - 1; j++)
                    {
                        gap = curLane[j + 1].Time - curLane[j].Time;
                        if (gap > Vibro)
                        {
                            i = j;
                            break;
                        }

                        sectionList.Add(curLane[j + 1].Time);
                    }

                    if (sectionList.Count >= 12)
                        vibroCount += sectionList.Count;
                }
            }

            return (double)vibroCount / Count;
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

        private double GetJackCutOff(int time)
        {
            for (var i = 0; i < NewJacks.Count - 1; i++)
                if (time >= NewJacks[i].Item2 && time < NewJacks[i + 1].Item2)
                    return NewJacks[i].Item1;

            return NewJacks[NewJacks.Count - 1].Item1;
        }
    }
}