using System;
using System.Windows;
using System.Windows.Controls;
using Debeon.Services;
using Debeon.Models;

namespace Debeon.Views
{
    public partial class MainWindow : Window
    {
        private readonly ApiService _apiService;
        private RobloxConfig _currentConfig;

        public MainWindow()
        {
            InitializeComponent();
            _apiService = new ApiService();
            _currentConfig = new RobloxConfig();

            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            NavigateToDashboard(null, null);

            var installations = await _apiService.GetInstallationsAsync();
            if (installations.Count > 0)
            {
                StatusText.Text = $"Roblox {installations[0].Version} detected";
            }
            else
            {
                StatusText.Text = "No Roblox installation found";
            }
        }

        private void NavigateToDashboard(object sender, RoutedEventArgs e)
        {
            ContentFrame.Navigate(new DashboardPage(_apiService, _currentConfig));
            ResetButtonStyles();
            DashboardButton.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2D2D30"));
        }

        private void NavigateToGraphics(object sender, RoutedEventArgs e)
        {
            ContentFrame.Navigate(new GraphicsPage(_currentConfig));
            ResetButtonStyles();
            GraphicsButton.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2D2D30"));
        }

        private void NavigateToAudio(object sender, RoutedEventArgs e)
        {
            ContentFrame.Navigate(new AudioPage(_currentConfig));
            ResetButtonStyles();
            AudioButton.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2D2D30"));
        }

        private void NavigateToControls(object sender, RoutedEventArgs e)
        {
            ContentFrame.Navigate(new ControlsPage(_currentConfig));
            ResetButtonStyles();
            ControlsButton.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2D2D30"));
        }

        private void NavigateToPerformance(object sender, RoutedEventArgs e)
        {
            ContentFrame.Navigate(new PerformancePage(_currentConfig));
            ResetButtonStyles();
            PerformanceButton.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2D2D30"));
        }

        private void NavigateToFastFlags(object sender, RoutedEventArgs e)
        {
            ContentFrame.Navigate(new FastFlagsPage(_apiService, _currentConfig));
            ResetButtonStyles();
            FastFlagsButton.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2D2D30"));
        }

        private void NavigateToProfiles(object sender, RoutedEventArgs e)
        {
            ContentFrame.Navigate(new ProfilesPage(_apiService, _currentConfig, OnProfileLoaded));
            ResetButtonStyles();
            ProfilesButton.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2D2D30"));
        }

        private void NavigateToAssets(object sender, RoutedEventArgs e)
        {
            ContentFrame.Navigate(new AssetsPage(_apiService));
            ResetButtonStyles();
            AssetsButton.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2D2D30"));
        }

        private void ResetButtonStyles()
        {
            DashboardButton.Background = System.Windows.Media.Brushes.Transparent;
            GraphicsButton.Background = System.Windows.Media.Brushes.Transparent;
            AudioButton.Background = System.Windows.Media.Brushes.Transparent;
            ControlsButton.Background = System.Windows.Media.Brushes.Transparent;
            PerformanceButton.Background = System.Windows.Media.Brushes.Transparent;
            FastFlagsButton.Background = System.Windows.Media.Brushes.Transparent;
            ProfilesButton.Background = System.Windows.Media.Brushes.Transparent;
            AssetsButton.Background = System.Windows.Media.Brushes.Transparent;
        }

        private async void ApplyConfiguration(object sender, RoutedEventArgs e)
        {
            StatusText.Text = "Applying configuration...";

            bool success = await _apiService.ApplyConfigAsync(_currentConfig);

            if (success)
            {
                StatusText.Text = "Configuration applied successfully!";
                MessageBox.Show("Configuration has been applied to Roblox.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                StatusText.Text = "Failed to apply configuration";
                MessageBox.Show("Failed to apply configuration. Make sure the backend server is running.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnProfileLoaded(RobloxConfig config)
        {
            _currentConfig = config;
            StatusText.Text = "Profile loaded";
        }
    }
}
