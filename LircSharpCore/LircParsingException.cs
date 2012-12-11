using System;

namespace Ben.LircSharp
{
    public class LircParsingException : Exception
    {
        public LircParsingException(string message)
            : base(message)
        {
        }

        public LircParsingException(string expectedToken, string actualToken)
            : base(string.Format("Expected {0} token, got {1} token", expectedToken, actualToken))
        {
        }
    }
}
