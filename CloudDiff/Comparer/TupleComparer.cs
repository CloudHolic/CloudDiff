using System;
using System.Collections.Generic;

namespace CloudDiff.Comparer
{
    public class TupleComparer: IComparer<Tuple<int, int>>
    {
        public int Compare(Tuple<int, int> x, Tuple<int, int> y)
        {
            if (x == null)
            {
                if (y == null)
                    return 0;
                return -1;
            }

            return y == null ? 1 : x.Item1.CompareTo(y.Item1);
        }
    }
}
