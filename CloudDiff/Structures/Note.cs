namespace CloudDiff.Structures
{
    //  Represents a simple note, not LN.
    public struct Note : INote
    {
        public Note(int time, int lane)
        {
            Time = time;
            EndTime = 0;
            Lane = lane;
        }

        public int Time { get; set; }

        public int EndTime { get; set; }

        public int Lane { get; set; }
    }

    //  Represents a simple note, including LN-counter.
    public class NoteCount
    {
        public NoteCount(int time, int lane, int lns)
        {
            Time = time;
            Lane = lane;
            LNs = lns;
        }

        public int Time { get; set; }

        public int Lane { get; set; }

        public int LNs { get; set; }
    }
}
