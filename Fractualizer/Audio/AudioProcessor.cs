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
        public readonly Queue<FrameInfo> qframeInfo = new Queue<FrameInfo>();
        private readonly double[] histogram;
        private readonly int cFrameSample;
        public double energyAvg, dEnergyAvg;
        public FrameInfo frameInfoLast => frameInfoLastI;
        private FrameInfo frameInfoLastI;
        public FrameInfo[] RgframeInfo() => qframeInfo.ToArray();

        public BandData(int cFrameSample)
        {
            this.cFrameSample = cFrameSample;
            histogram = new double[cFrameSample];
        }

        public void AddFrameInfo(FrameInfo frameInfo)
        {
            qframeInfo.Enqueue(frameInfo);
            energyAvg += frameInfo.energy/cFrameSample;
            dEnergyAvg += frameInfo.dEnergy/cFrameSample;
            frameInfoLastI = frameInfo;
            if (qframeInfo.Count > cFrameSample)
            {
                var frameInfoOldest = qframeInfo.Dequeue();
                energyAvg -= frameInfoOldest.energy/cFrameSample;
                dEnergyAvg -= frameInfoOldest.dEnergy/cFrameSample;
            }

            if (!frameInfo.fBeat || qframeInfo.Count < cFrameSample)
                return;

            var rgframeInfo = RgframeInfo();
            int iFrameLast = qframeInfo.Count - 1;
            for (int diFrame = 1; diFrame <= iFrameLast; diFrame++)
            {
                histogram[diFrame] *= 1 - 0.04 / diFrame;
                if (rgframeInfo[iFrameLast - diFrame].fBeat)
                    histogram[diFrame]++;
            }
        }

        public int DiTempo(out double confidence)
        {
            int diBest = 0;
            double valMax = 0;
            double valTotal = 0;
            for (int i = 4; i < histogram.Length; i++)
            {
                valTotal += histogram[i];
                if (histogram[i] > valMax)
                {
                    valMax = histogram[i];
                    diBest = i;
                }
            }
            confidence = valTotal > 0 ? valMax/valTotal : 0;
            return diBest;
        }

        public double EnergyVariance()
        {
            double variance = 0;
            foreach (var fi in qframeInfo)
            {
                double deviation = fi.energy - energyAvg;
                variance += deviation*deviation;                
            }
            return variance / qframeInfo.Count;
        }

        public double DEnergyVariance()
        {
            double variance = 0;
            foreach (var fi in qframeInfo)
            {
                double deviation = fi.dEnergy - dEnergyAvg;
                variance += deviation * deviation;
            }
            return variance / qframeInfo.Count;
        }
    }

    public class AudioProcessor
    {
        private const int cFrameSample = 48;
        private const int cBand = 16;
        private readonly BandData[] rgbandData = new BandData[cBand];
        private readonly BandData bandDataAvg = new BandData(cFrameSample);

        private bool fBeatI;

        public bool fBeat
        {
            get
            {
                lock (this)
                {
                    bool fBeatT = fBeatI;
                    fBeatI = false;
                    return fBeatT;                    
                }
            }

            private set { lock (this) { fBeatI = value; } }
        }

        public float a = 8;
        public float b = -10f;

        public event Action<FrameInfo[]> OnFrameInfoCalculated;
        public event Action<BandData> OnBandDataCalculated;

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

        private int tempoCur = -1;
        private bool fFoundBeat;
        private int cFrameSinceBeat;
        private void OnFftCalculated(object sender, FftEventArgs e)
        {
            var freqs = e.Result.Take(e.Result.Length/2).ToArray();
            int cFreqBand = freqs.Length / 2 / cBand;

            var beats = new bool[cBand];
            var rgframeInfo = new FrameInfo[cBand];
            for (int iBand = 0; iBand < cBand; iBand++)
            {
                double energy = 0;
                for (int iFreq = iBand*cFreqBand; iFreq < (iBand + 1)*cFreqBand; iFreq++)
                    energy += Length2(freqs[iFreq]);

                double logEnergy = Math.Log(energy + 1);
                var bandData = rgbandData[iBand];
                double dEnergy = logEnergy - bandData.frameInfoLast.energy;
                //dEnergy = Math.Log(Math.Max(dEnergy, energyMin));
                if (dEnergy < 0)
                    dEnergy = 0;
                //else
                //    dEnergy *= dEnergy;

                //var c = 100;
                //bool fBeat = logEnergy > (a + b * bandData.EnergyVariance()) * bandData.energyAvg;
                bool fBeat = dEnergy > (a + b * bandData.EnergyVariance() / bandData.energyAvg) * bandData.dEnergyAvg;
                beats[iBand] = fBeat;

                var frameInfo = new FrameInfo(energy: logEnergy, dEnergy: dEnergy, fBeat: fBeat);
                rgframeInfo[iBand] = frameInfo;
                bandData.AddFrameInfo(frameInfo);
            }

            double confidenceMax = 0;
            int bestTempo = 0;
            int iBandBest = 0;
            for (int iBand = 0; iBand < rgbandData.Length; iBand++)
            {
                double confidence;
                int tempo = rgbandData[iBand].DiTempo(out confidence);
                if (confidence > confidenceMax)
                {
                    confidenceMax = confidence;
                    bestTempo = tempo;
                    iBandBest = iBand;
                }
            }

            if (bestTempo != tempoCur)
            {
                tempoCur = bestTempo;
                fFoundBeat = false;
            }

            if (!fFoundBeat && tempoCur > 0)
            {
                if (beats[iBandBest])
                {
                    var rgframeInfoT = rgbandData[iBandBest].RgframeInfo();
                    fFoundBeat = rgframeInfoT[rgframeInfoT.Length - tempoCur - 1].fBeat;
                    cFrameSinceBeat = 0;
                }
            }
            else if (fFoundBeat)
            {
                cFrameSinceBeat = (cFrameSinceBeat + 1)%tempoCur;
            }

            //bool fCurrentBeat = fFoundBeat && cFrameSinceBeat == 0;
            //bool fCurrentBeat = beats[iBandBest];
            //bool fCurrentBeat = beats.Contains(true);
            bool fCurrentBeat = beats[0];
            if (fCurrentBeat)
                this.fBeat = true;

            bandDataAvg.AddFrameInfo(new FrameInfo(0, 0, fCurrentBeat));
            OnFrameInfoCalculated?.Invoke(rgframeInfo);
            OnBandDataCalculated?.Invoke(bandDataAvg);
        }

        private static float Length2(Complex complex)
        {
            return complex.X*complex.X + complex.Y*complex.Y;
        }
    }
}
