using System;
using CloudDiff.Beatmap;
using CloudDiff.Utils;
using FLS;

namespace CloudDiff.Processor
{
    public static class RatingCalculator
    {
        public static double CalcRating(BeatmapInfo map, PatternAnalyzer pat)
        {
            // Variable : CorJenksDen, Key, Od, Jack, Spam, Vibro
            
            // Not implemented yet.
            return 0.0;
        }

        public static double CalcJackScore(PatternAnalyzer pat)
        {
            var jackScore = 0.0;
            foreach (var curJack in pat.JackSectionList)
            {
                for (var i = 0 ; i < curJack.Count ; i++)
                {
                    double gap1 = 0, gap2 = 0;
                    if (i > 0)
                        gap1 = GetJackBpmScore(BpmConverter.MillisecondsToBpm(curJack[i].Time - curJack[i - 1].Time));
                    if (i < curJack.Count - 1)
                        gap2 = GetJackBpmScore(BpmConverter.MillisecondsToBpm(curJack[i + 1].Time - curJack[i].Time));

                    jackScore += (gap1 + gap2) / (Math.Abs(gap1) < 0.001 || Math.Abs(gap2) < 0.001 ? 1 : 2) * GetJackTimesScore(curJack.Count);
                }
            }

            return jackScore / pat.Count;
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