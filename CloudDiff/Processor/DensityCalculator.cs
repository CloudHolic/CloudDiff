using System;
using System.Collections.Generic;
using System.Linq;

using CloudDiff.Structures;

// ReSharper disable InconsistentNaming
namespace CloudDiff.Processor
{
    public static class DensityCalculator
    {
        public static double GetJenksDensity(ref List<Note> notes, ref List<LongNote> lns)
        {
            int[] groupCorrection = {1, 1, 1, 2, 4, 7, 9, 10, 10, 10};
            
            //  Get the list of densities.
            CalcDensityList(ref notes, ref lns, out var denList);

            //  Check if the density list is empty.
            if (denList.Count == 0)
                throw new InvalidOperationException("Empty list");

            var temp = denList.Select(x => (double)x).ToList();
            var corr = NaturalBreaks.CreateNaturalBreaksArray(temp, 10);
            var groupList = new List<int>(denList.Count);

            foreach (var cur in denList)
            {
                for (var j = 1; j < corr.Count + 1; j++)
                {
                    if (j == corr.Count || cur < corr[j])
                    {
                        groupList.Add(j - 1);
                        break;
                    }
                }
            }

            var jenksDen = 0.0;
            for (var i = 0; i < denList.Count; i++)
                jenksDen += denList[i] * groupCorrection[groupList[i]];

            var corGroupCount = 0;
            for (var i = 0; i < 10; i++)
                corGroupCount += groupList.Count(x => x == i) * groupCorrection[i];

            return jenksDen / corGroupCount;
        }

        public static double GetCorrectedJenksDensity(ref List<Note> notes, ref List<LongNote> lns, int key)
        {
            int[] groupCorrection = { 1, 1, 1, 2, 4, 7, 9, 10, 10, 10 };

            //  Get the list of densities.
            CalcCorrectedDensities(ref notes, ref lns, key, out var denList);

            //  Check if the density list is empty.
            if (denList.Count == 0)
                throw new InvalidOperationException("Empty list");

            var temp = denList.Select(x => (double)x).ToList();
            var corr = NaturalBreaks.CreateNaturalBreaksArray(temp, 10);
            var groupList = new List<int>(denList.Count);

            foreach (var cur in denList)
            {
                for (var j = 1; j < corr.Count + 1; j++)
                {
                    if (j == corr.Count || cur < corr[j])
                    {
                        groupList.Add(j - 1);
                        break;
                    }
                }
            }

            var corJenksDen = 0.0;
            for (var i = 0; i < denList.Count; i++)
                corJenksDen += denList[i] * groupCorrection[groupList[i]];

            var corGroupCount = 0;
            for (var i = 0; i < 10; i++)
                corGroupCount += groupList.Count(x => x == i) * groupCorrection[i];

            return corJenksDen / corGroupCount;
        }

        private static Tuple<int, int> GetPeriods(ref List<Note> notes, ref List<LongNote> lns)
        {
            int startTiming, endTiming;

            if (notes.Count == 0)
            {
                startTiming = lns[0].Time;
                endTiming = lns[lns.Count - 1].Time;
            }
            else if (lns.Count == 0)
            {
                startTiming = notes[0].Time;
                endTiming = notes[notes.Count - 1].Time;
            }
            else
            {
                startTiming = Math.Min(notes[0].Time, lns[0].Time);
                endTiming = Math.Max(notes[notes.Count - 1].Time, lns[lns.Count - 1].Time);
            }

            var startPeriod = startTiming - (startTiming % 250);
            var endPeriod = endTiming + (250 - (endTiming % 250));

            return Tuple.Create(startPeriod, endPeriod);
        }

        private static void CalcDensityList(ref List<Note> notes, ref List<LongNote> lns, out List<int> density)
        {
            density = new List<int>();

            var periods = GetPeriods(ref notes, ref lns);
            var startPeriod = periods.Item1;
            var endPeriod = periods.Item2;

            for (var i = startPeriod; i < endPeriod - 1000; i += 250)
            {
                var counts = 0;

                //  Count the number of simple notes.
                counts += notes.Count(cur => cur.Time >= i && cur.Time <= i + 1000);

                //  Count the number of long notes.
                counts += lns.Count(cur => (cur.Time >= i && cur.Time <= i + 1000)
                                    || (cur.Endtime >= i && cur.Endtime <= i + 1000) || (cur.Time <= i && cur.Endtime >= i + 1000));

                density.Add(counts);
            }
        }

        private static void CalcCorrectedDensities(ref List<Note> notes, ref List<LongNote> lns, int key, out List<double> density)
        {
            int Key;
            var pat = new PatternAnalyzer(notes, lns, key, false);

            var corNotes = new List<NoteCount>();
            var corLNs = new List<LongNoteCount>();

            var Notes = new List<Note>();
            var LNs = new List<LongNote>();

            density = new List<double>();
            var specialStyle = key==8 && (double)(pat.Notes[0].Count + pat.LNs[0].Count)/pat.Count < 0.06;
            if (!specialStyle)
            {
                Notes = notes;
                LNs = lns;
                Key = key;
            }
            else
            {
                Notes.AddRange(notes.Where(cur => cur.Line != 0));
                LNs.AddRange(lns.Where(cur => cur.Line != 0));
                Key = key - 1;
            }

            var periods = GetPeriods(ref notes, ref lns);
            var startPeriod = periods.Item1;
            var endPeriod = periods.Item2;

            for (var i = startPeriod; i < endPeriod - 1000; i += 250)
            {
                corNotes.Clear();
                corLNs.Clear();

                //  Get the notes in current period.
                corNotes.AddRange(from cur in Notes where cur.Time >= i && cur.Time <= i + 1000 select new NoteCount(cur.Time, cur.Line, 0));

                corLNs.AddRange(
                    from cur in LNs where (cur.Time >= i && cur.Time <= i + 1000)
                    || (cur.Endtime >= i && cur.Endtime <= i + 1000)
                    || (cur.Time <= i && cur.Endtime >= i + 1000)
                    select new LongNoteCount(cur.Time, cur.Endtime, cur.Line, 0));

                //  Count the LN-count for each notes.
                foreach (var cur in corLNs)
                {
                    foreach (var note in corNotes)
                    {
                        if (note.Time >= cur.Time && note.Time < cur.Endtime)
                            note.LNs++;
                    }

                    for (var j = corLNs.IndexOf(cur) + 1; j < corLNs.Count; j++)
                    {
                        if ((corLNs[j].Time >= cur.Time && corLNs[j].Time < cur.Endtime)
                            || (corLNs[j].Endtime >= cur.Time && corLNs[j].Endtime <= cur.Endtime)
                            || (corLNs[j].Time <= cur.Time && corLNs[j].Endtime >= cur.Endtime))
                            corLNs[j].LNs++;
                    }
                }

                //  Correct the density.
                var den = corNotes.Sum(cur => (double) Key / (Key - cur.LNs)) +
                          corLNs.Sum(cur => (double) Key / (Key - cur.LNs) * 1.1);

                density.Add(den);
            }
        }
    }
}