using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Windows;

namespace file_sharing
{
    class SharingManager
    {
        private readonly List<Client> clients;
        private readonly string clientName;
        private ClientsRegistrar clientsRegistrar;
        private readonly Messenger messenger;
        private IPAddress ipAddress;
        private static object locker = new object();

        public SharingManager(string clientName, IPAddress iPAddress)
        {
            this.clientName = clientName;
            this.ipAddress = iPAddress;
            clients = new List<Client>();
            clientsRegistrar = new ClientsRegistrar();
            messenger = new Messenger();
        }

        public List<Client> Clients
        {
            get
            {
                return clients;
            }
        }

        public void Initialize()
        {
            bool successfulyConected = clientsRegistrar.SendRequest(clientName, ipAddress);
            StartReceiving();
            if (!successfulyConected)
            {
                MessageBox.Show("Connection error");
            }
            UpdateView();
        }

        public static void UpdateView()
        {
            SharingWindow.GetInstance().UpdateOnlineBox();
        }

        public void Send(string filePath)
        {
            lock (locker)
            {
                messenger.SendFile(clients, filePath);
            }
        }

        public void Disconnect()
        {
            messenger.SendDisconnect(clients);
        }

        private void StartReceiving()
        {
            Thread connectionRequestsThread = new Thread(() => { clientsRegistrar.ReceiveConnectionRequests(clients); });
            Thread clientsRegistarThread = new Thread(() => { clientsRegistrar.AcceptRequests(clients, messenger, clientName, ipAddress); });
            connectionRequestsThread.IsBackground = true;
            clientsRegistarThread.IsBackground = true;
            connectionRequestsThread.Start();
            clientsRegistarThread.Start();
        }
    }

}
