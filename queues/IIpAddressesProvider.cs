using System.Net;

namespace queues;

public interface IIpAddressesProvider
{
    IReadOnlyList<IPEndPoint> IPAddresses { get; }
}

public class IpAddressesProvider : IIpAddressesProvider
{
    private readonly IReadOnlyList<IPEndPoint> _ipAddresses;

    public IpAddressesProvider(IReadOnlyList<IPEndPoint> ipAddresses)
    {
        _ipAddresses = ipAddresses;
    }

    public IReadOnlyList<IPEndPoint> IPAddresses
    {
        get
        {
            return _ipAddresses;
        }
    }
}