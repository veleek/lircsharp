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
        public string Version { get; set; }
    }
}
