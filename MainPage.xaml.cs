// MainPage.xaml.cs - Code-behind for the main page
using EegMonitor.Models;
using EegMonitor.Services;
using Microsoft.Maui.Graphics;
using System.Security.Cryptography.X509Certificates;

namespace EegMonitor
{
    [XamlCompilation(XamlCompilationOptions.Compile)] // Add this attribute

    public static class Globals
    {

    }
    public partial class MainPage : ContentPage
    {
        private readonly MockEegGenerator _mockEegGenerator = new MockEegGenerator();
        private bool _isRunning = false;
        private IDispatcherTimer _timer;

        // For rendering the raw EEG graph
        private readonly Queue<double> _dataPoints = new Queue<double>();
        private const int MaxDataPoints = 128;  // Store last 128 data points for display

        // Maximum expected value for frequency bands (for normalizing progress bars)
        private const double MaxFrequencyValue = 1.0; //10.0;

        public MainPage()
        {
            InitializeComponent();
            // Set up EegData type for broad use
            _eegData = new EegData            
            {
                RawValues = new List<double>(),
                Delta = 0,
                Theta = 0,
                Alpha = 0,
                Beta = 0,
                Gamma = 0
            };

            
            // Initialize graphics view
            RawDataGraphicsView.Drawable = new EegGraphDrawable(_dataPoints);

            // Timer
            _timer = Dispatcher.CreateTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(100); // Update at 10Hz
            _timer.Tick += Timer_Tick;                       
        }
        private EegData _eegData;

        private void OnStartStopClicked(object sender, EventArgs e)
        {                  
            if (_isRunning)
            {
                // Stop
                _timer.Stop();
                _isRunning = false;
                StartStopButton.Text = "Start";
                StatusLabel.Text = "Stopped";
                double[] allData = _dataPoints.ToArray();
                _mockEegGenerator.CalculateFrequencyBands(allData, _eegData);
                UpdateFrequencyBandsDisplay(_eegData); // Calculate FFT here
                
            }
            else
            {
                // Start
                _timer.Start();
                _isRunning = true;
                StartStopButton.Text = "Stop";
                StatusLabel.Text = "Running";
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            // Generate new EEG data
            _eegData = _mockEegGenerator.GenerateData();

            // Update raw data display
            UpdateRawDataDisplay(_eegData);

            // Update frequency bands display
            // Was moved to Stop as FFT currently take enough time to be slow on tick.
            // Could potentially calculate it on every N ticks
            //UpdateFrequencyBandsDisplay(_eegData);
        }

        private void UpdateRawDataDisplay(EegData eegData)
        {
            // Add new data point to the queue
            double rawValue = eegData.RawValues[0]; // Just use the first channel
            _dataPoints.Enqueue(rawValue);

            // Remove old data points if we have too many
            while (_dataPoints.Count > MaxDataPoints)
            {
                _dataPoints.Dequeue();
            }
            
            // Trigger redraw of the graph
            RawDataGraphicsView.Invalidate();

            // Update the current value label
            RawValueLabel.Text = $"Current Value: {rawValue:F2}";
        }

        private void UpdateFrequencyBandsDisplay(EegData eegData)
        {
            var maxPower = Math.Max(eegData.Delta, Math.Max(eegData.Theta, Math.Max(eegData.Alpha, Math.Max(eegData.Beta, eegData.Gamma))));
            // Update Delta            
            DeltaProgressBar.Progress = Math.Min(maxPower, eegData.Delta / maxPower);
            
            DeltaValueLabel.Text = $"{eegData.Delta:F1}";

            // Update Theta            
            ThetaProgressBar.Progress = Math.Min(maxPower, eegData.Theta / maxPower);
            ThetaValueLabel.Text = $"{eegData.Theta:F1}";

            // Update Alpha            
            AlphaProgressBar.Progress = Math.Min(maxPower, eegData.Alpha / maxPower);
            AlphaValueLabel.Text = $"{eegData.Alpha:F1}";

            // Update Beta            
            BetaProgressBar.Progress = Math.Min(maxPower, eegData.Beta / maxPower);
            BetaValueLabel.Text = $"{eegData.Beta:F1}";

            // Update Gamma            
            GammaProgressBar.Progress = Math.Min(maxPower, eegData.Gamma / maxPower);            
            GammaValueLabel.Text = eegData.Gamma.ToString("F2");            
        }
    }

    // Custom drawable for rendering the EEG graph
    public class EegGraphDrawable : IDrawable
    {
        private readonly Queue<double> _dataPoints;

        public EegGraphDrawable(Queue<double> dataPoints)
        {
            _dataPoints = dataPoints;
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (_dataPoints.Count < 2) return;

            // Set up drawing parameters
            canvas.StrokeColor = Colors.Green;
            canvas.StrokeSize = 2;

            float width = dirtyRect.Width;
            float height = dirtyRect.Height;
            float centerY = height / 2;
            float scaleY = height / 30; // Scale factor for the y-axis

            // Calculate x-increment based on number of points
            float xIncrement = width / (_dataPoints.Count - 1);

            // Start at x=0
            float x = 0;

            // Create points array for drawing the line
            var points = new PathF();
            bool firstPoint = true;

            foreach (var point in _dataPoints)
            {
                // Calculate y-position (invert because in UI, y increases downward)
                float y = centerY - (float)(point * scaleY);

                // Add point to the path
                if (firstPoint)
                {
                    points.MoveTo(x, y);
                    firstPoint = false;
                }
                else
                {
                    points.LineTo(x, y);
                }

                // Move to next x-position
                x += xIncrement;
            }

            // Draw the path
            canvas.DrawPath(points);

            // Draw the baseline (zero line)
            canvas.StrokeColor = Colors.Gray;
            canvas.StrokeSize = 1;
            canvas.DrawLine(0, centerY, width, centerY);
        }
    }
}