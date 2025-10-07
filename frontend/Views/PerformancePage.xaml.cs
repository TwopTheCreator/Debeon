using System.Windows;
using System.Windows.Controls;
using Debeon.Models;

namespace Debeon.Views
{
    public partial class PerformancePage : Page
    {
        private readonly RobloxConfig _config;

        public PerformancePage(RobloxConfig config)
        {
            InitializeComponent();
            _config = config;

            LoadSettings();
        }

        private void LoadSettings()
        {
            if (_config.Rendering.FrameRateLimit.HasValue)
            {
                FpsLimitSlider.Value = _config.Rendering.FrameRateLimit.Value;
                UnlimitedFpsCheckBox.IsChecked = false;
            }
            else
            {
                UnlimitedFpsCheckBox.IsChecked = true;
                FpsLimitSlider.IsEnabled = false;
            }

            DynamicLightingCheckBox.IsChecked = _config.Rendering.DynamicLighting;
            PostProcessingCheckBox.IsChecked = _config.Rendering.PostProcessing;
            BloomCheckBox.IsChecked = _config.Rendering.Bloom;
            DepthOfFieldCheckBox.IsChecked = _config.Rendering.DepthOfField;
            MotionBlurCheckBox.IsChecked = _config.Rendering.MotionBlur;
            AmbientOcclusionCheckBox.IsChecked = _config.Rendering.AmbientOcclusion;
            ReflectionsCheckBox.IsChecked = _config.Rendering.Reflections;
            GlobalIlluminationCheckBox.IsChecked = _config.Rendering.GlobalIllumination;

            LowLatencyModeCheckBox.IsChecked = _config.Performance.LowLatencyMode;
            PowerSavingModeCheckBox.IsChecked = _config.Performance.PowerSavingMode;
        }

        private void FpsLimitSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (FpsLimitValue != null)
            {
                int value = (int)e.NewValue;
                FpsLimitValue.Text = $"{value} FPS";

                if (UnlimitedFpsCheckBox?.IsChecked == false)
                {
                    _config.Rendering.FrameRateLimit = (uint)value;
                }
            }
        }

        private void UnlimitedFpsCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (UnlimitedFpsCheckBox.IsChecked == true)
            {
                FpsLimitSlider.IsEnabled = false;
                FpsLimitValue.Text = "Unlimited";
                _config.Rendering.FrameRateLimit = null;
            }
            else
            {
                FpsLimitSlider.IsEnabled = true;
                _config.Rendering.FrameRateLimit = (uint)FpsLimitSlider.Value;
                FpsLimitValue.Text = $"{(int)FpsLimitSlider.Value} FPS";
            }
        }
    }
}
