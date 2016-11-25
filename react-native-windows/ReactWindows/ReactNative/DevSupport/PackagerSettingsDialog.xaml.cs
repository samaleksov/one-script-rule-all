using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Content Dialog item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace ReactNative.DevSupport
{
    sealed partial class PackagerSettingsDialog : ContentDialog
    {
        private static readonly Thickness s_buttonMargin = new Thickness(2);

        private DevInternalSettings devInternalSettings;

        public PackagerSettingsDialog(DevInternalSettings devInternalSettings)
        {
            this.InitializeComponent();
            this.devInternalSettings = devInternalSettings;
            string existing = devInternalSettings.DebugServerHost;
            this.host_preference.Text = existing != null ? existing.Split(':')[0] : "";
            this.port_preference.Text = existing != null ? existing.Split(':')[1] : "";
        }
        
        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            devInternalSettings.DebugServerHost = this.host_preference.Text + ':' + this.port_preference.Text;
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {

        }
    }
}
