using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using CloudDiff.Beatmap;
using CloudDiff.Processor;

namespace CloudDiff
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow
    {
        //  ReSharper disable once InconsistentNaming
        private bool isCalculating;

        public MainWindow()
        {
            InitializeComponent();
            isCalculating = false;
        }

        private void Window_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
                return;

            var files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);

            PathText.Text = files?[0];
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Filter = @".osu files (*.osu)|*.osu",
                RestoreDirectory = true
            };

            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                PathText.Text = ofd.FileName;
        }

        private void Calc_Click(object sender, RoutedEventArgs e)
        {
            if (isCalculating)
                return;

            StateBlock.Text = "Calculating...";
            var calcThread = new Thread(CalcRating);
            calcThread.Start();
        }

        private void CalcRating()
        {
            string text = null, output;
            isCalculating = true;

            try
            {
#if DEBUG
                var sw = new Stopwatch();
                sw.Start();
#endif
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                {
                    text = PathText.Text;
                }));

                var map = new BeatmapInfo(text);
                var jack = new PatternAnalyzer(map.Notes, map.LNs, map.Data.Keys, map.Data.Bpms, map.Data.SpecialStyle);
                var specialStyle = map.Data.SpecialStyle ||
                                   (map.Data.Keys == 8 && PatternAnalyzer.IsSpecialStyle(map.Notes, map.LNs));
                var maxBpm = Math.Round(map.Data.Bpms.Select(cur => cur.Item1).Max(), 2);
                var minBpm = Math.Round(map.Data.Bpms.Select(cur => cur.Item1).Min(), 2);
                
                output = map.Data.Artist + " - " + map.Data.Title + " [" + map.Data.Diff + "]\nMade by " + map.Data.Creator
                         + "\nBPM: " + (Math.Abs(maxBpm - minBpm) < 0.001 ? $"{maxBpm}" : $"{minBpm} - {maxBpm}\t")
                         + "\tOD: " + map.Data.Od + "\tHP: " + map.Data.Hp
                         + "\tKeys: " + (map.Data.Keys == 8 || specialStyle ? Convert.ToString(map.Data.Keys - 1) + "+1" : Convert.ToString(map.Data.Keys))
#if DEBUG
                         + "\nJack Score: " + Math.Round(RatingCalculator.CalcJackScore(jack), 2) + "    "
                         + "\tVibro Ratio: " + Math.Round(jack.GetVibroRatio() * 100, 2) + "%"
                         + "\tSpam Ratio: " + Math.Round(jack.GetSpamRatio() * 100, 2) + "%"
                         + "\nDensity Score: " + Math.Round(map.CorJenksDen, 2)
                         + "\tSpeed Score: " + Math.Round(map.JenksSpeed, 2)
#endif
                         + "\nRating: " + Math.Round(RatingCalculator.CalcRating(map, jack), 2);
#if DEBUG
                sw.Stop();

                output += "\nElapsed Time: " + sw.ElapsedMilliseconds;
#endif
            }
            catch (Exception ex)
            {
                output = ex.Message;
            }

            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            {
                StateBlock.Text = output;
            }));

            isCalculating = false;
        }
    }
}