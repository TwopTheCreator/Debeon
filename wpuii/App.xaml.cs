using System.Windows;
using Wpf.Ui.Appearance;

namespace wpuii
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            SystemThemeWatcher.Watch(this);
        }
    }
}
