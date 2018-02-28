using System;

namespace CloudDiff.Utils
{
    public static class BpmConverter
    {
        public static double BpmToMilliseconds(double bpm, int snap = 4)
        {
            if (Math.Abs(bpm) < 0.001)
                return 0;

            //  +1 for correction.
            return 60000 / (bpm * snap) + 1;
        }

        public static double MillisecondsToBpm(double ms, int snap = 4)
        {
            if (Math.Abs(ms) < 0.001)
                return 0;

            return 60000 / (ms * snap);
        }
    }
}
