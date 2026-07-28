using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RoomLoom.Infrastructure.Media;

namespace RoomLoom.Api.Tests;

public class LiveKitMediaProviderTests
{
    [Fact]
    public async Task GenerateJoinTokenAsync_ProducesSignedJwt_WithExpectedClaims()
    {
        var provider = new LiveKitMediaProvider(Options.Create(new LiveKitOptions
        {
            Url = "ws://localhost:7880",
            ApiKey = "devkey",
            Secret = "secret-secret-secret-secret-secret",
        }));

        var jwt = await provider.GenerateJoinTokenAsync("room-xyz", "alice");

        var parts = jwt.Split('.');
        Assert.Equal(3, parts.Length); // header.payload.signature

        var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
        using var payload = JsonDocument.Parse(payloadJson);
        var root = payload.RootElement;

        Assert.Equal("alice", root.GetProperty("sub").GetString());
        Assert.Equal("devkey", root.GetProperty("iss").GetString());

        var video = root.GetProperty("video");
        Assert.True(video.GetProperty("roomJoin").GetBoolean());
        Assert.Equal("room-xyz", video.GetProperty("room").GetString());
    }

    private static byte[] Base64UrlDecode(string s)
    {
        var padded = s.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
        return Convert.FromBase64String(padded);
    }
}
