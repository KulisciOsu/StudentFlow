using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace StudentFlow.Api.IntegrationTests;

public class AuthRequestBodyLimitTests
{
    [Theory]
    [InlineData("register", 16_384)]
    [InlineData("register", 16_385)]
    [InlineData("login", 16_384)]
    [InlineData("login", 16_385)]
    public async Task UnknownLengthBody_EnforcesExactByteLimitBeforeWriting(string endpoint, int bytes)
    {
        await using var app = await RegistrationApp.CreateAsync();
        await app.SetCsrfAsync();
        if (endpoint == "login")
            Assert.Equal(201, (int)(await app.Client.PostAsJsonAsync("/api/auth/register", RegistrationApp.Valid())).StatusCode);

        object payload = endpoint == "register" ? RegistrationApp.Valid()
            : new { email = "jan.novak@example.test", password = "moje-dlouhe-heslo-2026", rememberMe = false };
        var json = JsonSerializer.SerializeToUtf8Bytes(payload);
        var body = Enumerable.Repeat((byte)' ', bytes).ToArray();
        json.CopyTo(body, 0);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/auth/{endpoint}")
        {
            Content = new UnknownLengthJson(body)
        };
        request.Headers.TransferEncodingChunked = true;
        Assert.Null(request.Content.Headers.ContentLength);

        var response = await app.Client.SendAsync(request);
        if (bytes > 16_384)
        {
            Assert.Equal(413, (int)response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
            Assert.Equal("no-store", response.Headers.CacheControl!.ToString());
            var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("PAYLOAD_TOO_LARGE", problem.GetProperty("code").GetString());
            Assert.Equal(endpoint == "register" ? 0L : 1L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM app_user"));
            Assert.Equal(0L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM user_session"));
            Assert.False(response.Headers.Contains("Set-Cookie"));
        }
        else
        {
            Assert.Equal(endpoint == "register" ? 201 : 200, (int)response.StatusCode);
            Assert.Equal(1L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM app_user"));
            Assert.Equal(endpoint == "register" ? 0L : 1L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM user_session"));
        }
    }

    private sealed class UnknownLengthJson : HttpContent
    {
        private readonly byte[] body;
        public UnknownLengthJson(byte[] body)
        {
            this.body = body;
            Headers.ContentType = new MediaTypeHeaderValue("application/json");
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) => stream.WriteAsync(body).AsTask();
        protected override bool TryComputeLength(out long length) { length = 0; return false; }
    }
}
