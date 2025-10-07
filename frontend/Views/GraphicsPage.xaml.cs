using System;
using System.Windows;
using System.Windows.Controls;
using Debeon.Models;

namespace Debeon.Views
{
    public partial class GraphicsPage : Page
    {
        private readonly RobloxConfig _config;

        public GraphicsPage(RobloxConfig config)
        {
            InitializeComponent();
            _config = config;

            LoadSettings();
        }

        private void LoadSettings()
        {
            GraphicsQualitySlider.Value = _config.Graphics.GraphicsQuality;
            ShadowQualitySlider.Value = _config.Graphics.ShadowQuality;
            TextureQualitySlider.Value = _config.Graphics.TextureQuality;
            AntiAliasingSlider.Value = _config.Graphics.AntiAliasing;
            VsyncCheckBox.IsChecked = _config.Graphics.Vsync;
            FullscreenCheckBox.IsChecked = _config.Graphics.Fullscreen;
            RenderDistanceSlider.Value = _config.Graphics.RenderDistance;
            AnisotropicSlider.Value = _config.Graphics.AnisotropicFiltering;
            ParticleQualitySlider.Value = _config.Graphics.ParticleQuality;
            ResolutionWidthBox.Text = _config.Graphics.ResolutionWidth.ToString();
            ResolutionHeightBox.Text = _config.Graphics.ResolutionHeight.ToString();
        }

        private void GraphicsQualitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (GraphicsQualityValue != null)
            {
                int value = (int)e.NewValue;
                GraphicsQualityValue.Text = value.ToString();
                _config.Graphics.GraphicsQuality = (byte)value;
            }
        }

        private void ShadowQualitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ShadowQualityValue != null)
            {
                int value = (int)e.NewValue;
                string[] labels = { "Off", "Low", "Medium", "High" };
                ShadowQualityValue.Text = labels[value];
                _config.Graphics.ShadowQuality = (byte)value;
            }
        }

        private void TextureQualitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TextureQualityValue != null)
            {
                int value = (int)e.NewValue;
                TextureQualityValue.Text = value.ToString();
                _config.Graphics.TextureQuality = (byte)value;
            }
        }

        private void AntiAliasingSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (AntiAliasingValue != null)
            {
                int value = (int)e.NewValue;
                AntiAliasingValue.Text = value == 0 ? "Off" : $"{value}x";
                _config.Graphics.AntiAliasing = (byte)value;
            }
        }

        private void RenderDistanceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (RenderDistanceValue != null)
            {
                int value = (int)e.NewValue;
                RenderDistanceValue.Text = value.ToString();
                _config.Graphics.RenderDistance = (uint)value;
            }
        }

        private void AnisotropicSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (AnisotropicValue != null)
            {
                int value = (int)e.NewValue;
                AnisotropicValue.Text = value == 0 ? "Off" : $"{value}x";
                _config.Graphics.AnisotropicFiltering = (byte)value;
            }
        }

        private void ParticleQualitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ParticleQualityValue != null)
            {
                int value = (int)e.NewValue;
                ParticleQualityValue.Text = value.ToString();
                _config.Graphics.ParticleQuality = (byte)value;
            }
        }
    }
}
