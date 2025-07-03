// Services/MockEegGenerator.cs - Simple mock EEG generator
using EegMonitor.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using System.Diagnostics;
using FftSharp;



namespace EegMonitor.Services
{ 
        public class MockEegGenerator
    {
        private readonly Random _random = new Random();
        private double _lastValue = 0;

        // Number of EEG channels to simulate
        private const int ChannelCount = 1; // Just one channel for simplicity

        // Generate a new EEG data point
        public EegData GenerateData()
        {
            var data = new EegData
            {
                RawValues = new List<double>()
            };

            // Generate raw values for each channel
            for (int i = 0; i < ChannelCount; i++)
            {
                // Create simulated EEG data
                double newValue = _lastValue;

                // Components for sine waves
                double[] freqs = [1, 10, 18];//{10000000000.0, 7000, 3000};                
                double[] amps = [5, 8, 12];
                double[] yshifts = [0, 0.5, -0.5];
                double[] phases = [0, 45, 90];                
                //double mult = 1.0; //10000000.0;
                
                //newValue += Math.Sin(2 * Math.PI * f1 * DateTime.Now.Ticks);
                newValue += Math.Sin(DateTime.Now.Ticks / Math.PI * 180 * freqs[0] + phases[0]) * amps[0] + yshifts[0]
                    * Math.Sin(DateTime.Now.Ticks / Math.PI * 180 * freqs[1] + phases[1]) * amps[1] + yshifts[1]
                    * Math.Sin(DateTime.Now.Ticks / Math.PI * 180 * freqs[2] + phases[2]) * amps[2] + yshifts[2];
              
                // Add some random noise
                double noise_level = 10;                
                newValue += ((_random.NextDouble() * 2 - 1) * noise_level);

                // Constrain range to avoid drift
                newValue = Math.Clamp(newValue, -15, 15);

                _lastValue = newValue;
                data.RawValues.Add(newValue);
            }

            return data;
        }

        public void CalculateFrequencyBands(double[] pts, EegData data)
        {
            // FFT, get x data (frequencies) and y data (power)
            double[] fftPower = GetPowerSpectrum(pts);
            double[] frequencies = GetFrequencyScale(pts.Length, 256);

            // Get power in each band, see function for options
            var bandPowers = GetEegBandPowers(fftPower, frequencies);
            
            // Assign band powers to data
            data.Delta = bandPowers.Delta;
            data.Theta = bandPowers.Theta;
            data.Alpha = bandPowers.Alpha;
            data.Beta = bandPowers.Beta;
            data.Gamma = bandPowers.Gamma;
        }
        static double[] GetPowerSpectrum(double[] signal)
        {
            // Apply window function to reduce spectral leakage
            var window = new FftSharp.Windows.Hanning();
            double[] windowed = window.Apply(signal);

            // Get real FFT
            System.Numerics.Complex[] fft = FftSharp.FFT.Forward(windowed);

            // Convert to power spectrum
            double[] power = new double[fft.Length];
            for (int i = 0; i < fft.Length; i++)
            {
                power[i] = fft[i].Magnitude * fft[i].Magnitude;
            }

            return power;
        }
        static double[] GetFrequencyScale(int signalLength, double sampleRate)
            // Get a spaced scale of frequencies based on a signal
        {
            return FftSharp.FFT.FrequencyScale(signalLength, sampleRate);
        }

        static (double Delta, double Theta, double Alpha, double Beta, double Gamma) GetEegBandPowers(
    double[] powerSpectrum, double[] frequencies)
        {
            // Get the power within fixed ranges for each band.
            // Currently hardcoded ranges for the 5 common neural ranges.
            // THis function calculates several options, "return" section has multiple
            // commented out options, currently using 10*log10(v^2).

            // Fix min length
            int validLength = Math.Min(powerSpectrum.Length, frequencies.Length);

            // Find indices for each frequency band
            var deltaIndices = GetIndicesInRange(frequencies, 0.5, 4, validLength);
            var thetaIndices = GetIndicesInRange(frequencies, 4, 8, validLength);
            var alphaIndices = GetIndicesInRange(frequencies, 8, 13, validLength);
            var betaIndices = GetIndicesInRange(frequencies, 13, 30, validLength);
            var gammaIndices = GetIndicesInRange(frequencies, 30, 100, validLength);

            // Calculate average power in each band            
            double deltaPower = CalculateAveragePower(powerSpectrum, deltaIndices);
            double thetaPower = CalculateAveragePower(powerSpectrum, thetaIndices);
            double alphaPower = CalculateAveragePower(powerSpectrum, alphaIndices);
            double betaPower = CalculateAveragePower(powerSpectrum, betaIndices);
            double gammaPower = CalculateAveragePower(powerSpectrum, gammaIndices);

            // Calculate max power in each band
            double deltaMax = GetMaxPower(powerSpectrum, deltaIndices);
            double thetaMax = GetMaxPower(powerSpectrum, thetaIndices);
            double alphaMax = GetMaxPower(powerSpectrum, alphaIndices);
            double betaMax = GetMaxPower(powerSpectrum, betaIndices);
            double gammaMax = GetMaxPower(powerSpectrum, gammaIndices);

            // Calculate 10 * log10(V^2) power in each band
            double deltaLogV2 = CalculateLogV2(powerSpectrum, deltaIndices);
            double thetaLogV2 = CalculateLogV2(powerSpectrum, thetaIndices);
            double alphaLogV2 = CalculateLogV2(powerSpectrum, alphaIndices);
            double betaLogV2 = CalculateLogV2(powerSpectrum, betaIndices);
            double gammaLogV2 = CalculateLogV2(powerSpectrum, gammaIndices);
            
            // Can change comments
            //return (deltaPower, thetaPower, alphaPower, betaPower, gammaPower);
            //return (deltaMax, thetaMax, alphaMax, betaMax, gammaMax);
            //return (deltaPower/deltaMax, thetaPower / thetaMax, alphaPower / alphaMax, betaPower / betaMax, gammaPower / gammaMax);
            return (deltaLogV2, thetaLogV2, alphaLogV2, betaLogV2, gammaLogV2); 
        }

        static List<int> GetIndicesInRange(double[] frequencies, double minFreq, double maxFreq, int maxLength)
        {
            // Find indices of frequencies in the specified range
            return frequencies.Select((value, index) => new { Value = value, Index = index })
                             .Where(item => item.Value >= minFreq && item.Value <= maxFreq && item.Index < maxLength)
                             .Select(item => item.Index)
                             .ToList();
        }
        static double CalculateAveragePower(double[] powerSpectrum, List<int> indices)
        {
            // Average accross range. Generally returns an excessive delta value.
            if (indices.Count == 0)
                return 0;

            double sum = indices.Sum(index => powerSpectrum[index]);
            return sum / indices.Count;
        }

        static double GetMaxPower(double[] powerSpectrum, List<int> indices)
        {
            // Get max power in range. Mostly used to scale the average.
            if (indices.Count == 0)
                return 0;
            double max = powerSpectrum[indices[0]];
            foreach (var index in indices)
            {
                if (powerSpectrum[index] > max)
                    max = powerSpectrum[index];
            }
            return max;
        }

        static double CalculateLogV2(double[] powerSpectrum, List<int> indices)
        {
            // Calculate 10 * log10(V^2) power in each band.
            // One of the most common ways to calculate power, others can be used.
            if (indices.Count == 0)
                return 0;
            double sum = indices.Sum(index => 10*Math.Log10(Math.Pow(powerSpectrum[index],2)));
            return sum / indices.Count;
        }
    }
}