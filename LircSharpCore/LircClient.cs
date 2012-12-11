using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
//using NLog;

namespace Ben.LircSharp
{
    public abstract class LircClient
    {
        //protected static Logger logger = LogManager.GetLogger("LircClient");

        private LircCommandParser parser = new LircCommandParser();
        public Dictionary<string, ObservableCollection<string>> RemoteCommands { get; set; }
        private string host;
        private int port;

        public event EventHandler<LircCommandEventArgs> CommandCompleted;

        public LircClient()
        {
            parser = new LircCommandParser();
            parser.CommandParsed += new EventHandler<LircCommandEventArgs>(parser_CommandParsed);
        }

        public LircClient(string host, int port) : this()
        {
            this.Connect(host, port);
        }

        public void Connect()
        {
            if (string.IsNullOrWhiteSpace(this.host))
            {
                throw new InvalidOperationException("You must set the host before attempting to connect");
            }

            if (port == 0)
            {
                throw new InvalidOperationException("You must set the port before attempting to connect");
            }

            this.Connect(this.host, this.port);
        }

        public void Connect(string host, int port)
        {
            this.host = host;
            this.port = port;

            ConnectInternal(host, port);
        }

        public void SendCommand(string remote, string command)
        {
            SendCommand(string.Format("SEND_ONCE {0} {1}\n", remote, command));
        }

        public void SendCommand(string command)
        {
            if (!command.EndsWith("\n"))
            {
                command += '\n';
            }

            // TODO: Add logging
            //logger.Info("SendCommand: " + command.Trim());
            SendCommandInternal(command);
        }

        public ObservableCollection<string> GetCommands(string remote)
        {
            if (!RemoteCommands.ContainsKey(remote))
            {
                throw new ArgumentException("You must specify a valid remote name");
            }

            var commands = RemoteCommands[remote];
            if (commands.Count == 0)
            {
                SendCommand("LIST " + remote);
            }

            return commands;
        }

        protected abstract void SendCommandInternal(string command);        

        protected abstract void ConnectInternal(string host, int port);

        protected void ParseData(byte[] buffer, int index, int count)
        {
            try
            {
                parser.Parse(buffer, index, count);
            }
            catch (LircParsingException e)
            {
                // TODO: Add logging
                //logger.WarnException("Exception while parsing data", e);
            }
        }

        private void parser_CommandParsed(object sender, LircCommandEventArgs e)
        {
            if (e.Command.Command.StartsWith("LIST"))
            {
                if (e.Command.Command.Length == 4)
                {
                    RemoteCommands = new Dictionary<string, ObservableCollection<string>>();
                    foreach (var remote in e.Command.Data)
                    {
                        RemoteCommands.Add(remote, new ObservableCollection<string>());
                        SendCommand("LIST " + remote);
                    }
                }
                else
                {
                    var index = e.Command.Command.IndexOf(' ');
                    var remote = e.Command.Command.Substring(index + 1).Trim();

                    //Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        foreach (var command in e.Command.Data)
                        {
                            RemoteCommands[remote].Add(command);
                        }
                    }
                    //);
                }
            }
            else
            {
                // TODO: Add logging
                //logger.Trace(e.Command.Command + " - " + e.Command.Succeeded);
            }

            CommandCompleted(this, e);
        }
    }
}
