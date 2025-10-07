using System.Windows;
using System.Windows.Controls;
using Debeon.Models;

namespace Debeon.Views
{
    public partial class AudioPage : Page
    {
        private readonly RobloxConfig _config;

        public AudioPage(RobloxConfig config)
        {
            InitializeComponent();
            _config = config;

            LoadSettings();
        }

        private void LoadSettings()
        {
            MasterVolumeSlider.Value = _config.Audio.MasterVolume * 100;
            MusicVolumeSlider.Value = _config.Audio.MusicVolume * 100;
            SfxVolumeSlider.Value = _config.Audio.SfxVolume * 100;
            VoiceVolumeSlider.Value = _config.Audio.VoiceVolume * 100;
            SpatialAudioCheckBox.IsChecked = _config.Audio.SpatialAudio;
        }

        private void MasterVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (MasterVolumeValue != null)
            {
                int value = (int)e.NewValue;
                MasterVolumeValue.Text = $"{value}%";
                _config.Audio.MasterVolume = (float)(value / 100.0);
            }
        }

        private void MusicVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (MusicVolumeValue != null)
            {
                int value = (int)e.NewValue;
                MusicVolumeValue.Text = $"{value}%";
                _config.Audio.MusicVolume = (float)(value / 100.0);
            }
        }

        private void SfxVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SfxVolumeValue != null)
            {
                int value = (int)e.NewValue;
                SfxVolumeValue.Text = $"{value}%";
                _config.Audio.SfxVolume = (float)(value / 100.0);
            }
        }

        private void VoiceVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (VoiceVolumeValue != null)
            {
                int value = (int)e.NewValue;
                VoiceVolumeValue.Text = $"{value}%";
                _config.Audio.VoiceVolume = (float)(value / 100.0);
            }
        }
    }
}
