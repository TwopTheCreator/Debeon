using System.Windows;
using ModernWpf;

namespace Debeon
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
            ThemeManager.Current.AccentColor = System.Windows.Media.Color.FromRgb(0, 120, 212);
        }
    }
}
