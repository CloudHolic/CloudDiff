using System;
using System.Collections.Generic;

namespace CloudDiff.Structures
{
    public struct Metadata
    {
        public Metadata(List<Tuple<double, double>> bpms, double hp, double od, int key, bool style,
            string title, string artist, string creator, string diff)
        {
            Bpms = bpms;
            Hp = hp;
            Od = od;
            Keys = key;
            SpecialStyle = style;

            Title = title;
            Artist = artist;
            Creator = creator;
            Diff = diff;
        }

        public List<Tuple<double, double>> Bpms { get; set; }

        public double Hp { get; set; }

        public double Od { get; set; }

        public int Keys { get; set; }

        public bool SpecialStyle { get; set; }

        public string Title { get; set; }

        public string Artist { get; set; }

        public string Creator { get; set; }

        public string Diff { get; set; }
    }
}
