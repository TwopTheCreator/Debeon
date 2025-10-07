using System.Windows;
using System.Windows.Controls;
using Debeon.Models;

namespace Debeon.Views
{
    public partial class ControlsPage : Page
    {
        private readonly RobloxConfig _config;

        public ControlsPage(RobloxConfig config)
        {
            InitializeComponent();
            _config = config;

            LoadSettings();
        }

        private void LoadSettings()
        {
            MouseSensitivitySlider.Value = _config.Controls.MouseSensitivity;
            InvertYAxisCheckBox.IsChecked = _config.Controls.InvertYAxis;
            GamepadEnabledCheckBox.IsChecked = _config.Controls.GamepadEnabled;
            GamepadSensitivitySlider.Value = _config.Controls.GamepadSensitivity;

            if (_config.Controls.KeyBindings.ContainsKey("forward"))
                ForwardKeyBox.Text = _config.Controls.KeyBindings["forward"];
            if (_config.Controls.KeyBindings.ContainsKey("backward"))
                BackwardKeyBox.Text = _config.Controls.KeyBindings["backward"];
            if (_config.Controls.KeyBindings.ContainsKey("left"))
                LeftKeyBox.Text = _config.Controls.KeyBindings["left"];
            if (_config.Controls.KeyBindings.ContainsKey("right"))
                RightKeyBox.Text = _config.Controls.KeyBindings["right"];
            if (_config.Controls.KeyBindings.ContainsKey("jump"))
                JumpKeyBox.Text = _config.Controls.KeyBindings["jump"];
        }

        private void MouseSensitivitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (MouseSensitivityValue != null)
            {
                MouseSensitivityValue.Text = e.NewValue.ToString("F2");
                _config.Controls.MouseSensitivity = (float)e.NewValue;
            }
        }

        private void GamepadSensitivitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (GamepadSensitivityValue != null)
            {
                GamepadSensitivityValue.Text = e.NewValue.ToString("F2");
                _config.Controls.GamepadSensitivity = (float)e.NewValue;
            }
        }

        private void ResetKeybindings(object sender, RoutedEventArgs e)
        {
            ForwardKeyBox.Text = "W";
            BackwardKeyBox.Text = "S";
            LeftKeyBox.Text = "A";
            RightKeyBox.Text = "D";
            JumpKeyBox.Text = "Space";

            _config.Controls.KeyBindings["forward"] = "W";
            _config.Controls.KeyBindings["backward"] = "S";
            _config.Controls.KeyBindings["left"] = "A";
            _config.Controls.KeyBindings["right"] = "D";
            _config.Controls.KeyBindings["jump"] = "Space";

            MessageBox.Show("Key bindings reset to default.", "Reset", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
