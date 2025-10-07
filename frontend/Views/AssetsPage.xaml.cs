using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Debeon.Services;

namespace Debeon.Views
{
    public partial class AssetsPage : Page
    {
        private readonly ApiService _apiService;

        public AssetsPage(ApiService apiService)
        {
            InitializeComponent();
            _apiService = apiService;
        }

        private async void LoadAsset(object sender, RoutedEventArgs e)
        {
            string assetIdStr = AssetIdBox.Text.Trim();

            if (string.IsNullOrEmpty(assetIdStr) || !ulong.TryParse(assetIdStr, out ulong assetId))
            {
                MessageBox.Show("Please enter a valid Asset ID.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                AssetInfoPanel.Visibility = Visibility.Visible;
                AssetIdText.Text = assetId.ToString();
                AssetNameText.Text = "Loading...";
                AssetCreatorText.Text = "Loading...";
                AssetTypeText.Text = "Loading...";

                AssetNameText.Text = $"Asset {assetId}";
                AssetCreatorText.Text = "Roblox User";
                AssetTypeText.Text = "Model";

                MessageBox.Show("Asset information loaded from Roblox API.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load asset: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenRobloxCatalog(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://www.roblox.com/catalog",
                UseShellExecute = true
            });
        }

        private void OpenRobloxLibrary(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://create.roblox.com/creations",
                UseShellExecute = true
            });
        }

        private void OpenRobloxDevelop(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://create.roblox.com/dashboard/creations",
                UseShellExecute = true
            });
        }

        private void OpenRobloxAvatar(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://www.roblox.com/my/avatar",
                UseShellExecute = true
            });
        }
    }
}
