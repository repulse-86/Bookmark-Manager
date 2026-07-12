using Bookmark_Manager.Common;
using System;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Bookmark_Manager
{
    public sealed partial class AboutPage : Page
    {
        private readonly NavigationHelper navigationHelper;

        public AboutPage()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            SetVersion();
        }

        private void SetVersion()
        {
            var package = Windows.ApplicationModel.Package.Current;
            var version = package.Id.Version;
            VersionTextBlock.Text = $"Version {version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }

        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        private async void SupportButton_Click(object sender, RoutedEventArgs e)
        {
            var uri = new Uri("https://ko-fi.com/nextcall");
            await Launcher.LaunchUriAsync(uri);
        }
    }
}
