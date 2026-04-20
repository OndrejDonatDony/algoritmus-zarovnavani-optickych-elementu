using OpenCvSharp.WpfExtensions;
using OpticalElementsSimulator.model;
using System.Windows;

namespace SimulatorUI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void GeneratePreview_Click(object sender, RoutedEventArgs e)
        {
            var settings = new SimulationSettings
            {
                Noise = (int)NoiseSlider.Value,
                SampleRadius = (int)SampleRadiusSlider.Value,
                RefRadius = (int)RefRadiusSlider.Value,
                WidthExternal = int.TryParse(OuterWidthTextBox.Text, out int ow) ? ow : 1480,
                HeightExternal = int.TryParse(OuterHeightTextBox.Text, out int oh) ? oh : 1224,
                WidthInternal = int.TryParse(InnerWidthTextBox.Text, out int iw) ? iw : 1280,
                HeightInternal = int.TryParse(InnerHeightTextBox.Text, out int ih) ? ih : 1024,
                Seed = int.TryParse(SeedTextBox.Text, out int s) ? s : 12345
            };

            var runner = new SimulatorRunner();

            var result = runner.Run(settings);



            PreviewImage.Source = result.PreviewTrim.ToWriteableBitmap();
            ResultTextBox.Text = result.ResultText;
        }

        private void RunAlgorithm_Click(object sender, RoutedEventArgs e)
        {
            GeneratePreview_Click(sender, e);
        }
    }
}