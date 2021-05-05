using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace file_sharing
{
    class Client
    {
        private string name;
        private IPAddress ipAddress;
        private TcpClient connection;
        private const int BUFFER_SIZE = 1_048_576;

        public Client(string name, IPAddress ipAddress, TcpClient connection)
        {
            this.name = name;
            this.ipAddress = ipAddress;
            this.connection = connection;
        }

        public string Name
        {
            get
            {
                return name;
            }
        }

        public IPAddress IpAddress
        {
            get
            {
                return ipAddress;
            }
        }

        public TcpClient Connection
        {
            get
            {
                return connection;
            }
        }

        public override bool Equals(object obj)
        {
            return obj is Client client &&
                   name == client.name &&
                   EqualityComparer<IPAddress>.Default.Equals(ipAddress, client.ipAddress);
        }

        public override int GetHashCode()
        {
            int hashCode = 862851038;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(name);
            hashCode = hashCode * -1521134295 + EqualityComparer<IPAddress>.Default.GetHashCode(ipAddress);
            hashCode = hashCode * -1521134295 + EqualityComparer<TcpClient>.Default.GetHashCode(connection);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + EqualityComparer<IPAddress>.Default.GetHashCode(IpAddress);
            hashCode = hashCode * -1521134295 + EqualityComparer<TcpClient>.Default.GetHashCode(Connection);
            return hashCode;
        }

        public void ReceiveMessages(List<Client> clients)
        {
            NetworkStream OneUserStream = Connection.GetStream();
            try
            {
                while (true)
                {
                    byte[] byteMessage = new byte[BUFFER_SIZE];
                    StringBuilder MessageBuilder = new StringBuilder();
                    string message;
                    int RecBytes = 0;
                    do
                    {
                        RecBytes = OneUserStream.Read(byteMessage, 0, byteMessage.Length);
                        MessageBuilder.Append(Encoding.UTF8.GetString(byteMessage, 0, RecBytes));
                    }
                    while (OneUserStream.DataAvailable);

                    message = MessageBuilder.ToString();
                    int messageCode = message[0];
                    switch (messageCode)
                    {
                        case Messenger.MESSAGE_CODE:
                            {
                                string resultMessage;
                                resultMessage = name + " (" + DateTime.Now.ToLongTimeString() + "): " + message.Substring(1);
                                break;
                            }
                        case Messenger.NAME_CODE:
                            {
                                name = message.Substring(1);
                                break;
                            }
                        case Messenger.USER_DISCONNECT_CODE:
                            {
                                DisconnectClient(clients);
                                break;
                            }
                    }
                    SharingManager.UpdateView();
                }
            }
            catch
            {
                DisconnectClient(clients);
            }
            finally
            {
                if (Connection != null)
                    Connection.Close();
            }
        }

        private void DisconnectClient(List<Client> clients)
        {
            clients.Remove(this);
            SharingManager.UpdateView();
        }
    }
}
