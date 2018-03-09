using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using CloudDiff.Structures;

namespace CloudDiff.Comparer
{
    public class NoteComparer : IComparer<INote>, IEqualityComparer<Note>, IEqualityComparer<LongNote>
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

        public bool Equals(Note x, Note y)
        {
            return x.Time == y.Time;
        }

        public int GetHashCode(Note obj)
        {
            return obj.Time.GetHashCode();
        }

        public bool Equals(LongNote x, LongNote y)
        {
            return x.Time == y.Time;
        }

        public int GetHashCode(LongNote obj)
        {
            return obj.Time.GetHashCode();
        }
    }
}
