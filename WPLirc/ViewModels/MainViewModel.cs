using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using NLog;
using System.Text;
using System.Linq;
using System.Net;
using System.IO;
using System.Windows.Markup;
using System.Windows.Controls;

namespace Ben.LircSharp.Phone
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private static Logger logger = LogManager.GetLogger("ViewModel");
        private string rawLog = string.Empty;

        public MainViewModel()
        {
            this.LogLines = new ObservableCollection<string>();

            this.Remotes = new ObservableCollection<string>();

            this.Connect();

            this.LoadRemoteLayout(Settings.RemoteLayoutXaml);
        }

        public Dispatcher Dispatcher
        {
            get { return Deployment.Current.Dispatcher; }
        }

        /// <summary>
        /// An instance of the LircClient to communicate with the server
        /// </summary>
        public LircClient Client { get; private set; }

        public bool IsConnected
        {
            get;
            private set;
        }

        public ObservableCollection<string> LogLines { get; private set; }

        public string RawLog
        {
            get
            {
                return rawLog;
            }
            set
            {
                if (value != rawLog)
                {
                    rawLog = value;
                    NotifyPropertyChanged("RawLog");
                }
            }
        }

        public ObservableCollection<string> Remotes { get; private set; }

        private string selectedRemote = null;
        /// <summary>
        /// The currently selected remote
        /// </summary>
        /// <returns></returns>
        public string SelectedRemote
        {
            get
            {
                return selectedRemote;
            }
            set
            {
                if (value != selectedRemote)
                {
                    selectedRemote = value;
                    NotifyPropertyChanged("SelectedRemote");

                    this.SelectedRemoteCommands = Client != null ? Client.GetCommands(selectedRemote) : null;
                }
            }
        }

        private List<string> selectedRemoteCommands = null;
        /// <summary>
        /// 
        /// </summary>
        public List<string> SelectedRemoteCommands
        {
            get
            {
                return selectedRemoteCommands;
            }
            set
            {
                if (value != selectedRemoteCommands)
                {
                    selectedRemoteCommands = value;
                    NotifyPropertyChanged("SelectedRemoteCommands");
                }
            }
        }

        private string version = null;
        public string Version
        {
            get
            {
                return version;
            }
            set
            {
                if (value != version)
                {
                    version = value;
                    NotifyPropertyChanged("Version");
                }
            }
        }

        private string status = null;
        public string Status
        {
            get
            {
                return status;
            }
            set
            {
                if (value != status)
                {
                    status = value;
                    NotifyPropertyChanged("Status");
                }
            }
        }

        private Panel remotePanel = null;
        public Panel RemotePanel
        {
            get { return remotePanel; }
            set
            {
                if (value != remotePanel)
                {
                    remotePanel = value;
                    NotifyPropertyChanged("RemotePanel");
                }
            }
        }

        /// <summary>
        /// Creates and adds a few ItemViewModel objects into the Items collection.
        /// </summary>
        public void Connect()
        {
            if (this.Client != null)
            {
                return;
            }

            this.Client = new LircSocketClient();
            this.Client.Connected += Client_Connected;
            this.Client.CommandCompleted += Client_CommandCompleted;
            this.Client.Error += Client_Error;
            this.Client.Message += Client_Message;

            string message = string.Format("Connecting to {0}:{1}", Settings.Host, Settings.Port);
            logger.Info(message);
            this.Status = message;
            this.Client.Connect(Settings.Host, Settings.Port);
        }

        public void Disconnect()
        {
            if (Client != null)
            {
                string message = string.Format("Disconnecting from {0}:{1}", Settings.Host, Settings.Port);
                logger.Info(message);
                this.Status = message;
                this.Client.Disconnect();
                this.Client = null;

                this.Remotes.Clear();
                this.Version = null;

                this.Status = "Disconnected";
            }
        }

        public void ReloadRemoteLayout()
        {
            if (Settings.RemoteLayoutUrl == null)
            {
                this.RemotePanel = null;
                return;
            }

            HttpWebRequest req = WebRequest.CreateHttp(Settings.RemoteLayoutUrl);
            req.Headers[HttpRequestHeader.IfModifiedSince] = DateTime.UtcNow.ToString("R");

            req.BeginGetResponse(result =>
            {
                try
                {
                    HttpWebResponse resp = req.EndGetResponse(result) as HttpWebResponse;

                    using (StreamReader reader = new StreamReader(resp.GetResponseStream()))
                    {
                        string remoteXaml = reader.ReadToEnd();

                        Dispatcher.BeginInvoke(() =>
                        {
                            if (LoadRemoteLayout(remoteXaml))
                            {
                                // If it loaded successfully, save the XAML for next time
                                Settings.RemoteLayoutXaml = remoteXaml;
                            }
                        });
                    }
                }
                catch(Exception e)
                {
                    logger.ErrorException("Unable to retrieve remote layout", e);
                }
            }, 
            null);
        }

        public bool LoadRemoteLayout(string remoteXaml)
        {
            if (string.IsNullOrWhiteSpace(remoteXaml))
            {
                return false;
            }

            try
            {
                using (var xamlReader = new StringReader(remoteXaml))
                {
                    var remotePanel = XamlReader.Load(xamlReader.ReadToEnd()) as Panel;

                    if (remotePanel != null)
                    {
                        foreach (Button button in remotePanel.Children.OfType<Button>())
                        {
                            button.Click += new RoutedEventHandler(remoteButton_Click);
                        }

                        this.RemotePanel = remotePanel;
                    }
                }
            }
            catch (Exception e)
            {
                logger.ErrorException("Error parsing remote layout XAML", e);
                return false;
            }

            return true;
        }

        void remoteButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            String commandList = button.Tag as String;
            String[] commands = commandList.Split(',', ';');

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (s, ea) =>
            {
                for (int i = 0; i < commands.Length; i++)
                {
                    string command = commands[i];
                    if (command.StartsWith("sleep", StringComparison.OrdinalIgnoreCase))
                    {
                        int sleepDuration = Int32.Parse(command.Split(' ')[1]);
                        System.Threading.Thread.Sleep(sleepDuration);
                    }
                    else
                    {
                        App.ViewModel.Client.SendCommand("SEND_ONCE " + command);
                        if (i < commands.Length - 1)
                        {
                            System.Threading.Thread.Sleep(10);
                        }
                    }
                }
            };
            worker.RunWorkerAsync();
        }

        private void Client_Connected(object sender, EventArgs e)
        {
            logger.Info("Connected!");

            this.Client.SendCommand("LIST");
            this.Client.SendCommand("VERSION");
        }

        private void Client_CommandCompleted(object sender, LircCommandEventArgs e)
        {
            logger.Debug(e.Command.Command + " Completed");

            switch (e.Command.Command)
            {
                case "ListRemotes":
                    this.SetConnectedStatusMessage();
                    Dispatcher.BeginInvoke(() =>
                    {
                        lock (Remotes)
                        {
                            Remotes.Clear();
                            foreach (var remote in Client.RemoteCommands.Keys)
                            {
                                Remotes.Add(remote);
                            }

                            if (Remotes.Count > 0)
                            {
                                SelectedRemote = Remotes[0];
                            }
                        }
                    });
                    break;
                case "ListRemote":
                    var listRemote = e.Command as LircListRemoteCommand;
                    Dispatcher.BeginInvoke(() => 
                    {
                        // Refresh the list of remote commands if the response 
                        // was from the currently selected remote
                        if (listRemote.Remote == this.SelectedRemote)
                        {
                            SelectedRemoteCommands = Client.GetCommands(listRemote.Remote);
                        }
                    });
                    break;
                case "Version":
                    var versionCommand = e.Command as LircVersionCommand;
                    this.Version = versionCommand.Version;
                    this.SetConnectedStatusMessage();
                    break;
                default:
                    break;
            }
        }

        private void Client_Error(object sender, LircErrorEventArgs e)
        {
            logger.ErrorException(e.Message, e.Exception);
        }

        private void Client_Message(object sender, LircMessageEventArgs e)
        {
            logger.Info(e.Message);
        }

        private void SetConnectedStatusMessage()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Connected ({0}:{1})", this.Client.Host, this.Client.Port);

            /*
            if (this.Version != null)
            {
                sb.AppendLine();
                sb.Append(this.Version);
            }
             */

            Dispatcher.BeginInvoke(() =>
            {
                this.Status = sb.ToString();
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}