using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;

namespace file_sharing
{
    class ClientsRegistrar
    {
        private const int TCP_PORT = 31244;
        private const int UDP_PORT = 31234;

        public void AcceptRequests(List<Client> clients, Messenger messenger, string clientName, IPAddress ownIPAddress)
        {
            UdpClient udpCLient = null;
            try
            {
                udpCLient = new UdpClient();
                IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Any, UDP_PORT);
                udpCLient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                udpCLient.ExclusiveAddressUse = false;
                udpCLient.Client.Bind(iPEndPoint);
                while (true)
                {
                    byte[] receivedData = udpCLient.Receive(ref iPEndPoint);
                    string receivedName = Encoding.UTF8.GetString(receivedData);
                    bool newClient = clients.Find(client => client.IpAddress.ToString() == iPEndPoint.Address.ToString()) == null;
                    bool ownRequest = ownIPAddress.ToString() == iPEndPoint.Address.ToString();
                    if (newClient && !ownRequest)
                    {
                        TcpClient tcpClient = EstablishConnection(iPEndPoint.Address, receivedName);
                        if (tcpClient != null)
                        {
                            Client client = new Client(receivedName, iPEndPoint.Address, tcpClient);
                            clients.Add(client);
                            StartFileReceiving(client, clients);
                            messenger.SendName(client, clientName);
                            SharingManager.UpdateView();
                        }
                    }
                }
            }
            catch
            {
                MessageBox.Show("Accept client error");
            }
            finally
            {
                if (udpCLient != null)
                {
                    udpCLient.Close();
                }
            }
        }

        public void ReceiveConnectionRequests(List<Client> clients)
        {
            TcpListener tcpListener = new TcpListener(IPAddress.Any, TCP_PORT);
            tcpListener.Start();
            try
            {
                while (true)
                {
                    TcpClient tcpClient = tcpListener.AcceptTcpClient();
                    IPAddress senderIP = ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address;
                    Client senderClient = clients.Find(client => client.IpAddress == senderIP);
                    if (senderClient == null)
                    {
                        lock (SharingWindow.OnlineThreadLock)
                        {
                            Client client = new Client(null, senderIP, tcpClient);
                            clients.Add(client);
                            senderClient = client;
                        }
                        SharingManager.UpdateView();
                    }
                    StartFileReceiving(senderClient, clients);
                }
            }
            catch
            {
                MessageBox.Show("Connection request receiving error");
            }
            finally
            {
                tcpListener.Stop();
            }
        }

        public bool SendRequest(string name, IPAddress ipAddress)
        {
            UdpClient udpCLient = null;
            try
            {
                IPAddress broadcastIP = NetworkUtil.GetBroadcastIP(ipAddress);
                IPEndPoint iPEndPoint = new IPEndPoint(broadcastIP, UDP_PORT);
                udpCLient = new UdpClient(UDP_PORT, AddressFamily.InterNetwork);
                udpCLient.Connect(iPEndPoint);
                byte[] registrationMessage = Encoding.UTF8.GetBytes(name);
                int bytesSent = udpCLient.Send(registrationMessage, registrationMessage.Length);
                return bytesSent == registrationMessage.Length;
            }
            catch
            {
                MessageBox.Show("Request send error");
                return false;
            }
            finally
            {
                if (udpCLient != null)
                {
                    udpCLient.Close();
                }
            }
        }

        private TcpClient EstablishConnection(IPAddress iPAddress, string name)
        {
            try
            {
                TcpClient tcpClient = new TcpClient();
                tcpClient.Connect(new IPEndPoint(iPAddress, TCP_PORT));
                return tcpClient;
            }
            catch
            {
                MessageBox.Show("Failed to establish connection with " + name);
                return null;
            }
        }

        private void StartFileReceiving(Client client, List<Client> clients)
        {
            Thread messageReceivingThread = new Thread(() => { client.ReceiveMessages(clients); });
            messageReceivingThread.IsBackground = true;
            messageReceivingThread.Start();
        }
    }
}
