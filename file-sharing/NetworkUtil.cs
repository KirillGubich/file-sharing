using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace file_sharing
{
    static class NetworkUtil
    {
        public static List<IPAddress> GetIPList()
        {
            string hostName = Dns.GetHostName();
            IPAddress[] ipAddrs = Dns.GetHostAddresses(hostName);
            List<IPAddress> ipList = new List<IPAddress>();
            foreach (var item in ipAddrs)
            {
                if (item.AddressFamily == AddressFamily.InterNetwork)
                    ipList.Add(item);
            }
            return ipList;
        }
    }
}
