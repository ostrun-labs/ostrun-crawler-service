using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using OstrunCrawlerService.Api;
using OstrunCrawlerService.Api.Endpoints;
using OstrunCrawlerService.Api.Fetching;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower)));

builder.Services.AddOptions<CrawlerOptions>()
    .Bind(builder.Configuration.GetSection(CrawlerOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddHttpClient<UrlFetcher>((services, client) =>
    {
        client.Timeout = Timeout.InfiniteTimeSpan;
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            services.GetRequiredService<IOptions<CrawlerOptions>>().Value.UserAgent);
    })
    // Callers match on the status they get back, so a redirect must stay visible
    // instead of being replaced by whatever the target of the redirect answers.
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false });

var app = builder.Build();

app.UseExceptionHandler();

app.MapHealthChecks("/health");
app.MapFetchEndpoints();

app.Run();
