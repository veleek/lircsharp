using System.Collections.Generic;

namespace Ben.LircSharp
{
    public class LircCommand
    {
        public string Command { get; set; }
        public bool Succeeded { get; set; }
        public List<string> Data { get; set; }

        public virtual void AddData(string dataLine)
        {
            if (Data == null)
            {
                Data = new List<string>();
            }

            Data.Add(dataLine);
        }
    }

    public class LircVersionCommand : LircCommand
    {
        public LircVersionCommand()
        {
            this.Command = "Version";
        }

        public string Version
        {
            get { return Data[0]; }
        }
    }

    public class LircListRemotesCommand : LircCommand
    {
        public LircListRemotesCommand()
        {
            this.Command = "ListRemotes";
        }

        public List<string> Remotes
        {
            get { return this.Data; }
        }
    }

    public class LircListRemoteCommand : LircCommand
    {
        public LircListRemoteCommand(string remote)
        {
            this.Command = "ListRemote";
            this.Remote = remote;
        }

        public string Remote { get; set; }
    }
}
