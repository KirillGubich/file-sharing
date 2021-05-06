using System;
using System.Collections.Generic;
using System.IO;
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

        private void fileDropPanel_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                panelLabel.Content = "Drop files";
                e.Effects = DragDropEffects.Copy;
                Brush brush = new SolidColorBrush(Colors.LightGreen);
                fileDropPanel.Background = brush;
            }
        }

        private void fileDropPanel_DragLeave(object sender, DragEventArgs e)
        {
            panelLabel.Content = "Drag files here";
            Brush brush = new SolidColorBrush(Colors.WhiteSmoke);
            fileDropPanel.Background = brush;

        }

        private void fileDropPanel_Drop(object sender, DragEventArgs e)
        {
            string[] fileDropData = (string[]) e.Data.GetData(DataFormats.FileDrop);
            List<string> paths = new List<string>();
            foreach (string path in fileDropData)
            {
                if (Directory.Exists(path))
                {
                    paths.AddRange(Directory.GetFiles(path, "*.*", SearchOption.AllDirectories));
                }
                else
                {
                    paths.Add(path);
                }
            }

            paths.ForEach(file => sharingManager.Send(file));
            Brush brush = new SolidColorBrush(Colors.WhiteSmoke);
            fileDropPanel.Background = brush;
            panelLabel.Content = "Drag files here";
        }
    }
}
