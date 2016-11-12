using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NAudio.Dsp;
using NAudio.Wave;

namespace Audio
{
    public struct FrameInfo
    {
        public readonly double energy, dEnergy;
        public readonly bool fBeat;

        public FrameInfo(double energy, double dEnergy, bool fBeat)
        {
            this.energy = energy;
            this.dEnergy = dEnergy;
            this.fBeat = fBeat;
        }
    }

    public class BandData
    {
        private readonly Queue<FrameInfo> qframeInfo = new Queue<FrameInfo>();
        private readonly int cFrameSample;
        public double energyAvg, dEnergyAvg;        
        public double energyLast;
        public FrameInfo[] RgframeInfo() => qframeInfo.ToArray();

        public BandData(int cFrameSample)
        {
            this.cFrameSample = cFrameSample;
        }

        public double EnergyAvg()
        {
            double avg = 0;
            foreach (var fi in qframeInfo)
                avg += fi.energy;

            return avg/cFrameSample;
        }


        public void AddFrameInfo(FrameInfo frameInfo)
        {
            qframeInfo.Enqueue(frameInfo);
            energyAvg += frameInfo.energy/cFrameSample;
            dEnergyAvg += frameInfo.dEnergy/cFrameSample;
            if (qframeInfo.Count > cFrameSample)
            {
                var frameInfoOldest = qframeInfo.Dequeue();
                energyAvg -= frameInfoOldest.energy/cFrameSample;
                dEnergyAvg -= frameInfoOldest.dEnergy/cFrameSample;
            }
        }

        public double EnergyVariance()
        {
            double variance = 0;
            foreach (var fi in qframeInfo)
            {
                double deviation = fi.energy - energyAvg;
                variance += deviation*deviation;                
            }
            return variance;
        }

        public double DEnergyVariance()
        {
            double variance = 0;
            foreach (var fi in qframeInfo)
            {
                double deviation = fi.dEnergy - dEnergyAvg;
                variance += deviation * deviation;
            }
            return variance;
        }
    }

    public class AudioProcessor
    {
        private const int cFrameSample = 10;
        private const int cBand = 32;
        private readonly BandData[] rgbandData = new BandData[cBand];
        private float valI;
        private float minI = float.MaxValue;
        private float maxI = float.MinValue;
        public float energy
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

        public event Action<FrameInfo[]> OnFft;

        private WaveOut waveOut;

        public void StartProcessor(string filename)
        {
            for (int i = 0; i < cBand; i++)
                rgbandData[i] = new BandData(cFrameSample);

            try
            {
                const int csampleFft = 2048;
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
            catch (Exception e)
            {
                
            }
        }

        public const double energyMin = 1e-30;
        private void OnFftCalculated(object sender, FftEventArgs e)
        {
            var freqs = e.Result.Take(e.Result.Length/2).ToArray();
            int cFreqBand = freqs.Length / 2 / cBand;

            BitArray beats = new BitArray(cBand);
            var rgframeInfo = new FrameInfo[cBand];
            for (int iBand = 0; iBand < cBand; iBand++)
            {
                double energy = 0;
                for (int iFreq = iBand*cFreqBand; iFreq < (iBand + 1)*cFreqBand; iFreq++)
                    energy += Length2(freqs[iFreq]);

                double logEnergy = Math.Log(Math.Max(energy, energyMin));
                var bandData = rgbandData[iBand];
                double dEnergy = energy - bandData.energyLast;
                if (dEnergy < 0)
                    dEnergy = 0;
                //else
                //    dEnergy *= dEnergy;

                //var c = 100;
                //bool fBeat = dEnergy > c * bandData.EnergyVariance() && energy > 1e-8;
                bool fBeat = logEnergy > .7 * bandData.EnergyAvg();
                beats[iBand] = fBeat;

                var frameInfo = new FrameInfo(energy: logEnergy, dEnergy: dEnergy, fBeat: fBeat);
                rgframeInfo[iBand] = frameInfo;
                bandData.AddFrameInfo(frameInfo);
                bandData.energyLast = energy;
            }

            OnFft?.Invoke(rgframeInfo);
        }

        private static float Length2(Complex complex)
        {
            return complex.X*complex.X + complex.Y*complex.Y;
        }
    }
}
