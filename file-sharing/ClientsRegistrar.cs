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

        public void AcceptRequests(List<Client> clients, Messenger messenger, string clientName, IPAddress ownIPAddress)
        {
            UdpClient udpCLient = null;
            try
            {
                udpCLient = new UdpClient();
                IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Any, Messenger.UDP_PORT);
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
                            StartMessageReceiving(client, clients);
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

        public bool SendRequest(string name, IPAddress ipAddress)
        {
            UdpClient udpCLient = null;
            try
            {
                IPAddress broadcastIP = NetworkUtil.GetBroadcastIP(ipAddress);
                IPEndPoint iPEndPoint = new IPEndPoint(broadcastIP, Messenger.UDP_PORT);
                udpCLient = new UdpClient(Messenger.UDP_PORT, AddressFamily.InterNetwork);
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
                tcpClient.Connect(new IPEndPoint(iPAddress, Messenger.TCP_PORT));
                return tcpClient;
            }
            catch
            {
                MessageBox.Show("Failed to establish connection with " + name);
                return null;
            }
        }

        private void StartMessageReceiving(Client client, List<Client> clients)
        {
            Thread messageReceivingThread = new Thread(() => { client.ReceiveMessages(clients); });
            messageReceivingThread.IsBackground = true;
            messageReceivingThread.Start();
        }
    }
}
