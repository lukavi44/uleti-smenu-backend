﻿using System.Net;
using System.Text.Json;

namespace API.Middlewares
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occured.");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var response = context.Response;
            response.ContentType = "application/json";

            var statusCode = exception switch
            {
                KeyNotFoundException => (int)HttpStatusCode.NotFound, // 404
                UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized, // 401
                ArgumentException => (int)HttpStatusCode.BadRequest, // 400
                _ => (int)HttpStatusCode.InternalServerError // 500
            };

            response.StatusCode = statusCode;

            var errorResponse = new
            {
                StatusCode = statusCode,
                Message = exception.Message
            };

            var result = JsonSerializer.Serialize(errorResponse);
            return response.WriteAsync(result);
        }
    }
}
