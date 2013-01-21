using System;

namespace Ben.LircSharp
{
    public class LircCommandEventArgs : EventArgs
    {
        public LircCommand Command { get; private set; }

        public LircCommandEventArgs(LircCommand command)
        {
            this.Command = command;
        }
    }

    public class LircMessageEventArgs : EventArgs
    {
        public string Message { get; private set; }

        public LircMessageEventArgs(string message)
        {
            this.Message = message;
        }
    }
}
