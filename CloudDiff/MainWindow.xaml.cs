using System;
using System.Diagnostics;
using System.Globalization;
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
        // ReSharper disable once InconsistentNaming
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
                var jack = new PatternAnalyzer(map.Notes, map.LNs, map.Data.Keys, map.Data.SpecialStyle);
                var specialStyle = map.Data.SpecialStyle ||
                    (map.Data.Keys == 8 && (double)(jack.Notes[0].Count + jack.LNs[0].Count) / jack.Count < 0.06);

                output = map.Data.Artist + " - " + map.Data.Title + " [" + map.Data.Diff + "]\nMade by " + map.Data.Creator
                         + "\nBPM: " + (Math.Abs(map.Data.MaxBpm - map.Data.MinBpm) < 0.001
                             ? Convert.ToString(map.Data.MaxBpm, CultureInfo.CurrentCulture) 
                             : map.Data.MinBpm + " - " + map.Data.MaxBpm + "\t")
                         + "\tOD: " + map.Data.Od + "\tHP: " + map.Data.Hp
                         + "\tKeys: " + (specialStyle ? Convert.ToString(map.Data.Keys - 1) + "+1" : Convert.ToString(map.Data.Keys))
#if DEBUG
                         + "\nJack Ratio: " + Math.Round(jack.GetOldJackRatio() * 100, 2) + "%   "
                         + "\tVibro Ratio: " + Math.Round(jack.GetVibroRatio() * 100, 2) + "%"
                         + "\tSpam Ratio: " + Math.Round(jack.GetSpamRatio() * 100, 2) + "%"
                         + "\nJenks Density: " + Math.Round(map.JenksDen, 2)
                         + "\tCorrected Jenks Density: " + Math.Round(map.CorJenksDen, 2)
#endif
                         + "\nRating: " + Math.Round(RatingCalculator.CalcRating(map), 2);
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