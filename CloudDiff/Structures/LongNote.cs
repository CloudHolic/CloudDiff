namespace CloudDiff.Structures
{
    //  Represents an LN.
    public struct LongNote
    {
        public LongNote(int time, int endtime, int lane)
        {
            Time = time;
            Endtime = endtime;
            Lane = lane;
        }

        public int Time { get; set; }

        public int Endtime { get; set; }

        public int Lane { get; set; }
    }

    //  Represents an LN, including LN-counter.
    public class LongNoteCount
    {
        public LongNoteCount(int time, int endtime, int lane, int lns)
        {
            Time = time;
            Endtime = endtime;
            Lane = lane;
            LNs = lns;
        }

        public int Time { get; set; }

        public int Endtime { get; set; }

        public int Lane { get; set; }

        public int LNs { get; set; }
    }
}