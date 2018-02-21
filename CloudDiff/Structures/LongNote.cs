namespace CloudDiff.Structures
{
    //  Represents an LN.
    public struct LongNote : INote
    {
        public LongNote(int time, int endtime, int lane)
        {
            Time = time;
            EndTime = endtime;
            Lane = lane;
        }

        public int Time { get; set; }

        public int EndTime { get; set; }

        public int Lane { get; set; }
    }

    //  Represents an LN, including LN-counter.
    public class LongNoteCount
    {
        public LongNoteCount(int time, int endtime, int lane, int lns)
        {
            Time = time;
            EndTime = endtime;
            Lane = lane;
            LNs = lns;
        }

        public int Time { get; set; }

        public int EndTime { get; set; }

        public int Lane { get; set; }

        public int LNs { get; set; }
    }
}