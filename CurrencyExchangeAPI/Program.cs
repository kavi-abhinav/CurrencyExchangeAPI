
using CurrencyExchangeAPI.Middlewares;
using CurrencyExchangeAPI.Services;
using Microsoft.Extensions.Configuration;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Extensions.Http;
using Serilog;

namespace CurrencyExchangeAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            //setup serilog
            var logOutputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3} @ CurrencyExchangeAPI:" + Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") + "] {Message:lj}{NewLine}{Exception}";
            string frankfurterApiBaseUrl = builder.Configuration.GetValue<string>("API:FrankfurterApiBaseUrl") ?? "https://api.frankfurter.app/";
            int maxTimeout = builder.Configuration.GetValue<int?>("API:MaxTimeout") ?? 30;
            int maxRetries = builder.Configuration.GetValue<int?>("API:MaxRetryCount") ?? 3;

            Log.Logger = new LoggerConfiguration()
                         .ReadFrom.Configuration(builder.Configuration)
                         .Enrich.FromLogContext()
                         .WriteTo.Console(outputTemplate: logOutputTemplate)    //Setup serilog to write to console (i.e standard output)
                         .CreateLogger();


            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddTransient<ExceptionHandlingMiddleware>();
            builder.Services.AddSerilog();
            builder.Services.AddMemoryCache();
            builder.Services.AddHttpClient<ICurrencyService, FrankfurterCurrencyService>(client =>
            {
                client.BaseAddress = new Uri(frankfurterApiBaseUrl);
                //Timeout defines the overall timeout for our api, even if the retries are not finished our api will timeout after this limit
                client.Timeout = TimeSpan.FromSeconds(maxTimeout);
            })
            .AddPolicyHandler(CreateRetryPolicy(maxRetries));

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.UseMiddleware<ExceptionHandlingMiddleware>();

            app.MapControllers();

            app.Run();
        }

        static IAsyncPolicy<HttpResponseMessage>  CreateRetryPolicy(int maxRetries)
        {
            //Defines re-try policy with exponential backoff and jitter
            var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(1), retryCount: maxRetries);

            return HttpPolicyExtensions
                .HandleTransientHttpError() //adds expections, 5xx and timeout codes
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)  //we can add more http codes here if needed
                .WaitAndRetryAsync(delay);
        }
    }
}
