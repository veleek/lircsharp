using System;

namespace Ben.LircSharp
{
    public class LircCommandFactory
    {
        public static LircCommand Create(string command)
        {
            int spaceIndex = command.IndexOf(' ');
            if (spaceIndex != -1)
            {

            }

            switch (spaceIndex == -1 ? command : command.Substring(0, spaceIndex))
            {
                case "VERSION":
                case "LIST":
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
