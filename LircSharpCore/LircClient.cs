using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
//using NLog;

namespace Ben.LircSharp
{
    public abstract class LircClient : IDisposable
    {
        //protected static Logger logger = LogManager.GetLogger("LircClient");

        private LircCommandParser parser = new LircCommandParser();
        private object connectLock = new object();
        
        protected bool disposed = false;

        public event EventHandler Connected;
        public event EventHandler<LircMessageEventArgs> Message;
        public event EventHandler<LircCommandEventArgs> CommandCompleted;
        public event EventHandler<LircErrorEventArgs> Error;

        public Dictionary<string, List<string>> RemoteCommands { get; set; }

        public static List<LircClient> Clients = new List<LircClient>();

        public LircClient()
        {
            parser = new LircCommandParser();
            parser.CommandParsed += new EventHandler<LircCommandEventArgs>(parser_CommandParsed);

            Clients.Add(this);
        }

        public LircClient(string host, int port) : this()
        {
            this.Connect(host, port);
        }

        public string Host { get; protected set; }
        public int Port { get; protected set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            this.disposed = true;
        }

        public void Connect()
        {
            if (string.IsNullOrWhiteSpace(this.Host))
            {
                throw new InvalidOperationException("You must set the host before attempting to connect");
            }

            if (this.Port == 0)
            {
                throw new InvalidOperationException("You must set the port before attempting to connect");
            }

            this.Connect(this.Host, this.Port);
        }

        public void Connect(string host, int port)
        {
            // Check if someone is already connecting
            if (Monitor.TryEnter(connectLock))
            {
                try
                {

                    this.Host = host;
                    this.Port = port;

                    ConnectInternal();
                }
                finally
                {
                    Monitor.Exit(connectLock);
                }
            }
        }

        public void Reconnect()
        {
            Disconnect();

            Connect();
        }

        public void Disconnect()
        {
            this.DisconnectInternal();
        }

        public void SendCommand(string remote, string command)
        {
            this.SendCommand(string.Format("SEND_ONCE {0} {1}\n", remote, command));
        }

        public void SendCommand(string command)
        {
            if (!command.EndsWith("\n"))
            {
                command += '\n';
            }

            SendCommandInternal(command);
        }

        public List<string> GetCommands(string remote)
        {
            if (remote == null)
            {
                return null;
            }

            if (!RemoteCommands.ContainsKey(remote))
            {
                throw new ArgumentException("You must specify a valid remote name");
            }

            var commands = RemoteCommands[remote];
            //if (commands.Count == 0)
            {
            //    SendCommand("LIST " + remote);
            }

            return commands ?? new List<string>();
        }

        protected abstract void SendCommandInternal(string command);        

        protected abstract void ConnectInternal();

        protected abstract void DisconnectInternal();

        protected void ParseData(byte[] buffer, int index, int count)
        {
            try
            {
                //OnError(System.Text.Encoding.UTF8.GetString(buffer, index, count));
                parser.Parse(buffer, index, count);
            }
            catch (LircParsingException e)
            {
                OnError("Exception while parsing data", e);
            }
        }

        private void parser_CommandParsed(object sender, LircCommandEventArgs e)
        {
            switch (e.Command.Command)
            {
                case "ListRemotes":
                    var listRemotes = e.Command as LircListRemotesCommand;
                    RemoteCommands = new Dictionary<string, List<string>>();
                    foreach (var remote in listRemotes.Remotes)
                    {
                        RemoteCommands[remote] = null;
                        SendCommand("LIST " + remote);
                    }
                    break;
                case "ListRemote":
                    var listRemote = e.Command as LircListRemoteCommand;
                    RemoteCommands[listRemote.Remote] = listRemote.Data;
                    break;
                case "Version":
                default:
                    break;
            }

            OnCommandCompleted(e.Command);
        }

        protected void OnConnected()
        {
            var connectedHandler = Connected;
            if (connectedHandler != null)
            {
                connectedHandler(this, EventArgs.Empty);
            }
        }

        protected void OnError(string message)
        {
            OnError(message, null);
        }

        protected void OnError(string message, Exception exception)
        {
            var errorHandler = Error;
            if (errorHandler != null)
            {
                errorHandler(this, new LircErrorEventArgs(message, exception));
            }
        }

        protected void OnCommandCompleted(LircCommand command)
        {
            var commandCompletedHandler = CommandCompleted;
            if (commandCompletedHandler != null)
            {
                commandCompletedHandler(this, new LircCommandEventArgs(command));
            }
        }

        protected void OnMessage(string message)
        {
            Message(this, new LircMessageEventArgs(message));
        }
    }
}
