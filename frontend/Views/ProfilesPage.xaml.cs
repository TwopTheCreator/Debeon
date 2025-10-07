using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Debeon.Services;
using Debeon.Models;

namespace Debeon.Views
{
    public partial class ProfilesPage : Page
    {
        private readonly ApiService _apiService;
        private readonly RobloxConfig _config;
        private readonly Action<RobloxConfig> _onProfileLoaded;

        public ProfilesPage(ApiService apiService, RobloxConfig config, Action<RobloxConfig> onProfileLoaded)
        {
            InitializeComponent();
            _apiService = apiService;
            _config = config;
            _onProfileLoaded = onProfileLoaded;

            Loaded += ProfilesPage_Loaded;
        }

        private async void ProfilesPage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadProfilesList();
        }

        private async System.Threading.Tasks.Task LoadProfilesList()
        {
            var profiles = await _apiService.GetProfilesAsync();
            ProfilesListBox.Items.Clear();

            foreach (var profile in profiles)
            {
                ProfilesListBox.Items.Add(profile);
            }
        }

        private async void SaveProfile(object sender, RoutedEventArgs e)
        {
            string profileName = ProfileNameBox.Text.Trim();

            if (string.IsNullOrEmpty(profileName))
            {
                MessageBox.Show("Please enter a profile name.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool success = await _apiService.SaveConfigAsync(profileName, _config);

            if (success)
            {
                MessageBox.Show($"Profile '{profileName}' saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                ProfileNameBox.Clear();
                await LoadProfilesList();
            }
            else
            {
                MessageBox.Show("Failed to save profile. Make sure the backend server is running.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadProfile(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var profileName = button?.DataContext as string;

            if (string.IsNullOrEmpty(profileName))
                return;

            var loadedConfig = await _apiService.GetConfigAsync(profileName);

            if (loadedConfig != null)
            {
                _config.Graphics = loadedConfig.Graphics;
                _config.Audio = loadedConfig.Audio;
                _config.Controls = loadedConfig.Controls;
                _config.Network = loadedConfig.Network;
                _config.Rendering = loadedConfig.Rendering;
                _config.Performance = loadedConfig.Performance;
                _config.UI = loadedConfig.UI;
                _config.CustomFlags = loadedConfig.CustomFlags;

                _onProfileLoaded?.Invoke(_config);

                MessageBox.Show($"Profile '{profileName}' loaded successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Failed to load profile.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeleteProfile(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var profileName = button?.DataContext as string;

            if (string.IsNullOrEmpty(profileName))
                return;

            var result = MessageBox.Show($"Are you sure you want to delete profile '{profileName}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                MessageBox.Show($"Profile '{profileName}' would be deleted here.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadProfilesList();
            }
        }

        private async void RefreshProfiles(object sender, RoutedEventArgs e)
        {
            await LoadProfilesList();
        }
    }
}
