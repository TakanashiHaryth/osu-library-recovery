using System.Net;
using System.Text;
using Recovery.Core.Models;
using Recovery.OsuApi;
using Xunit;

namespace Recovery.Tests;

public class OsuApiClientTests
{
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, HttpResponseMessage> HandlerFunc { get; set; } = _ => new HttpResponseMessage(HttpStatusCode.OK);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(HandlerFunc(request));
        }
    }

    [Fact]
    public async Task AuthenticateAsync_ValidCredentials_SetsTokenAndReturnsTrue()
    {
        var handler = new MockHttpMessageHandler
        {
            HandlerFunc = req =>
            {
                if (req.RequestUri?.AbsolutePath.Contains("oauth/token") == true)
                {
                    var responseJson = "{\"access_token\": \"mock_access_token_123\", \"expires_in\": 86400, \"token_type\": \"Bearer\"}";
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
                    };
                }
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }
        };

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://osu.ppy.sh/") };
        var client = new OsuApiClient(httpClient);

        var success = await client.AuthenticateAsync(12345, "secret_key");

        Assert.True(success);
        Assert.True(client.IsAuthenticated);
    }

    [Fact]
    public async Task ResolveUserAsync_ValidUser_ReturnsDeserializedUserWithBearerHeader()
    {
        string? capturedAuthHeader = null;

        var handler = new MockHttpMessageHandler
        {
            HandlerFunc = req =>
            {
                if (req.RequestUri?.AbsolutePath.Contains("oauth/token") == true)
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("{\"access_token\": \"token_abc\", \"expires_in\": 3600}", Encoding.UTF8, "application/json")
                    };
                }

                if (req.RequestUri?.AbsolutePath.Contains("api/v2/users/peppy") == true)
                {
                    capturedAuthHeader = req.Headers.Authorization?.ToString();
                    var userJson = "{\"id\": 2, \"username\": \"peppy\", \"avatar_url\": \"https://a.ppy.sh/2\", \"country_code\": \"AU\", \"playmode\": \"osu\", \"is_supporter\": true}";
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(userJson, Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }
        };

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://osu.ppy.sh/") };
        var client = new OsuApiClient(httpClient);

        await client.AuthenticateAsync(100, "secret");
        var user = await client.ResolveUserAsync("peppy");

        Assert.NotNull(user);
        Assert.Equal(2, user.Id);
        Assert.Equal("peppy", user.Username);
        Assert.Equal("Bearer token_abc", capturedAuthHeader);
    }
}
