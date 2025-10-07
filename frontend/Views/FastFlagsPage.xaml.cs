using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Debeon.Services;
using Debeon.Models;

namespace Debeon.Views
{
    public partial class FastFlagsPage : Page
    {
        private readonly ApiService _apiService;
        private readonly RobloxConfig _config;

        public FastFlagsPage(ApiService apiService, RobloxConfig config)
        {
            InitializeComponent();
            _apiService = apiService;
            _config = config;

            RefreshFlagsList();
        }

        private async void LoadCurrentFlags(object sender, RoutedEventArgs e)
        {
            var flags = await _apiService.GetFlagsAsync();

            if (flags.Count > 0)
            {
                _config.CustomFlags.Clear();
                foreach (var flag in flags)
                {
                    _config.CustomFlags[flag.Key] = flag.Value;
                }
                RefreshFlagsList();
                MessageBox.Show($"Loaded {flags.Count} flags from Roblox configuration.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("No flags found or unable to connect to backend.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ClearAllFlags(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to clear all custom flags?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _config.CustomFlags.Clear();
                RefreshFlagsList();
                MessageBox.Show("All custom flags cleared.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ApplyUncapFpsPreset(object sender, RoutedEventArgs e)
        {
            _config.CustomFlags["DFIntTaskSchedulerTargetFps"] = "999";
            RefreshFlagsList();
            MessageBox.Show("Uncap FPS preset applied.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ApplyLowLatencyPreset(object sender, RoutedEventArgs e)
        {
            _config.CustomFlags["FFlagEnableLowLatencyMode"] = "true";
            _config.CustomFlags["DFIntConnectionMTUSize"] = "1492";
            RefreshFlagsList();
            MessageBox.Show("Low Latency preset applied.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ApplyUltraGraphicsPreset(object sender, RoutedEventArgs e)
        {
            _config.CustomFlags["DFIntDebugFRMQualityLevelOverride"] = "21";
            _config.CustomFlags["FIntRenderShadowIntensity"] = "100";
            _config.CustomFlags["DFIntTextureQualityOverride"] = "3";
            RefreshFlagsList();
            MessageBox.Show("Ultra Graphics preset applied.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ApplyPotatoModePreset(object sender, RoutedEventArgs e)
        {
            _config.CustomFlags["DFIntDebugFRMQualityLevelOverride"] = "1";
            _config.CustomFlags["FFlagDisablePostFx"] = "true";
            _config.CustomFlags["FIntRenderShadowIntensity"] = "0";
            RefreshFlagsList();
            MessageBox.Show("Potato Mode preset applied.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AddCustomFlag(object sender, RoutedEventArgs e)
        {
            string flagName = FlagNameBox.Text.Trim();
            string flagValue = FlagValueBox.Text.Trim();

            if (string.IsNullOrEmpty(flagName) || string.IsNullOrEmpty(flagValue))
            {
                MessageBox.Show("Please enter both flag name and value.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _config.CustomFlags[flagName] = flagValue;
            RefreshFlagsList();

            FlagNameBox.Clear();
            FlagValueBox.Clear();
        }

        private void RemoveCustomFlag(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var flagText = (button?.DataContext as string)?.Split('=')[0]?.Trim();

            if (!string.IsNullOrEmpty(flagText) && _config.CustomFlags.ContainsKey(flagText))
            {
                _config.CustomFlags.Remove(flagText);
                RefreshFlagsList();
            }
        }

        private async void SaveCustomFlags(object sender, RoutedEventArgs e)
        {
            bool success = await _apiService.SetFlagsAsync(_config.CustomFlags);

            if (success)
            {
                MessageBox.Show("Custom flags applied successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Failed to apply flags. Make sure the backend server is running.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshFlagsList()
        {
            CustomFlagsListBox.Items.Clear();
            foreach (var flag in _config.CustomFlags.OrderBy(f => f.Key))
            {
                CustomFlagsListBox.Items.Add($"{flag.Key} = {flag.Value}");
            }
        }
    }
}
