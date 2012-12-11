using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.IO;
using System.Text;

namespace Ben.LircSharp.Phone
{
    public static class StreamReaderExtensions
    {
        private static char[] buffer = new char[1024];

        public static string ReadString(this StreamReader reader)
        {
            int charsRead = 0;
            StringBuilder sb = new StringBuilder();

            while ((charsRead = reader.Read(buffer, 0, buffer.Length)) > 0)
            {
                sb.Append(buffer, 0, charsRead);
            }

            return sb.ToString();
        }
    }
}
