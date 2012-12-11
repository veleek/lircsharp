using System;
using System.IO;
using System.Text;
using System.Windows;
using SocketEx;

namespace Ben.Lirc.Phone
{
    public class LircTcpClient
    {
        private LircCommandParser parser = new LircCommandParser();

        private TcpClient client;
        private Stream stream;

        private byte[] readBuffer = new byte[1024];
        private byte[] writeBuffer = new byte[1024];

        public LircTcpClient(string host, int port)
        {
            parser = new LircCommandParser();
            parser.CommandParsed += new EventHandler<LircCommandEventArgs>(parser_CommandParsed);

            client = new TcpClient();
            client.BeginConnect(host, port, ClientConnected, null);
        }

        public bool Connected { get; set; }

        public void SendCommand(string command)
        {
            if (!command.EndsWith("\n"))
            {
                command += '\n';
            }

            int bytesToWrite = Encoding.UTF8.GetBytes(command, 0, command.Length, writeBuffer, 0);
            stream.Write(writeBuffer, 0, bytesToWrite);
        }


        private void ClientConnected(IAsyncResult result)
        {
            try
            {
                client.EndConnect(result);
            }
            catch
            {
                return;
            }

            stream = client.GetStream();
            stream.BeginRead(readBuffer, 0, readBuffer.Length, ReadCompleted, result.AsyncState);
        }


        private void ReadCompleted(IAsyncResult result)
        {
            try
            {
                int bytesRead = stream.EndRead(result);
                parser.Parse(readBuffer, 0, bytesRead);

                stream.BeginRead(readBuffer, 0, readBuffer.Length, ReadCompleted, result.AsyncState);
            }
            catch
            {
                MessageBox.Show("Client read failed...");
            }
        }

        private void parser_CommandParsed(object sender, LircCommandEventArgs e)
        {
            if (e.Command.Command == "LIST")
            {
                foreach (var remote in e.Command.Data)
                {
                    MessageBox.Show(remote);
                }
            }
        }
    }
}
