using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using soat.eleven.kutcut.domain.Dtos;
using soat.eleven.kutcut.infra.Configuration;
using soat.eleven.kutcut.infra.Services;
using System.Net;
using System.Text.Json;

namespace soat.eleven.kutcut.tests.Infra;

public class HttpUserServiceTests
{
    private readonly Mock<IHttpClientFactory> _mockFactory = new();
    private readonly Mock<IOptions<AuthServiceSettings>> _mockOptions = new();
    private readonly Mock<ILogger<HttpUserService>> _mockLogger = new();

    private HttpUserService BuildService(AuthServiceSettings? settings = null)
    {
        var cfg = settings ?? new AuthServiceSettings
        {
            BaseUrl = "http://auth-service",
            TimeoutSeconds = 10
        };
        _mockOptions.Setup(o => o.Value).Returns(cfg);
        return new HttpUserService(_mockFactory.Object, _mockOptions.Object, _mockLogger.Object);
    }

    private void SetupHttpClient(HttpResponseMessage response)
    {
        var handler = new FakeHttpMessageHandler(response);
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://auth-service") };
        _mockFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
    }

    [Fact]
    public void GetUser_SuccessResponse_ReturnsDeserializedUser()
    {
        var userId = Guid.NewGuid();
        var user = new UserDto { Name = "Alice", Email = "alice@example.com" };
        var json = JsonSerializer.Serialize(user);
        SetupHttpClient(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        });

        var service = BuildService();
        var result = service.GetUser(userId);

        result.Should().NotBeNull();
        result.Name.Should().Be("Alice");
        result.Email.Should().Be("alice@example.com");
    }

    [Fact]
    public void GetUser_NonSuccessStatusCode_ThrowsHttpRequestException()
    {
        var userId = Guid.NewGuid();
        SetupHttpClient(new HttpResponseMessage(HttpStatusCode.NotFound));

        var service = BuildService();

        var act = () => service.GetUser(userId);

        act.Should().Throw<HttpRequestException>();
    }

    [Fact]
    public void GetUser_NullDeserialization_ThrowsInvalidOperationException()
    {
        var userId = Guid.NewGuid();
        // JSON "null" will deserialize to null
        SetupHttpClient(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null", System.Text.Encoding.UTF8, "application/json")
        });

        var service = BuildService();

        var act = () => service.GetUser(userId);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GetUser_HttpClientThrows_RethrowsException()
    {
        var userId = Guid.NewGuid();
        var handler = new ThrowingHttpMessageHandler(new HttpRequestException("Connection refused"));
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://auth-service") };
        _mockOptions.Setup(o => o.Value).Returns(new AuthServiceSettings
        {
            BaseUrl = "http://auth-service",
            TimeoutSeconds = 5
        });
        _mockFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        var service = new HttpUserService(_mockFactory.Object, _mockOptions.Object, _mockLogger.Object);

        var act = () => service.GetUser(userId);

        act.Should().Throw<HttpRequestException>();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private sealed class FakeHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(response);
    }

    private sealed class ThrowingHttpMessageHandler(Exception exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => throw exception;
    }
}
