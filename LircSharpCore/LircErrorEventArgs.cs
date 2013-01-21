using System;

namespace Ben.LircSharp
{
    public class LircErrorEventArgs : EventArgs
    {
        public String Message { get; private set; }
        public Exception Exception { get; private set; }

        public LircErrorEventArgs(String message) : this(message, null)
        {
        }

        public LircErrorEventArgs(String message, Exception exception)
        {
            this.Message = message;
            this.Exception = exception;
        }
    }

}
