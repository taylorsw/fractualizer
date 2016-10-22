using System;
using System.Linq;
using NAudio.Wave;

namespace Audio
{
    public class AudioProcessor
    {
        private float valI;
        private float minI = float.MaxValue;
        private float maxI = float.MinValue;
        public float val
        {
            get { lock (this) { return valI; } }
            set { lock (this) { valI = value; } }
        }
        public float min
        {
            get { lock (this) { return minI; } }
            set { lock (this) { minI = value; } }
        }
        public float max
        {
            get { lock (this) { return maxI; } }
            set { lock (this) { maxI = value; } }
        }

        private WaveOut waveOut;

        public void StartProcessor(string filename)
        {
            try
            {
                const int csampleFft = 1024;
                waveOut = new WaveOut { DesiredLatency = 200 };
                var reader = new AudioFileReader(filename);
                var sampleProvider = reader.ToSampleProvider();
                var aggregator = new SampleAggregator(sampleProvider, csampleFft);
                aggregator.PerformFFT = true;
                aggregator.NotificationCount = csampleFft;
                aggregator.FftCalculated += OnFftCalculated;
                waveOut.Init(aggregator);
                waveOut.Play();
                waveOut.Volume = 1.0f;
            }
            catch (Exception)
            {
                
            }
        }

        private void OnFftCalculated(object sender, FftEventArgs e)
        {
            val = (float)Math.Sqrt(e.Result.Select(i => i.X * i.X + i.Y * i.Y).Sum());
            max = Math.Max(val, max);
            min = Math.Min(val, min);
        }
    }
}
