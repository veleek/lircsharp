using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.IO.IsolatedStorage;
using Microsoft.Phone.Tasks;

namespace Ben.LircSharp.Phone
{
    public partial class SettingsPage : PhoneApplicationPage
    {
        public SettingsPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            HostTextBox.Text = Settings.Host;
            PortTextBox.Text = Settings.Port.ToString();

            RemoteLayoutUrlTextBox.Text = Settings.RemoteLayoutUrl != null ? Settings.RemoteLayoutUrl.ToString() : string.Empty;
        }

        private bool ValidateTextBox(TextBox textBox, out string value)
        {
            value = textBox.Text;

            if (string.IsNullOrWhiteSpace(value))
            {
                textBox.Focus();
                textBox.SelectAll();
                return false;
            }

            return true;
        }

        private bool ValidateHost(out string host)
        {
            return ValidateTextBox(HostTextBox, out host);
        }

        private bool ValidatePort(out int port)
        {
            if (!int.TryParse(PortTextBox.Text, out port))
            {
                PortTextBox.Focus();
                PortTextBox.SelectAll();
                return false;
            }

            return true;
        }

        private bool ValidateRemoteLayoutUrl(out Uri remoteLayoutUrl)
        {
            string text = RemoteLayoutUrlTextBox.Text;
            remoteLayoutUrl = null;

            if (string.IsNullOrWhiteSpace(text))
            {
                remoteLayoutUrl = null;
            }
            else if (!Uri.TryCreate(text, UriKind.Absolute, out remoteLayoutUrl) && !Uri.TryCreate("http://" + text, UriKind.Absolute, out remoteLayoutUrl))
            {
                RemoteLayoutUrlTextBox.Focus();
                RemoteLayoutUrlTextBox.SelectAll();
                return false;
            }

            return true;
        }

        private void Ok_Click(object sender, EventArgs e)
        {
            string host;
            int port;
            Uri remoteLayoutUrl;

            if (!ValidateHost(out host) || !ValidatePort(out port) || !ValidateRemoteLayoutUrl(out remoteLayoutUrl))
            {
                return;
            }

            if (host != Settings.Host || port != Settings.Port)
            {
                App.ViewModel.Disconnect();

                Settings.Host = host;
                Settings.Port = port;
                App.ViewModel.Connect();
            }

            if (remoteLayoutUrl != Settings.RemoteLayoutUrl)
            {
                Settings.RemoteLayoutUrl = remoteLayoutUrl;
                App.ViewModel.ReloadRemoteLayout();
            }

            // Set the host and port
            NavigationService.GoBack();
        }

        private void Cancel_Click(object sender, EventArgs e)
        {
            NavigationService.GoBack();
        }

        private void PortTextBox_ActionIconTapped(object sender, EventArgs e)
        {
            PortTextBox.Text = "8765";
        }

        private void HostTextBox_ActionIconTapped(object sender, EventArgs e)
        {
            HostTextBox.Text = string.Empty;
        }

        private void HostTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            string host;
            ValidateHost(out host);
        }

        private void HostTextBox_GotFocus(object sender, RoutedEventArgs e)
        {

        }

        private void PortTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            int port;
            ValidatePort(out port);
        }

        private void PortTextBox_GotFocus(object sender, RoutedEventArgs e)
        {

        }

        private void RemoteLayoutUrlTextBox_ActionIconTapped(object sender, EventArgs e)
        {
            RemoteLayoutUrlTextBox.Text = string.Empty;
        }

        private void RemoteLayoutUrlTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            Uri uri;
            ValidateRemoteLayoutUrl(out uri);
        }

        private void RemoteLayoutUrlTextBox_GotFocus(object sender, RoutedEventArgs e)
        {

        }

        private void WebsiteButton_Click(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;
            String url = b.Tag as String;

            WebBrowserTask browserTask = new WebBrowserTask();
            browserTask.Uri = new Uri(url);
            browserTask.Show();
        }
    }
}