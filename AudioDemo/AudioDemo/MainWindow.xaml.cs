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
        private readonly AudioProcessor processor = new AudioProcessor();

        public MainWindow()
        {
            InitializeComponent();
            var canvas = new Canvas();
            AddChild(canvas);
            PreviewKeyDown += OnKeyDown;

            //processor.OnFrameInfoCalculated += DrawRgframeInfo;
            processor.OnBandDataCalculated += DrawBandData;
            processor.StartProcessor("Resources/dontletmedown.mp3");
        }

        void OnKeyDown(object o, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Q:
                    processor.a /= 1.05f;
                    break;
                case Key.W:
                    processor.a *= 1.05f;
                    break;
                case Key.A:
                    processor.b /= 1.05f;
                    break;
                case Key.S:
                    processor.b *= 1.05f;
                    break;
            }
        }

        void DrawBandData(BandData bandData)
        {
            DrawBarChart(bandData.qframeInfo.Select(fi => new Tuple<double, bool>(fi.dEnergy, fi.fBeat)).ToArray());
        }

        void DrawRgframeInfo(FrameInfo[] rgframeInfo)
        {
            DrawBarChart(rgframeInfo.Select(fi => new Tuple<double, bool>(fi.dEnergy, fi.fBeat)).ToArray());
        }

        void DrawBarChart(Tuple<double, bool>[] data)
        {
            var canvas = (Canvas)Content;
            canvas.Children.Clear();
            for (int i = 0; i < data.Length; i++)
            {
                var val = data[i].Item1;
                var rect = new Rectangle();
                rect.Fill = data[i].Item2 ? Brushes.Red : Brushes.Green;
                rect.Width = 1024d / data.Length;
                //rect.Height = 50 * val;
                rect.Height = 500;
                //rect.Height = Math.Max(200 * (val + 6), 0);
                Canvas.SetLeft(rect, i * rect.Width);
                canvas.Children.Add(rect);
            }
            var panel = new StackPanel();
            panel.Children.Add(new TextBlock {Text = "a: " + processor.a});
            panel.Children.Add(new TextBlock {Text = "b: " + processor.b});
            canvas.Children.Add(panel);
        }
    }
}
