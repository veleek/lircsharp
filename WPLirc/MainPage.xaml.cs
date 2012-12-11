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

namespace Ben.LircSharp.Phone
{
    public partial class MainPage : PhoneApplicationPage
    {
        private LircClient client;
        public static ObservableCollection<string> LogLines = new ObservableCollection<string>();

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            // Set the data context of the listbox control to the sample data
            DataContext = App.ViewModel;
            this.Loaded += new RoutedEventHandler(MainPage_Loaded);

            client = new LircSocketClient("192.168.1.100", 8765);
            client.CommandCompleted += new EventHandler<LircCommandEventArgs>(client_CommandCompleted);

            LogMessages.ItemsSource = MainPage.LogLines;

            var xamlReader = new StreamReader(TitleContainer.OpenStream("Remote.xaml"));
            var remotePanel = XamlReader.Load(xamlReader.ReadToEnd()) as Panel;

            if (remotePanel != null)
            {
                foreach (Button button in remotePanel.Children.OfType<Button>())
                {
                    button.Click += new RoutedEventHandler(remoteButton_Click);
                }
                RemotePivot.Content = remotePanel;
            }
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            App.Tracker.TrackRelatime("MainPage");
        }

        void remoteButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            String commandList = button.Tag as String;
            String[] commands = commandList.Split(',', ';');

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (s, ea) =>
            {
                foreach (var command in commands)
                {
                    if (command.StartsWith("sleep", StringComparison.OrdinalIgnoreCase))
                    {
                        int sleepDuration = Int32.Parse(command.Split(' ')[1]);
                        System.Threading.Thread.Sleep(sleepDuration);
                    }
                    else
                    {
                        client.SendCommand("SEND_ONCE " + command);
                        System.Threading.Thread.Sleep(50);
                    }
                }
            };
            worker.RunWorkerAsync();
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            throw new NotImplementedException();
        }

        void client_CommandCompleted(object sender, LircCommandEventArgs e)
        {
            if (e.Command.Command == "LIST")
            {
                Dispatcher.BeginInvoke(() =>
                {
                    var remotes = client.RemoteCommands.Keys.ToArray();
                    RemoteListPicker.ItemsSource = remotes;
                });
            }
        }

        // Load data for the ViewModel Items
        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (!App.ViewModel.IsDataLoaded)
            {
                App.ViewModel.LoadData();
            }
        }

        private void VersionButton_Click(object sender, RoutedEventArgs e)
        {
            client.SendCommand("VERSION");
        }

        private void ListButton_Click(object sender, RoutedEventArgs e)
        {
            client.SendCommand("LIST");
        }

        private void RemoteListPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                string remote = e.AddedItems[0] as string;
                var commands = client.GetCommands(remote);
                if (commands != null)
                {
                    CommandListPicker.ItemsSource = commands; //.GroupBy(s => s[0], (firstChar, commandSet) => new Grouping<char, string>(firstChar, commandSet));
                }
            }
        }

        private void CommandListPicker_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            client.SendCommand((string)RemoteListPicker.SelectedItem, (sender as FrameworkElement).DataContext as String);
        }

        private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void Settings_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Settings.xaml", UriKind.Relative));
        }
    }
}