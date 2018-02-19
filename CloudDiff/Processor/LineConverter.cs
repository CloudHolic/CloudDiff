using System;

namespace CloudDiff.Processor
{
    public class LineConverter
    {
        public LineConverter(int k)
        {
            Key = k;
        }

        public int Key { get; }

        public int GetLine(int coordinate)
        {
            return Math.Min(Key - 1, (int)Math.Floor((double)coordinate * Key / 512));
        }
    }
}