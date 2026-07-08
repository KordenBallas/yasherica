using System.Net.NetworkInformation;
using System.Net.Sockets;
using Combat.Arena.Core;

namespace Combat.Arena.Networking
{
    /// <summary>
    /// Claims the local machine's dialable address for the migration book: the first IPv4
    /// unicast address of an up, non-loopback interface. LAN best-effort by design — behind NAT
    /// the claim is not reachable from outside (documented X1 limitation until the X3 relay).
    /// </summary>
    public class LanEndpointSource : IArenaLocalEndpointSource
    {
        public string GetLocalAddress()
        {
            try
            {
                foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus != OperationalStatus.Up
                        || nic.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    {
                        continue;
                    }

                    foreach (var address in nic.GetIPProperties().UnicastAddresses)
                    {
                        if (address.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            return address.Address.ToString();
                        }
                    }
                }
            }
            catch (NetworkInformationException)
            {
                // No claimable interface — the seat simply stays out of the address book.
            }

            return null;
        }
    }
}
