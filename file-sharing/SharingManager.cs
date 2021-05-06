using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
            messenger.SendFile(clients, filePath);
        }

        public void Disconnect()
        {
            messenger.SendDisconnect(clients);
        }

        private void StartReceiving()
        {
            Thread messengerThread = new Thread(() => { messenger.ReceiveConnectionRequests(clients); });
            Thread clientsRegistarThread = new Thread(() => { clientsRegistrar.AcceptRequests(clients, messenger, clientName, ipAddress); });
            messengerThread.IsBackground = true;
            clientsRegistarThread.IsBackground = true;
            messengerThread.Start();
            clientsRegistarThread.Start();
        }
    }

}
