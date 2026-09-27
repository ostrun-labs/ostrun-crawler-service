using System.Net;
using OstrunCrawlerService.Api.Fetching;

namespace OstrunCrawlerService.UnitTests.Fetching;

public class OutboundGuardTests
{
    [Theory]
    [InlineData("0.0.0.0")]
    [InlineData("10.1.2.3")]
    [InlineData("100.64.0.1")]
    [InlineData("127.0.0.1")]
    [InlineData("169.254.169.254")]
    [InlineData("172.16.0.1")]
    [InlineData("172.31.255.255")]
    [InlineData("192.168.1.1")]
    [InlineData("198.18.0.1")]
    [InlineData("224.0.0.1")]
    [InlineData("255.255.255.255")]
    [InlineData("::")]
    [InlineData("::1")]
    [InlineData("::ffff:127.0.0.1")]
    [InlineData("::ffff:10.0.0.1")]
    [InlineData("64:ff9b::a00:1")]
    [InlineData("fd00::1")]
    [InlineData("fe80::1")]
    [InlineData("ff02::1")]
    public void Refuses_internal_and_reserved_addresses(string address)
    {
        Assert.False(OutboundGuard.IsPublic(IPAddress.Parse(address)));
    }

    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("104.16.0.1")]
    [InlineData("172.32.0.1")]
    [InlineData("::ffff:8.8.8.8")]
    [InlineData("2606:4700::1111")]
    public void Allows_public_addresses(string address)
    {
        Assert.True(OutboundGuard.IsPublic(IPAddress.Parse(address)));
    }

    [Theory]
    [InlineData("http://localhost:1/")]
    [InlineData("http://127.0.0.1:1/")]
    [InlineData("http://[::1]:1/")]
    public async Task Handler_refuses_to_connect_to_loopback(string url)
    {
        using var client = new HttpClient(OutboundGuard.CreateHandler());

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(url));

        Assert.IsType<BlockedAddressException>(exception.InnerException);
    }
}
