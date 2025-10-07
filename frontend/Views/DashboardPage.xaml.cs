using System.Windows;
using System.Windows.Controls;
using Debeon.Services;
using Debeon.Models;

namespace Debeon.Views
{
    public partial class DashboardPage : Page
    {
        private readonly ApiService _apiService;
        private readonly RobloxConfig _config;

        public DashboardPage(ApiService apiService, RobloxConfig config)
        {
            InitializeComponent();
            _apiService = apiService;
            _config = config;

            Loaded += DashboardPage_Loaded;
        }

        private async void DashboardPage_Loaded(object sender, RoutedEventArgs e)
        {
            var installations = await _apiService.GetInstallationsAsync();

            if (installations.Count > 0)
            {
                var installation = installations[0];
                InstallationPathText.Text = installation.Path;
                VersionText.Text = installation.Version;
                ChannelText.Text = installation.Channel;
            }
            else
            {
                InstallationPathText.Text = "Not detected";
                VersionText.Text = "N/A";
                ChannelText.Text = "N/A";
            }

            UpdateConfigSummary();
        }

        private void UpdateConfigSummary()
        {
            GraphicsQualityText.Text = $"{_config.Graphics.GraphicsQuality}/21";
            FpsLimitText.Text = _config.Rendering.FrameRateLimit.HasValue ? $"{_config.Rendering.FrameRateLimit} FPS" : "Unlimited";
            VsyncText.Text = _config.Graphics.Vsync ? "Enabled" : "Disabled";
            AAText.Text = $"{_config.Graphics.AntiAliasing}x";
            VolumeText.Text = $"{(int)(_config.Audio.MasterVolume * 100)}%";
        }

        private void LoadDefaultProfile(object sender, RoutedEventArgs e)
        {
            _config.Graphics.GraphicsQuality = 10;
            _config.Rendering.FrameRateLimit = 60;
            _config.Graphics.Vsync = true;
            _config.Performance.LowLatencyMode = false;
            UpdateConfigSummary();
            MessageBox.Show("Default profile loaded.", "Profile", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LoadUltraProfile(object sender, RoutedEventArgs e)
        {
            _config.Graphics.GraphicsQuality = 21;
            _config.Graphics.ShadowQuality = 3;
            _config.Graphics.TextureQuality = 3;
            _config.Graphics.AntiAliasing = 8;
            _config.Rendering.DynamicLighting = true;
            _config.Rendering.PostProcessing = true;
            _config.Rendering.Bloom = true;
            _config.Rendering.AmbientOcclusion = true;
            _config.Rendering.Reflections = true;
            _config.Rendering.GlobalIllumination = true;
            UpdateConfigSummary();
            MessageBox.Show("Ultra graphics profile loaded.", "Profile", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LoadPerformanceProfile(object sender, RoutedEventArgs e)
        {
            _config.Graphics.GraphicsQuality = 1;
            _config.Graphics.ShadowQuality = 0;
            _config.Graphics.TextureQuality = 1;
            _config.Graphics.ParticleQuality = 1;
            _config.Graphics.Vsync = false;
            _config.Graphics.AntiAliasing = 0;
            _config.Rendering.FrameRateLimit = null;
            _config.Rendering.DynamicLighting = false;
            _config.Rendering.PostProcessing = false;
            _config.Rendering.Bloom = false;
            _config.Rendering.MotionBlur = false;
            _config.Rendering.AmbientOcclusion = false;
            _config.Performance.LowLatencyMode = true;
            UpdateConfigSummary();
            MessageBox.Show("Performance profile loaded.", "Profile", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
