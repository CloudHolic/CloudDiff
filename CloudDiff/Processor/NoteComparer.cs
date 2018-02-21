using System.Collections.Generic;
using CloudDiff.Structures;

namespace CloudDiff.Processor
{
    public class NoteComparer : IComparer<INote>
    {
        public int Compare(INote x, INote y)
        {
            if (x == null)
            {
                if (y == null)
                    return 0;
                return -1;
            }

            return y == null ? 1 : x.Time.CompareTo(y.Time);
        }
    }
}
