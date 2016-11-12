using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Audio;
using NAudio.Dsp;

namespace AudioDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool toggle;
                
        public MainWindow()
        {
            InitializeComponent();
            var canvas = new Canvas();
            AddChild(canvas);

            var processor = new AudioProcessor();
            processor.OnFft += DrawRgframeInfo;
            processor.StartProcessor("Resources/callonme.mp3");
        }

        void DrawRgframeInfo(FrameInfo[] rgframeInfo)
        {
            var canvas = (Canvas)Content;
            canvas.Children.Clear();
            for (int i = 0; i < rgframeInfo.Length; i++)
            {
                var fi = rgframeInfo[i];
                var rect = new Rectangle();
                rect.Fill = fi.fBeat ? Brushes.Red : Brushes.Green;
                rect.Width = 1024d / rgframeInfo.Length;
                rect.Height = 20 * (fi.energy - Math.Log(AudioProcessor.energyMin));
                //rect.Height = 500;
                Canvas.SetLeft(rect, i * rect.Width);
                canvas.Children.Add(rect);
            }
        }

        void DrawBeats(BitArray beats)
        {
            var canvas = (Canvas)Content;
            canvas.Children.Clear();
            for (int i = 0; i < beats.Length; i++)
            {
                var rect = new Rectangle();
                rect.Fill = beats[i] ? Brushes.Red : Brushes.Green;
                rect.Width = 1024d / beats.Length;
                rect.Height = 100;
                Canvas.SetLeft(rect, i * rect.Width);
                canvas.Children.Add(rect);
            }
        }

        void DrawBarChart(Tuple<float, bool>[] data)
        {
            var canvas = (Canvas)Content;
            canvas.Children.Clear();
            var x = 0;
            for (int i = 0; i < data.Length; i++)
            {
                float val = data[i].Item1;
                var rect = new Rectangle();
                rect.Fill = data[i].Item2 ? Brushes.Red : Brushes.Green;
                rect.Width = 1024d / data.Length;
                rect.Height = 20000 * val;
                Canvas.SetLeft(rect, i * rect.Width);
                canvas.Children.Add(rect);
            }
        }

        void DrawBarChart(Complex[] result)
        {
            result = result.Take(result.Length/2).ToArray();
            var canvas = (Canvas)Content;
            canvas.Children.Clear();
            var x = 0;
            var nBatches = result.Length;
            var batchSize = result.Length/nBatches;
            for (int i = 0; i < nBatches; i++)
            {
                float sum = 0;
                for (int j = i*batchSize; j < (i + 1)*batchSize; j++)
                {
                    sum += result[j].X*result[j].X + result[j].Y*result[j].Y;
                }

                var rms = Math.Sqrt(sum)/batchSize;

                var rect = new Rectangle();
                rect.Fill = Brushes.Green;
                rect.Width = 1024d / result.Length;
                rect.Height = 200000 * rms;
                Canvas.SetLeft(rect, i * rect.Width);
                canvas.Children.Add(rect);
            }
        }
    }
}
