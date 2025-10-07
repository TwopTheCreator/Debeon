using System;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace wpuii
{
    public partial class MainWindow : FluentWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            NavigationView.Loaded += NavigationView_Loaded;
            NavigationView.SelectionChanged += NavigationView_SelectionChanged;
        }

        private void NavigationView_Loaded(object sender, RoutedEventArgs e)
        {
            if (NavigationView.MenuItems.Count > 0)
            {
                NavigationView.SelectedItem = NavigationView.MenuItems[0];
                NavigateTo("home");
            }
        }

        private void NavigationView_SelectionChanged(Wpf.Ui.Controls.NavigationView sender, RoutedEventArgs args)
        {
            if (sender.SelectedItem is NavigationViewItem item)
            {
                string? tag = item.Tag?.ToString();
                if (!string.IsNullOrEmpty(tag))
                {
                    NavigateTo(tag);
                }
            }
        }

        private void NavigateTo(string pageTag)
        {
            Type? pageType = pageTag switch
            {
                "home" => typeof(Pages.HomePage),
                "dashboard" => typeof(Pages.DashboardPage),
                "controls" => typeof(Pages.ControlsPage),
                "datagrid" => typeof(Pages.DataGridPage),
                "charts" => typeof(Pages.ChartsPage),
                "forms" => typeof(Pages.FormsPage),
                "settings" => typeof(Pages.SettingsPage),
                "about" => typeof(Pages.AboutPage),
                _ => null
            };

            if (pageType != null)
            {
                ContentFrame.Navigate(Activator.CreateInstance(pageType));
            }
        }
    }
}
