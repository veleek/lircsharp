using System;
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
        private ManualResetEvent writeEvent = new ManualResetEvent(true);

        protected byte[] readBuffer = new byte[1024];
        protected byte[] writeBuffer = new byte[1024];

        public LircSocketClient(string host, int port) : base(host, port) { }

        protected override void ConnectInternal(string host, int port)
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            var connectArgs = new SocketAsyncEventArgs();
            connectArgs.RemoteEndPoint = new DnsEndPoint(host, port);
            connectArgs.Completed += connect_Completed;

            if (!socket.ConnectAsync(connectArgs))
            {
                connect_Completed(this, connectArgs);
            }
        }

        protected override void SendCommandInternal(string command)
        {
            // Queue up the command
            writeCommandQueue.Enqueue(command);
            // And signal the worker to start pumping
            writeEvent.Set();
        }

        void connect_Completed(object sender, SocketAsyncEventArgs e)
        {
            readArgs = new SocketAsyncEventArgs();
            readArgs.SetBuffer(readBuffer, 0, readBuffer.Length);
            readArgs.Completed += socketRead_Completed;

            writeArgs = new SocketAsyncEventArgs();
            writeArgs.SetBuffer(writeBuffer, 0, writeBuffer.Length);
            writeArgs.Completed += socketWrite_Completed;

            ThreadPool.QueueUserWorkItem(DoRead);
            ThreadPool.QueueUserWorkItem(DoWrite);
        }

        private void DoRead(object state)
        {
            if (!socket.ReceiveAsync(readArgs))
            {
                socketRead_Completed(this, readArgs);
            }
        }

        private void socketRead_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.SocketError)
            {
                case SocketError.NotConnected:
                    Connect();
                    break;
                case SocketError.OperationAborted:
                    // This occurs when the app has been tombstoned
                    Connect();
                    break;
                case SocketError.Shutdown:
                    // Stop writing commands
                    break;
                case SocketError.TryAgain:
                    // Okay
                    socket.ReceiveAsync(readArgs);
                    break;
                case SocketError.Success:
                    // Success, parse the message
                    ParseData(e.Buffer, e.Offset, e.BytesTransferred);
                    // Then kick of another read
                    DoRead(e.UserToken);
                    break;
                default:
                    // log the error
                    // TODO: Add logging
                    //logger.Error("Unexpected reading error: " + e.SocketError);
                    // And kick of another read
                    DoRead(e.UserToken);
                    break;
            }
        }

        private void DoWrite(object state)
        {
            bool commandProcessed = false;

            do
            {
                if (writeCommandQueue.Count <= 0)
                {
                    do
                    {
                        writeEvent.WaitOne();
                    }
                    while (writeCommandQueue.Count <= 0);
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

                if (command == null)
                {
                    continue;
                }

                var bytesToWrite = Encoding.UTF8.GetBytes(command, 0, command.Length, writeBuffer, 0);
                writeArgs.SetBuffer(0, bytesToWrite);
                socket.SendAsync(writeArgs);
                commandProcessed = true;
            }
            while (!commandProcessed);
            
        }

        private void socketWrite_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.SocketError)
            {
                case SocketError.NotConnected:
                    Connect();
                    break;
                case SocketError.OperationAborted:
                    // This occurs when the app has been tombstoned
                    Connect();
                    break;
                case SocketError.Shutdown:
                    // Stop writing commands
                    break;
                case SocketError.TryAgain:
                    // Okay
                    socket.SendAsync(writeArgs);
                    break;
                case SocketError.Success:
                    // Success, kick of another write
                    DoWrite(null);
                    break;
                default:
                    // Display the error
                    // TODO: Add logging
                    //logger.Error("Unexpected writing error: " + e.SocketError);
                    // And kick of another write
                    DoWrite(null);
                    break;
            }
        }
    }
}
