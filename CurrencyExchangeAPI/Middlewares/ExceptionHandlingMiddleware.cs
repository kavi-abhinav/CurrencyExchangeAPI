using CurrencyExchangeAPI.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace CurrencyExchangeAPI.Middlewares
{
    public class ExceptionHandlingMiddleware : IMiddleware
    {
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(ILogger<ExceptionHandlingMiddleware> logger)
        {
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                await next(context);
            }
            catch (InvalidDataException ex)
            {
                _logger.LogError(ex.ToString());

                ProblemDetails problem = new()
                {
                    Status = StatusCodes.Status400BadRequest,
                    Type = "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.1",
                    Title = "Bad Request",
                    Detail = ex.Message
                };
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(problem);

            }
            catch (InvalidCurrencyException ex)
            {
                _logger.LogError(ex.ToString());

                ProblemDetails problem = new()
                {
                    Status = StatusCodes.Status400BadRequest,
                    Type = "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.1",
                    Title = "Bad Request",
                    Detail = ex.Message
                };
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(problem);

            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex.ToString());
               
                ProblemDetails problem = new()
                {
                    Status = StatusCodes.Status408RequestTimeout,
                    Type = "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.9",
                    Title = "Request Timeout",
                    Detail = "The request to the server timed out."
                };
                context.Response.StatusCode = StatusCodes.Status408RequestTimeout;
                await context.Response.WriteAsJsonAsync(problem);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());

                ProblemDetails problem = new()
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Type = "https://datatracker.ietf.org/doc/html/rfc9110#section-15.6.1",
                    Title = "Internal Server Error",
                    Detail = "The server failed to process your request."
                };
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(problem);
            }
        }
    }
}
