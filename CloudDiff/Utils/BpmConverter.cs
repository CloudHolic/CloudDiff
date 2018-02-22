namespace CloudDiff.Utils
{
    public static class BpmConverter
    {
        public static double BpmToMilliseconds(double bpm, int snap = 4)
        {
            //  +1 for correction.
            return 60000 / (bpm * snap) + 1;
        }

        public static double MillisecondsToBpm(double ms, int snap = 4)
        {
            return 60000 / (ms * snap);
        }
    }
}
