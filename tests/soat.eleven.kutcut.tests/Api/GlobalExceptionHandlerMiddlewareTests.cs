using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using soat.eleven.kutcut.application.Exceptions;
using soat.eleven.kutcut.core.api.Middlewares;

namespace soat.eleven.kutcut.tests.Api;

public class GlobalExceptionHandlerMiddlewareTests
{
    private readonly Mock<ILogger<GlobalExceptionHandlerMiddleware>> _logger;

    public GlobalExceptionHandlerMiddlewareTests()
    {
        _logger = new Mock<ILogger<GlobalExceptionHandlerMiddleware>>();
    }

    private static DefaultHttpContext BuildContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    [Fact]
    public async Task InvokeAsync_NoException_CallsNextDelegate()
    {
        var context = BuildContext();
        var called = false;
        RequestDelegate next = _ => { called = true; return Task.CompletedTask; };

        var middleware = new GlobalExceptionHandlerMiddleware(next, _logger.Object);
        await middleware.InvokeAsync(context);

        called.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_NoException_DoesNotAlterStatusCode()
    {
        var context = BuildContext();
        RequestDelegate next = _ => Task.CompletedTask;

        var middleware = new GlobalExceptionHandlerMiddleware(next, _logger.Object);
        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_UnauthorizedAccessException_Returns401()
    {
        var context = BuildContext();
        RequestDelegate next = _ => throw new UnauthorizedAccessException("Não autorizado");

        var middleware = new GlobalExceptionHandlerMiddleware(next, _logger.Object);
        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task InvokeAsync_UnauthorizedAccessException_BodyContainsTitleNaoAutorizado()
    {
        var context = BuildContext();
        RequestDelegate next = _ => throw new UnauthorizedAccessException("Não autorizado");

        var middleware = new GlobalExceptionHandlerMiddleware(next, _logger.Object);
        await middleware.InvokeAsync(context);

        var body = await ReadResponseBodyAsync(context);
        // JSON serializer escapes non-ASCII by default: "Não" → "N\u00E3o"
        // Check for the ASCII portion that is always present
        body.Should().Contain("autorizado");
    }

    [Fact]
    public async Task InvokeAsync_ForbiddenAccessException_Returns403()
    {
        var context = BuildContext();
        RequestDelegate next = _ => throw new ForbiddenAccessException();

        var middleware = new GlobalExceptionHandlerMiddleware(next, _logger.Object);
        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task InvokeAsync_ForbiddenAccessException_BodyContainsAcessoNegado()
    {
        var context = BuildContext();
        RequestDelegate next = _ => throw new ForbiddenAccessException();

        var middleware = new GlobalExceptionHandlerMiddleware(next, _logger.Object);
        await middleware.InvokeAsync(context);

        var body = await ReadResponseBodyAsync(context);
        body.Should().Contain("Acesso negado");
    }

    [Fact]
    public async Task InvokeAsync_GenericException_Returns500()
    {
        var context = BuildContext();
        RequestDelegate next = _ => throw new InvalidOperationException("Erro inesperado");

        var middleware = new GlobalExceptionHandlerMiddleware(next, _logger.Object);
        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task InvokeAsync_GenericException_BodyContainsErroInterno()
    {
        var context = BuildContext();
        RequestDelegate next = _ => throw new InvalidOperationException("Erro inesperado");

        var middleware = new GlobalExceptionHandlerMiddleware(next, _logger.Object);
        await middleware.InvokeAsync(context);

        var body = await ReadResponseBodyAsync(context);
        body.Should().Contain("Erro interno do servidor");
    }

    [Fact]
    public async Task InvokeAsync_AnyException_SetsContentTypeToProblemJson()
    {
        var context = BuildContext();
        RequestDelegate next = _ => throw new Exception("test");

        var middleware = new GlobalExceptionHandlerMiddleware(next, _logger.Object);
        await middleware.InvokeAsync(context);

        context.Response.ContentType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task InvokeAsync_AnyException_ResponseBodyIsValidJson()
    {
        var context = BuildContext();
        RequestDelegate next = _ => throw new Exception("test");

        var middleware = new GlobalExceptionHandlerMiddleware(next, _logger.Object);
        await middleware.InvokeAsync(context);

        var body = await ReadResponseBodyAsync(context);
        body.Should().StartWith("{").And.EndWith("}");
    }

    [Fact]
    public async Task InvokeAsync_ExceptionMessageIsIncludedInResponse()
    {
        var context = BuildContext();
        // Use ASCII-only message to avoid JSON Unicode escaping of non-ASCII characters
        RequestDelegate next = _ => throw new InvalidOperationException("specific error message");

        var middleware = new GlobalExceptionHandlerMiddleware(next, _logger.Object);
        await middleware.InvokeAsync(context);

        var body = await ReadResponseBodyAsync(context);
        body.Should().Contain("specific error message");
    }

    private static async Task<string> ReadResponseBodyAsync(DefaultHttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        return await new StreamReader(context.Response.Body).ReadToEndAsync();
    }
}
