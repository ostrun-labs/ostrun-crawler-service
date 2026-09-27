using System.Net;
using System.Net.Sockets;

namespace OstrunCrawlerService.Api.Fetching;

public sealed class BlockedAddressException(string host)
    : Exception($"'{host}' does not resolve to any public address.");

public static class OutboundGuard
{
    private static readonly IPNetwork[] BlockedNetworks =
    [
        IPNetwork.Parse("0.0.0.0/8"),
        IPNetwork.Parse("10.0.0.0/8"),
        IPNetwork.Parse("100.64.0.0/10"),
        IPNetwork.Parse("127.0.0.0/8"),
        IPNetwork.Parse("169.254.0.0/16"),
        IPNetwork.Parse("172.16.0.0/12"),
        IPNetwork.Parse("192.0.0.0/24"),
        IPNetwork.Parse("192.168.0.0/16"),
        IPNetwork.Parse("198.18.0.0/15"),
        IPNetwork.Parse("224.0.0.0/4"),
        IPNetwork.Parse("240.0.0.0/4"),
        IPNetwork.Parse("::/128"),
        IPNetwork.Parse("::1/128"),
        IPNetwork.Parse("64:ff9b::/96"),
        IPNetwork.Parse("fc00::/7"),
        IPNetwork.Parse("fe80::/10"),
        IPNetwork.Parse("ff00::/8"),
    ];

    public static bool IsPublic(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        return !BlockedNetworks.Any(network => network.Contains(address));
    }

    public static SocketsHttpHandler CreateHandler() => new()
    {
        // Callers match on the status they get back, so a redirect must stay visible
        // instead of being replaced by whatever the target of the redirect answers.
        AllowAutoRedirect = false,
        // Through a proxy, ConnectAsync would only ever see the proxy's address.
        UseProxy = false,
        ConnectCallback = ConnectAsync,
    };

    // Checking the address at connect time, rather than resolving the host up front,
    // closes the gap where DNS answers differently between the check and the request.
    private static async ValueTask<Stream> ConnectAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        var host = context.DnsEndPoint.Host;
        var addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);
        var publicAddresses = addresses.Where(IsPublic).ToArray();
        if (publicAddresses.Length == 0)
        {
            throw new BlockedAddressException(host);
        }

        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            await socket.ConnectAsync(publicAddresses, context.DnsEndPoint.Port, cancellationToken);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }
}
