using System;
using CloudDiff.Beatmap;

namespace CloudDiff.Processor
{
    public static class RatingCalculator
    {
        public static double CalcRating(BeatmapInfo map)
        {
            // Not implemented yet.
            return 0.0;
        }

        public static double CalcJackScore(PatternAnalyzer pat)
        {
            var jackCount = 0;

            foreach (var curJack in pat.JackSectionList)
            {
                foreach (var curNote in curJack)
                {

                }
            }

            return (double)jackCount / pat.Count * 100;
        }

        private static double GetJackTimesScore(int times)
        {
            return Math.Log(times, 3);
        }

        private static double GetJackBpmScore(double bpm)
        {
            return Math.Pow(1.025, bpm / 5) - 1;
        }
    }
}