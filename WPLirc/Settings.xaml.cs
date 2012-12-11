using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;

namespace Ben.LircSharp.Phone
{
    public partial class Settings : PhoneApplicationPage
    {
        public Settings()
        {
            InitializeComponent();
        }

        private void Ok_Click(object sender, EventArgs e)
        {
            string host = HostTextBox.Text;

            if (String.IsNullOrWhiteSpace(host))
            {
                return;
            }

            ushort port;
            if (!ushort.TryParse(PortTextBox.Text, out port))
            {
                return;
            }

            // Set the host and port
            NavigationService.GoBack();
        }

        private void Cancel_Click(object sender, EventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}