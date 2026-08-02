namespace Adm.Infrastructure.Windows.Tests;

using Adm.Wpf.Bridge;

public sealed class BridgeContractTests
{
    private static readonly Uri Origin = new("http://127.0.0.1:5181/");
    private static readonly string[] AllowedErrorCodes = ["unsupported_version", "operation_not_allowed"];

    [Fact]
    public void ValidHostInfoRequestIsAccepted()
    {
        var request = BridgeProtocol.ParseRequest("{\"version\":\"1\",\"messageType\":\"request\",\"operation\":\"getHostInfo\",\"requestId\":\"adm-1\",\"payload\":{}}", "http://127.0.0.1:5181/", Origin);

        Assert.Equal(BridgeProtocol.GetHostInfo, request.Operation);
        Assert.Contains("AI Development Manager", BridgeProtocol.Success(request));
    }

    [Theory]
    [InlineData("version", "2")]
    [InlineData("operation", "readFile")]
    public void UnsupportedProtocolValuesAreRejected(string property, string value)
    {
        var json = $"{{\"version\":\"1\",\"messageType\":\"request\",\"operation\":\"getHostInfo\",\"requestId\":\"adm-1\",\"payload\":{{}}}}".Replace($"\"{property}\":\"{(property == "version" ? "1" : "getHostInfo")}\"", $"\"{property}\":\"{value}\"");

        var exception = Assert.Throws<BridgeProtocolException>(() => BridgeProtocol.ParseRequest(json, "http://127.0.0.1:5181/", Origin));
        Assert.Contains(exception.Code, AllowedErrorCodes);
    }

    [Fact]
    public void UnknownFieldsAndPayloadValuesAreRejected()
    {
        var exception = Assert.Throws<BridgeProtocolException>(() => BridgeProtocol.ParseRequest("{\"version\":\"1\",\"messageType\":\"request\",\"operation\":\"getHostInfo\",\"requestId\":\"adm-1\",\"payload\":{\"path\":\"secret\"},\"extra\":true}", "http://127.0.0.1:5181/", Origin));
        Assert.Equal("unknown_field", exception.Code);
    }

    [Theory]
    [InlineData("https://127.0.0.1:5181/")]
    [InlineData("http://localhost:5181/")]
    [InlineData("http://127.0.0.1:5182/")]
    public void OriginMismatchIsRejected(string source)
    {
        var json = "{\"version\":\"1\",\"messageType\":\"request\",\"operation\":\"getHostInfo\",\"requestId\":\"adm-1\",\"payload\":{}}";
        var exception = Assert.Throws<BridgeProtocolException>(() => BridgeProtocol.ParseRequest(json, source, Origin));
        Assert.Equal("origin_rejected", exception.Code);
    }

    [Fact]
    public void CancelResponseDoesNotExposeBusinessOperations()
    {
        var response = BridgeProtocol.Cancelled("adm-1");

        Assert.Contains("cancelled", response);
        Assert.DoesNotContain("readFile", response);
        Assert.DoesNotContain("execute", response);
        Assert.Single(BridgeProtocol.AllowedOperations);
        Assert.Contains(BridgeProtocol.GetHostInfo, BridgeProtocol.AllowedOperations);
    }
}
