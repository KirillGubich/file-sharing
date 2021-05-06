using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace file_sharing
{
    public partial class SharingWindow : Window
    {

        public static object OnlineThreadLock = new object();
        private SharingManager sharingManager;
        private static SharingWindow instance;

        private SharingWindow()
        {
            InitializeComponent();
        }

        public static SharingWindow GetInstance()
        {
            if (instance == null)
            {
                instance = new SharingWindow();
            }
            return instance;
        }

        public void Init(string name, IPAddress iPAddress)
        {
            sharingManager = new SharingManager(name, iPAddress);
            sharingManager.Initialize();
        }

        public void UpdateOnlineBox()
        {
            int i;
            int listCount = 0;
            int onlineCount;

            Dispatcher.Invoke(() =>
            {
                lock (OnlineThreadLock)
                {
                    onlineCount = sharingManager.Clients.Count;
                    clientsListBox.Items.Clear();
                    for (i = 0; i < sharingManager.Clients.Count; i++)
                    {
                        clientsListBox.Items.Add(sharingManager.Clients[i].Name + " [" + sharingManager.Clients[i].IpAddress + "]");
                    }
                    listCount = clientsListBox.Items.Count;
                }
            });
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            sharingManager.Send("D:/photo.png");
        }
    }
}
