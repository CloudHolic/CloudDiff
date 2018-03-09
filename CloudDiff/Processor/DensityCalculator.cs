using System;
using System.Collections.Generic;
using System.Linq;
using CloudDiff.Comparer;
using CloudDiff.Structures;

//  ReSharper disable InconsistentNaming
namespace CloudDiff.Processor
{
    public static class DensityCalculator
    {
        public static double GetJenksDensity(ref List<Note> notes, ref List<LongNote> lns, int key)
        {
            //  Get the list of densities.
            CalcDensityList(ref notes, ref lns, key, out var denList);

            return ApplyJenks(ref denList);
        }

        public static double GetJenksSpeed(ref List<Note> notes, ref List<LongNote> lns)
        {
            //  Get the list of speeds.
            CalcSpeedList(ref notes, ref lns, out var tempList);
            var speedList = tempList.Select(x => (double)x).ToList();

            return ApplyJenks(ref speedList);
        }

        private static double ApplyJenks(ref List<double> refList)
        {
            int[] groupCorrection = { 1, 1, 1, 2, 4, 7, 9, 10, 10, 10 };

            //  Check if the speed list is empty.
            if (refList.Count == 0)
                throw new InvalidOperationException("Empty list");

            var corr = NaturalBreaks.CreateNaturalBreaksArray(refList, 10);
            var groupList = new List<int>(refList.Count);

            foreach (var cur in refList)
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

            var jenksVar = refList.Select((t, i) => t * groupCorrection[groupList[i]]).Sum();

            var GroupCount = 0;
            for (var i = 0; i < 10; i++)
                GroupCount += groupList.Count(x => x == i) * groupCorrection[i];

            return jenksVar / GroupCount;
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

        private static void CalcDensityList(ref List<Note> notes, ref List<LongNote> lns, int key, out List<double> density)
        {
            int Key;

            var corNotes = new List<NoteCount>();
            var corLNs = new List<LongNoteCount>();

            var Notes = new List<Note>();
            var LNs = new List<LongNote>();

            density = new List<double>();
            var specialStyle = PatternAnalyzer.IsSpecialStyle(notes, lns);
            if (!specialStyle)
            {
                Notes = notes;
                LNs = lns;
                Key = key;
            }
            else
            {
                Notes.AddRange(notes.Where(cur => cur.Lane != 0));
                LNs.AddRange(lns.Where(cur => cur.Lane != 0));
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
                corNotes.AddRange(from cur in Notes where cur.Time >= i && cur.Time <= i + 1000 select new NoteCount(cur.Time, cur.Lane, 0));

                corLNs.AddRange(
                    from cur in LNs
                    where (cur.Time >= i && cur.Time <= i + 1000)
                          || (cur.EndTime >= i && cur.EndTime <= i + 1000)
                          || (cur.Time <= i && cur.EndTime >= i + 1000)
                    select new LongNoteCount(cur.Time, cur.EndTime, cur.Lane, 0));

                //  Count the LN-count for each notes.
                foreach (var cur in corLNs)
                {
                    foreach (var note in corNotes)
                    {
                        if (note.Time >= cur.Time && note.Time < cur.EndTime)
                            note.LNs++;
                    }

                    for (var j = corLNs.IndexOf(cur) + 1; j < corLNs.Count; j++)
                    {
                        if ((corLNs[j].Time >= cur.Time && corLNs[j].Time < cur.EndTime)
                            || (corLNs[j].EndTime >= cur.Time && corLNs[j].EndTime <= cur.EndTime)
                            || (corLNs[j].Time <= cur.Time && corLNs[j].EndTime >= cur.EndTime))
                            corLNs[j].LNs++;
                    }
                }

                //  Correct the density.
                var den = corNotes.Sum(cur => (double)Key / (Key - cur.LNs)) +
                          corLNs.Sum(cur => (double)Key / (Key - cur.LNs) * 1.1);

                density.Add(den);
            }
        }

        private static void CalcSpeedList(ref List<Note> notes, ref List<LongNote> lns, out List<int> speed)
        {
            speed = new List<int>();

            var periods = GetPeriods(ref notes, ref lns);
            var startPeriod = periods.Item1;
            var endPeriod = periods.Item2;

            var distinctNotes = notes.Distinct(new NoteComparer()).ToArray();
            var distinctLns = lns.Distinct(new NoteComparer()).ToArray();

            for (var i = startPeriod; i < endPeriod - 1000; i += 250)
            {
                var counts = 0;

                //  Count the number of simple notes.
                counts += distinctNotes.Count(cur => cur.Time >= i && cur.Time <= i + 1000);

                //  Count the number of long notes.
                counts += distinctLns.Count(cur => cur.Time >= i && cur.Time <= i + 1000);

                speed.Add(counts);
            }
        }
    }
}