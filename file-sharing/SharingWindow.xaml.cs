using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Media;

namespace file_sharing
{
    public partial class SharingWindow : Window
    {

        public static object OnlineThreadLock = new object();
        private SharingManager sharingManager;
        private static SharingWindow instance;
        private static int FILE_NAME_MAX_LENGTH = 22;
        private const string DRAG_FILES_LABEL = "Drag files here";
        private const string DROP_FILES_LABEL = "Drop files";
        private const string RECEIVING_LABEL = "Receiving: ";
        private const string SENDING_LABEL = "Sending: ";
        private const string FILE_MASK = "*.*";

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
        public static void SetReceivingProgress(int value)
        {
            GetInstance().receivingProgressBar.Value = value;
            GetInstance().receivingPercent.Content = value + " %";
        }

        public static void SetReceivingFile(string fileName)
        {
            if (fileName.Length > FILE_NAME_MAX_LENGTH)
            {
                fileName = fileName.Substring(0, FILE_NAME_MAX_LENGTH) + "...";
            }
            GetInstance().receivingFileNameLabel.Content = RECEIVING_LABEL + fileName;
        }

        public static void ResetReceivingProggress()
        {
            GetInstance().receivingProgressBar.Value = 0;
            GetInstance().receivingFileNameLabel.Content = "";
            GetInstance().receivingPercent.Content = "";
        }

        public static void SetSendingProgress(int value)
        {
            GetInstance().sendingProgressBar.Value = value;
            GetInstance().sendingPercent.Content = value + " %";
        }

        public static void SetSendingFile(string fileName)
        {
            if (fileName.Length > FILE_NAME_MAX_LENGTH)
            {
                fileName = fileName.Substring(0, FILE_NAME_MAX_LENGTH) + "...";
            }
            GetInstance().sendingFileNameLabel.Content = SENDING_LABEL + fileName;
        }

        public static void ResetSendingProgress()
        {
            GetInstance().sendingProgressBar.Value = 0;
            GetInstance().sendingFileNameLabel.Content = "";
            GetInstance().sendingPercent.Content = "";
        }

        private void ResetPanelInterface()
        {
            Brush brush = new SolidColorBrush(Colors.WhiteSmoke);
            fileDropPanel.Background = brush;
            panelLabel.Content = DRAG_FILES_LABEL;
        }

        private void fileDropPanel_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                panelLabel.Content = DROP_FILES_LABEL;
                e.Effects = DragDropEffects.Copy;
                Brush brush = new SolidColorBrush(Colors.LightGreen);
                fileDropPanel.Background = brush;
            }
        }

        private void fileDropPanel_DragLeave(object sender, DragEventArgs e)
        {
            ResetPanelInterface();

        }

        private void fileDropPanel_Drop(object sender, DragEventArgs e)
        {
            string[] fileDropData = (string[])e.Data.GetData(DataFormats.FileDrop);
            List<string> paths = new List<string>();
            foreach (string path in fileDropData)
            {
                if (Directory.Exists(path))
                {
                    paths.AddRange(Directory.GetFiles(path, FILE_MASK, SearchOption.AllDirectories));
                }
                else
                {
                    paths.Add(path);
                }
            }
            Thread sendThread = new Thread(() => paths.ForEach(file => sharingManager.Send(file)));
            sendThread.IsBackground = true;
            sendThread.Start();
            ResetPanelInterface();
        }
    }
}
