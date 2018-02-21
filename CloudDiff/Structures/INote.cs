namespace CloudDiff.Structures
{
    public interface INote
    {
        int Time { get; set; }

        int EndTime { get; set; }

        int Lane { get; set; }
    }
}
