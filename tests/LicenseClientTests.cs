using System;
using System.Threading.Tasks;
using Licensetorun.Licensing;
using Xunit;

public class LicenseClientTests
{
    [Fact]
    public void RequiresApiBaseAndProductId()
    {
        Assert.Throws<ArgumentException>(() => new LicenseClient("", "p"));
        Assert.Throws<ArgumentException>(() => new LicenseClient("https://x.test", ""));
    }

    [Fact]
    public void InstanceDefaultsToMachineName()
    {
        var client = new LicenseClient("https://x.test/", "p");
        Assert.False(string.IsNullOrEmpty(client.Instance));
    }

    [Fact]
    public async Task TransportFailureIsNetworkError()
    {
        var client = new LicenseClient("http://127.0.0.1:1", "p", "k", timeout: TimeSpan.FromMilliseconds(500));
        var result = await client.ValidateAsync();

        Assert.False(result.Ok);
        Assert.Equal(0, result.Status);
        Assert.Equal("network_error", result.Body.GetProperty("error").GetString());
    }
}
