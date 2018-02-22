using System.Collections.Generic;
using CloudDiff.Structures;

namespace CloudDiff.Comparer
{
    public class JackComparer : IComparer<List<INote>>
    {
        public int Compare(List<INote> x, List<INote> y)
        {
            if (x == null)
            {
                if (y == null)
                    return 0;
                return -1;
            }

            return y == null ? 1 : x[0].Time.CompareTo(y[0].Time);
        }
    }
}