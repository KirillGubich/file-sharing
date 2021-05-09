using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;

namespace file_sharing
{
    class Messenger
    {
        public const int TCP_PORT = 31244;
        public const int UDP_PORT = 31234;
        private const int MESSAGE_CODE_BYTE = 0;
        public const byte FILE_CODE = 1;
        public const byte NAME_CODE = 2;
        public const byte USER_DISCONNECT_CODE = 3;
        public const byte FILE_INFO_SIZE = 210;
        public const int FILE_SIZE_BYTE_INDEX = 1;
        public const int FILE_NAME_LENGTH_INDEX = 9;
        public const int FILE_NAME_BYTE_INDEX = 10;
        private static SharingWindow sharingWindow = SharingWindow.GetInstance();

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

        public void SendFile(List<Client> clients, string filePath)
        {
            FileInfo info = new FileInfo(filePath);
            if (info.Length == 0)
            {
                return;
            }
            string fileName = info.Name;
            long fileSize = info.Length;
            byte[] messageWithCode = CreateMessage(fileSize, fileName);
            foreach (Client client in clients)
            {
                client.Connection.GetStream().Write(messageWithCode, 0, messageWithCode.Length);
            }
            SendFileToClients(clients, filePath);
        }

        public void SendName(Client client, string name)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(name);
            byte[] messageWithCode = new byte[messageBytes.Length + 1];
            messageWithCode[MESSAGE_CODE_BYTE] = NAME_CODE;
            messageBytes.CopyTo(messageWithCode, 1);
            client.Connection.GetStream().Write(messageWithCode, 0, messageWithCode.Length);
        }
        public void SendDisconnect(List<Client> clients)
        {
            byte[] messageWithCode = new byte[1];
            messageWithCode[MESSAGE_CODE_BYTE] = USER_DISCONNECT_CODE;
            foreach (Client client in clients)
            {
                client.Connection.GetStream().Write(messageWithCode, 0, messageWithCode.Length);
            }
        }

        private void SendFileToClients(List<Client> clients, string filePath)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(filePath);
                sharingWindow.Dispatcher.Invoke(() => SharingWindow.SetSendingFile(fileInfo.Name));
                using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    byte[] fileBuffer = new byte[FileManager.FILE_BUFFER_SIZE];
                    int bytesRead;
                    long totalSent = 0;
                    while ((bytesRead = fileStream.Read(fileBuffer, 0, fileBuffer.Length)) > 0)
                    {
                        clients.ForEach((client) => client.Connection.GetStream().Write(fileBuffer, 0, bytesRead));
                        totalSent += bytesRead;
                        int progress = FileManager.CountProgress(totalSent, fileInfo.Length);
                        sharingWindow.Dispatcher.Invoke(() => SharingWindow.SetSendingProgress(progress));
                    }
                }
            }
            catch
            {
                MessageBox.Show("Connection was broken. File was not sent", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            sharingWindow.Dispatcher.Invoke(() => SharingWindow.ResetSendingProgress());
        }

        private byte[] CreateMessage(long fileSize, string fileName)
        {
            byte[] fileSizeBytes = BitConverter.GetBytes(fileSize);
            byte[] fileNameBytes = Encoding.UTF8.GetBytes(fileName);
            byte[] message = new byte[FILE_INFO_SIZE];
            message[MESSAGE_CODE_BYTE] = FILE_CODE;
            fileSizeBytes.CopyTo(message, FILE_SIZE_BYTE_INDEX);
            message[FILE_NAME_LENGTH_INDEX] = (byte)fileName.Length;
            fileNameBytes.CopyTo(message, FILE_NAME_BYTE_INDEX);
            return message;
        }

        private void StartFileReceiving(Client senderClient, List<Client> clients)
        {
            Thread messageReceivingThread = new Thread(() => { senderClient.ReceiveMessages(clients); });
            messageReceivingThread.IsBackground = true;
            messageReceivingThread.Start();
        }
    }
}
