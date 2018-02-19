using System.Collections.Generic;
using System.IO;
using CloudDiff.CustomExceptions;
using CloudDiff.Parser;
using CloudDiff.Processor;
using CloudDiff.Structures;

// ReSharper disable InconsistentNaming
namespace CloudDiff.Beatmap
{
    //  Represents a beatmap. It contains all information about a single osu-file.
    public struct BeatmapInfo
    {
        public Metadata Data { get; }

        public List<Note> Notes { get; }

        public List<LongNote> LNs { get; }

        public double JenksDen { get; }

        public double CorJenksDen { get; }

        public BeatmapInfo(string filename)
        {
            List<Note> notes;
            List<LongNote> lns;

            //  Load, and parse.
            if (File.Exists(filename))
            {
                if(filename.Split('.')[filename.Split('.').Length - 1] != "osu")
                    throw new InvalidBeatmapException("Unknown file format.");

                Data = MetadataParser.Parse(filename);
                HitObjectParser.Parse(filename, out notes, out lns, Data.Keys);
            }
            else
                throw new FileNotFoundException();

            //  Calculate densities.
            JenksDen = DensityCalculator.GetJenksDensity(ref notes, ref lns);
            CorJenksDen = DensityCalculator.GetCorrectedJenksDensity(ref notes, ref lns, Data.Keys);

            //  Copy data.
            Notes = notes;
            LNs = lns;
        }
    }
}