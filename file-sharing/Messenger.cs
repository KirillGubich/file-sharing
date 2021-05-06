using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace file_sharing
{
    class Messenger
    {
        public const int TCP_PORT = 31244;
        public const int UDP_PORT = 31234;
        public const byte FILE_CODE = 1;
        public const byte NAME_CODE = 2;
        public const byte USER_DISCONNECT_CODE = 3;

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
                    StartMessageReceiving(senderClient, clients);
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

        public void SendFile(List<Client> clients, string filePath)
        {          
            FileInfo info = new FileInfo(filePath);
            if (info.Length == 0)
            {
                return;
            }
            string fileName = info.Name;
            long fileSize = info.Length;
            byte[] fileSizeBytes = BitConverter.GetBytes(fileSize);
            byte[] messageBytes = Encoding.UTF8.GetBytes(fileName);
            byte[] messageWithCode = new byte[110];
            messageWithCode[0] = FILE_CODE;
            fileSizeBytes.CopyTo(messageWithCode, 1);
            messageWithCode[9] = (byte) fileName.Length;
            messageBytes.CopyTo(messageWithCode, 10);
            foreach (Client client in clients)
            {
                client.Connection.GetStream().Write(messageWithCode, 0, messageWithCode.Length);
            }

            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                foreach (Client client in clients)
                {
                    NetworkStream clientStream = client.Connection.GetStream();
                    fileStream.CopyTo(clientStream);
                }
            }
        }
        public void SendName(Client client, string name)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(name);
            byte[] messageWithCode = new byte[messageBytes.Length + 1];
            messageWithCode[0] = NAME_CODE;
            messageBytes.CopyTo(messageWithCode, 1);
            client.Connection.GetStream().Write(messageWithCode, 0, messageWithCode.Length);
        }
        public void SendDisconnect(List<Client> clients)
        {
            byte[] messageWithCode = new byte[1];
            messageWithCode[0] = USER_DISCONNECT_CODE;
            foreach (Client client in clients)
            {
                client.Connection.GetStream().Write(messageWithCode, 0, messageWithCode.Length);
            }
        }

        private void StartMessageReceiving(Client senderClient, List<Client> clients)
        {
            Thread messageReceivingThread = new Thread(() => { senderClient.ReceiveMessages(clients); });
            messageReceivingThread.IsBackground = true;
            messageReceivingThread.Start();
        }
    }
}
