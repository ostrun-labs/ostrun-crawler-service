using OstrunCrawlerService.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler();

app.MapHealthChecks("/health");

app.Run();
