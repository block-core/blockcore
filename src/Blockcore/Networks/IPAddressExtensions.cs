using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Blockcore.Networks
{
    public static class IPAddressExtensions
    {
        public static IPAddress MapToIPv6(this IPAddress addr)
        {
            if (addr.AddressFamily != AddressFamily.InterNetwork)
                throw new ArgumentException("Must pass an IPv4 address to MapToIPv6");

            string ipv4str = addr.ToString();

            return IPAddress.Parse("::ffff:" + ipv4str);
        }

        public static bool IsIPv4MappedToIPv6(this IPAddress addr)
        {
            bool pass1 = addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6, pass2;

            try
            {
                pass2 = (addr.ToString().StartsWith("0000:0000:0000:0000:0000:ffff:") ||
                        addr.ToString().StartsWith("0:0:0:0:0:ffff:") ||
                        addr.ToString().StartsWith("::ffff:")) &&
                        IPAddress.Parse(addr.ToString().Substring(addr.ToString().LastIndexOf(":") + 1)).AddressFamily == AddressFamily.InterNetwork;
            }
            catch
            {
                return false;
            }

            return pass1 && pass2;
        }

        public static IPAddress MapStringToIpAddress(this string str)
        {
            return IPAddress.Parse(str);
        }
    }
}
