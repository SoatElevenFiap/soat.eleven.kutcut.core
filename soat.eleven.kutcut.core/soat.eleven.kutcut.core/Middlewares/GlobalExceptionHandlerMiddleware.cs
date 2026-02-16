using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using soat.eleven.kutcut.application.Exceptions;

namespace soat.eleven.kutcut.core.api.Middlewares
{
    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

        public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Acesso não autorizado: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex, HttpStatusCode.Unauthorized, "Não autorizado");
            }
            catch (ForbiddenAccessException ex)
            {
                _logger.LogWarning(ex, "Acesso proibido: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex, HttpStatusCode.Forbidden, "Acesso negado");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro não tratado: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex, HttpStatusCode.InternalServerError, "Erro interno do servidor");
            }
        }

        private static async Task HandleExceptionAsync(
            HttpContext context,
            Exception exception,
            HttpStatusCode statusCode,
            string title)
        {
            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = (int)statusCode;

            var problemDetails = new ProblemDetails
            {
                Status = context.Response.StatusCode,
                Title = title,
                Detail = exception.Message,
                Instance = context.Request.Path
            };

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(problemDetails, options);
            await context.Response.WriteAsync(json);
        }
    }
}
