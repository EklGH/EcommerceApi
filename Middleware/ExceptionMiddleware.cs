using System.Net;
using System.Text.Json;

namespace EcommerceApi.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }


        // Intercepte toutes les requêtes
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context); // Passe au middleware suivant
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Une erreur est survenue: {ex.Message}");
                await HandleExceptionAsync(context, ex);
            }
        }

        // Définit la réponse JSON à retourner au client
        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            HttpStatusCode status;
            string message;

            switch (exception)
            {
                case KeyNotFoundException:
                    status = HttpStatusCode.NotFound;
                    message = exception.Message;
                    break;
                case UnauthorizedAccessException:
                    status = HttpStatusCode.Unauthorized;
                    message = exception.Message;
                    break;
                default:
                    status = HttpStatusCode.InternalServerError;
                    message = "Une erreur inattendue est survenue.";
                    break;
            }

            context.Response.StatusCode = (int)status;

            var response = new
            {
                statusCode = context.Response.StatusCode,
                message
            };

            var json = JsonSerializer.Serialize(response);

            return context.Response.WriteAsync(json);
        }
    }
}
