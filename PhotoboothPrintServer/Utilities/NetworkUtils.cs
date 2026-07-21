using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace PhotoboothPrintServer.Utilities;

public static class NetworkUtils
{
    public static string GetLocalIPv4Address()
    {
        try
        {
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up)
                    continue;

                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;

                var ipProps = ni.GetIPProperties();

                bool hasGateway = ipProps.GatewayAddresses
                    .Any(g => g.Address.AddressFamily == AddressFamily.InterNetwork);

                if (!hasGateway)
                    continue;

                foreach (var addr in ipProps.UnicastAddresses)
                {
                    if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return addr.Address.ToString();
                    }
                }
            }

            // Fallback jika tidak ada adapter dengan gateway terdeteksi
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var fallback = host.AddressList
                .FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);

            return fallback?.ToString() ?? "Not Connected";
        }
        catch
        {
            return "Not Connected";
        }
    }
}