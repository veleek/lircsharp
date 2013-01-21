using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using Microsoft.Phone.Controls;
using Microsoft.Xna.Framework;
using Ben.LircSharp;
using NLog;

namespace Ben.LircSharp.Phone
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();

            // Set the data context of the listbox control to the sample data
            DataContext = App.ViewModel;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            App.Tracker.TrackRelatime("MainPage");
        }

        private void CommandListPicker_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            App.ViewModel.Client.SendCommand((string)RemoteListPicker.SelectedItem, (sender as FrameworkElement).DataContext as String);
        }

        private void Settings_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/SettingsPage.xaml", UriKind.Relative));
        }

        private void Refresh_Click(object sender, EventArgs e)
        {
            App.ViewModel.Client.SendCommand("LIST");
        }

        private void ClearLog_Click(object sender, EventArgs e)
        {
            App.ViewModel.LogLines.Clear();
        }

        private void ReloadLayout_Click(object sender, EventArgs e)
        {
            App.ViewModel.ReloadRemoteLayout();
        }
    }
}