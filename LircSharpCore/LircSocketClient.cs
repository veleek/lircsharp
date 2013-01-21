using System;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Ben.LircSharp
{
    public class LircSocketClient : LircClient
    {
        private Socket socket;
        private SocketAsyncEventArgs readArgs;
        private SocketAsyncEventArgs writeArgs;

        private Queue<string> writeCommandQueue = new Queue<string>();
        private ManualResetEvent connectedEvent = new ManualResetEvent(false);
        private ManualResetEvent disposeEvent = new ManualResetEvent(false);
        private AutoResetEvent writeEvent = new AutoResetEvent(false);
        private Timer reconnectTimer;
        private object reconnectLock = new object();

        protected byte[] readBuffer = new byte[1024];
        protected byte[] writeBuffer = new byte[1024];

        public static List<Socket> ClientSockets = LircClient.Clients.OfType<LircSocketClient>().Select(c => c.socket).ToList();

        public LircSocketClient() : base() 
        {
            SetupReadWriteThreads();
        }

        public LircSocketClient(string host, int port) : base(host, port) 
        {
            SetupReadWriteThreads();
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Kill the reader/writer threads
                    disposeEvent.Set();
                }
            }

            base.Dispose(disposing);
        }

        private void SetupReadWriteThreads()
        {
            ThreadPool.QueueUserWorkItem(DoRead, null);
            ThreadPool.QueueUserWorkItem(DoWrite, null);
        }

        protected override void ConnectInternal()
        {
            if (socket != null)
            {
                throw new InvalidOperationException("You must call disconnect before calling connect.");
            }

            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            var connectArgs = new SocketAsyncEventArgs();
            connectArgs.RemoteEndPoint = new DnsEndPoint(this.Host, this.Port);
            connectArgs.Completed += connect_Completed;

            if (!socket.ConnectAsync(connectArgs))
            {
                connect_Completed(this, connectArgs);
            }
        }

        protected override void DisconnectInternal()
        {
            if (socket != null)
            {
                if (socket.Connected)
                {
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                }

                connectedEvent.Reset();
                socket.Dispose();
                socket = null;
            }
        }

        protected override void SendCommandInternal(string command)
        {
            // Queue up the command
            writeCommandQueue.Enqueue(command);
            // And signal the worker to start pumping
            writeEvent.Set();
        }

        private void connect_Completed(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                OnError("Unable to connect: " + e.SocketError);
                return;
            }

            readArgs = new SocketAsyncEventArgs();
            readArgs.SetBuffer(readBuffer, 0, readBuffer.Length);
            readArgs.Completed += socketRead_Completed;

            writeArgs = new SocketAsyncEventArgs();
            writeArgs.SetBuffer(writeBuffer, 0, writeBuffer.Length);
            writeArgs.Completed += socketWrite_Completed;

            connectedEvent.Set();

            OnConnected();
        }

        private void AttemptReconnectIfRequired()
        {
            Socket s = this.socket;

            if (s == null)
            {
                // We've disconnected, no automatic reconnect should be attempted
                return;
            }

            if (!s.Connected)
            {
                // If we're not connected then let everyone know
                connectedEvent.Reset();

                lock (reconnectLock)
                {
                    // Check to see if we should queue up a reconnect timer
                    if (reconnectTimer == null)
                    {
                        OnMessage("Setting up for reconnect...");
                        // Sleep 30 seconds and try again
                        reconnectTimer = new Timer(state =>
                        {

                            try
                            {
                                // If the socket is null, a disconnect was performed
                                // and we should not just blindly reconnect
                                if (socket != null && !socket.Connected)
                                {
                                    OnMessage("Reconnecting...");
                                    Reconnect();
                                }
                            }
                            finally
                            {
                                reconnectTimer = null;
                            }
                        }, null, TimeSpan.FromSeconds(30), TimeSpan.FromMilliseconds(-1));
                    }
                }
            }
        }

        private void DoRead(object state)
        {
            // If we're not connected, wait until we are
            int signaledHandle = WaitHandle.WaitAny(new WaitHandle[] { connectedEvent, disposeEvent });

            if (signaledHandle == 1)
            {
                // The object has been disposed, stop reading
                return;
            }

            // If the socket is null that means we've disconnected.
            if (socket == null)
            {
                // Stop performing reads, they will be restarted when a new connection is made
                //return;
            }

            try
            {
                if (socket == null || !socket.Connected)
                {
                    // We got disconnected somehow, just queue up a read
                    ThreadPool.QueueUserWorkItem(DoRead, state);
                    return;
                }

                if (!socket.ReceiveAsync(readArgs))
                {
                    // This means the read finished syncronously so parse the data
                    socketRead_Completed(this, readArgs);
                }
            }
            catch (Exception e)
            {
                OnError("Exception reading.", e);
                // There was an error starting up the read, so let's queue up another
                ThreadPool.QueueUserWorkItem(DoRead, state);
            }
        }

        private void socketRead_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.SocketError)
            {
                // This occurs when reconnecting and when tombstoning
                case SocketError.OperationAborted:
                // These can occur when the server is down or connecting somewhere non-existant
                case SocketError.ConnectionAborted:
                case SocketError.ConnectionRefused:
                case SocketError.ConnectionReset:
                case SocketError.NotConnected:
                    AttemptReconnectIfRequired();
                    break;
                case SocketError.Shutdown:
                    // Nothing to do, we're disconnecting, maybe we just shouldn't attempt to reconnect unless asked.
                    break;
                case SocketError.TryAgain:
                    // Okay, nothing to do here either
                    break;
                case SocketError.Success:
                    // Success, parse the message
                    ParseData(e.Buffer, e.Offset, e.BytesTransferred);
                    break;
                default:
                    // log the error
                    OnError(string.Format("Error while reading: {0}, Operation: {1}", e.SocketError, e.LastOperation));
                    break;
            }

            // Then kick of another read
            ThreadPool.QueueUserWorkItem(DoRead, e.UserToken);
        }

        private void DoWrite(object state)
        {
            bool commandProcessed = false;

            do
            {
                // Make sure we're in a state that we can do something
                // If we're not connected, wait until we are
                int signaledHandle = WaitHandle.WaitAny(new WaitHandle[] { connectedEvent, disposeEvent });

                if (signaledHandle == 1)
                {
                    // The object has been disposed, stop writing
                    return;
                }          

                if (writeCommandQueue.Count <= 0)
                {
                    // Either wait until we get a write or we're being disposed
                    signaledHandle = WaitHandle.WaitAny(new WaitHandle[] { writeEvent, disposeEvent });

                    if (signaledHandle == 1)
                    {
                        // The object has been disposed, stop reading
                        return;
                    }          
                }

                string command = null;
                try
                {
                    command = writeCommandQueue.Dequeue();
                }
                catch(InvalidOperationException)
                {
                    // Ignore, it just means the queue is empty
                }

                // If for some reason we didn't get a command, just start waiting again
                if (command == null)
                {
                    continue;
                }

                OnMessage("Sending command " + command.Trim());

                var bytesToWrite = Encoding.UTF8.GetBytes(command, 0, command.Length, writeBuffer, 0);
                writeArgs.SetBuffer(0, bytesToWrite);
                try
                {
                    socket.SendAsync(writeArgs);
                    commandProcessed = true;
                }
                catch (Exception e)
                {
                    OnError("Error sending command: " + command + "\r\n" + e.Message, e);
                }
            }
            while (!commandProcessed);
        }

        private void socketWrite_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.SocketError)
            {
                // This occurs when reconnecting and when tombstoning
                case SocketError.OperationAborted:
                // These can occur when the server is down or connecting somewhere non-existant
                case SocketError.ConnectionAborted:
                case SocketError.ConnectionRefused:
                case SocketError.ConnectionReset:
                case SocketError.NotConnected:
                    AttemptReconnectIfRequired();
                    break;
                case SocketError.Shutdown:
                    // Nothing to do, we're disconnecting, maybe we just shouldn't attempt to reconnect unless asked.
                    break;
                case SocketError.TryAgain:
                    // Okay
                    try
                    {
                        socket.SendAsync(writeArgs);
                        // If this succeeded, then a write is pending 
                        // and we should not queue up another one right away
                        return;
                    }
                    catch (Exception ex)
                    {
                        OnError("Error trying again.", ex);
                    }
                    break;
                case SocketError.Success:
                    break;
                default:
                    // Display the error
                    OnError(string.Format("Error writing to socket: {0}, Operation: {1}", e.SocketError, e.LastOperation));
                    break;
            }

            // Kick off another write
            ThreadPool.QueueUserWorkItem(DoWrite, e.UserToken);
        }
    }
}
