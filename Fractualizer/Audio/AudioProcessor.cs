using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Dsp;
using System.Diagnostics;

namespace Audio
{
    public class AudioProcessor
    {
        private float valI;
        public float val
        {
            get { lock (this) { return valI; } }
            set { lock (this) { valI = value; } }
        }

        private WaveOut waveOut;

        public void StartProcessor(string filename)
        {
            waveOut = new WaveOut { DesiredLatency = 200 };
            var reader = new AudioFileReader(filename);
            var sampleProvider = reader.ToSampleProvider();
            var aggregator = new SampleAggregator(sampleProvider);
            aggregator.PerformFFT = true;
            aggregator.FftCalculated += OnFftCalculated;
            waveOut.Init(aggregator);
            waveOut.Play();
        }

        private void OnFftCalculated(object sender, FftEventArgs e)
        {
            Debug.WriteLine(DateTime.Now);
            val = (float)Math.Sqrt(e.Result.Select(i => i.X * i.X + i.Y * i.Y).Sum());
        }
    }
}
