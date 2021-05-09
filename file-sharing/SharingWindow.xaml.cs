using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
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
        public static void SetReceivingProgress(string fileName, int value)
        {
            if (fileName.Length > 21)
            {
                fileName = fileName.Substring(0, 22) + "...";
            }
            GetInstance().receivingProgressBar.Value = value;
            GetInstance().receivingFileNameLabel.Content = "Receiving: " + fileName;
            GetInstance().receivingPercent.Content = value + " %";
        }

        public static void ResetReceivingProggress()
        {
            GetInstance().receivingProgressBar.Value = 0;
            GetInstance().receivingFileNameLabel.Content = "";
            GetInstance().receivingPercent.Content = "";
        }

        public static void SetSendingProgress(string fileName, int value)
        {
            if (fileName.Length > 21)
            {
                fileName = fileName.Substring(0, 22) + "...";
            }
            GetInstance().sendingProgressBar.Value = value;
            GetInstance().sendingFileNameLabel.Content = "Sending: " + fileName;
            GetInstance().sendingPercent.Content = value + " %";
        }

        public static void ResetSendingProgress()
        {
            GetInstance().sendingProgressBar.Value = 0;
            GetInstance().sendingFileNameLabel.Content = "";
            GetInstance().sendingPercent.Content = "";
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
            Thread sendThread = new Thread(() => paths.ForEach(file => sharingManager.Send(file)));
            sendThread.IsBackground = true;
            sendThread.Start();
            Brush brush = new SolidColorBrush(Colors.WhiteSmoke);
            fileDropPanel.Background = brush;
            panelLabel.Content = "Drag files here";
        }
    }
}
