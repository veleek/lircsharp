using System;

namespace Ben.LircSharp
{
    public class LircCommandFactory
    {
        public static LircCommand Create(string command)
        {
            string[] commandTokens= command.Split(' ');

            switch (commandTokens[0])
            {
                case "VERSION":
                    return new LircVersionCommand();
                case "LIST":
                    if (commandTokens.Length == 1)
                    {
                        return new LircListRemotesCommand();
                    }
                    else
                    {
                        return new LircListRemoteCommand(commandTokens[1]);
                    }
                case "SIGHUP":
                case "SEND_ONCE":
                case "SEND_START":
                case "SEND_STOP":
                default:
                    return new LircCommand
                    {
                        Command = command,
                    };
            }
        }
    }

}
