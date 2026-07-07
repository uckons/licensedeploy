using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace EnterpriseLicenseDeployer.Services
{
    public class ActiveNetworkInfo
    {
        public string IpAddress { get; set; } = string.Empty;
        public string MacAddress { get; set; } = string.Empty; // formatted "AA-BB-CC-DD-EE-FF"
        public string AdapterName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Detects the currently active (Up, non-loopback) network adapter,
    /// and returns its IPv4 address and physical (MAC) address.
    /// </summary>
    public class NetworkService
    {
        /// <summary>
        /// Returns info for the first "Up" adapter that has a valid IPv4 address
        /// and a gateway configured (i.e. it looks like the real, in-use connection).
        /// Falls back to any Up adapter with an IPv4 address if no gateway is found.
        /// </summary>
        public ActiveNetworkInfo? GetActiveNetworkInfo()
        {
            var candidates = NetworkInterface.GetAllNetworkInterfaces()
                .Where(nic => nic.OperationalStatus == OperationalStatus.Up
                              && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback
                              && nic.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                .ToList();

            // Prefer adapters that have a default gateway (real active connection).
            var withGateway = candidates
                .Where(nic => nic.GetIPProperties().GatewayAddresses
                    .Any(g => g.Address.AddressFamily == AddressFamily.InterNetwork
                              && !g.Address.Equals(IPAddress.Any)))
                .ToList();

            var searchList = withGateway.Count > 0 ? withGateway : candidates;

            foreach (var nic in searchList)
            {
                var ipProps = nic.GetIPProperties();
                var ipv4 = ipProps.UnicastAddresses
                    .FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork);

                if (ipv4 == null) continue;

                var mac = nic.GetPhysicalAddress();
                var macBytes = mac.GetAddressBytes();
                if (macBytes.Length == 0) continue;

                var macFormatted = string.Join("-", macBytes.Select(b => b.ToString("X2")));

                return new ActiveNetworkInfo
                {
                    IpAddress = ipv4.Address.ToString(),
                    MacAddress = macFormatted,
                    AdapterName = nic.Name
                };
            }

            return null;
        }
    }
}
