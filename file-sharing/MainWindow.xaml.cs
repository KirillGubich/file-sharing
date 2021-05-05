using System.Collections.Generic;
using System.Net;
using System.Windows;
using System.Windows.Controls;

namespace file_sharing
{

    public partial class MainWindow : Window
    {
        private IPAddress selectedIPAddress;

        public MainWindow()
        {
            InitializeComponent();
            FillComboBox();
        }

        private void FillComboBox()
        {
            List<IPAddress> ipList = NetworkUtil.GetIPList();
            foreach (IPAddress ip in ipList)
            {
                cmboxUserIP.Items.Add(ip.ToString());
            }
            cmboxUserIP.SelectedIndex = 0;
            selectedIPAddress = ipList[0];
        }

        private void cmboxUserIP_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedIPAddress = IPAddress.Parse(cmboxUserIP.SelectedItem.ToString());
        }

        private void btnConnectClick(object sender, RoutedEventArgs e)
        {
            SharingWindow sharingWindow = new SharingWindow();
            sharingWindow.Init(nameInput.Text, selectedIPAddress);
            sharingWindow.Show();
            Close();
        }
    }
}
