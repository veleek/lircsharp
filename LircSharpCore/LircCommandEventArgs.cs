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

}
