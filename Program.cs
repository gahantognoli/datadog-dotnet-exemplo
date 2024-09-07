using Datadog.Trace;
using Datadog.Trace.Configuration;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.DatadogLogs(
        apiKey: "d537121a430be139f019dfdee598df5b",
        source: "dotnet-application",
        service: "ApiDataDog",
        tags: ["env:dev", "version:1.0.0"]
    )
    .CreateLogger();

builder.Host.UseSerilog();

var settings = TracerSettings.FromDefaultSources();
settings.AnalyticsEnabled = true;
Tracer.Configure(settings);

var app = builder.Build();

app.Use(async (context, next) =>
{
    var tracer = Tracer.Instance;
    using (var scope = tracer.StartActive("http.request"))
    {
        var span = scope.Span;
        var requestInfo = $"Handling request: {context.Request.Method} {context.Request.Path}";
        Log.Information(requestInfo);
        span.SetTag("http.method", context.Request.Method);
        span.SetTag("http.url", context.Request.Path);

        try
        {
            await next.Invoke();

            span.SetTag("http.status_code", context.Response.StatusCode);

            if (context.Response.StatusCode == 404)
            {
                span.SetTag("error", "true");
                Log.Error("404 Not Found: {Method} {Path}", context.Request.Method, context.Request.Path);
            }
        }
        catch (Exception ex)
        {
            span.SetTag("error", "true");
            span.SetTag("error.message", ex.Message);
            span.SetTag("error.stack", ex.StackTrace);
            Log.Error(ex, "Unhandled exception while processing request {Method} {Path}", context.Request.Method, context.Request.Path);
        }
        finally
        {
            span.Finish();
            var responseInfo = $"Finished handling request: {context.Request.Method} {context.Request.Path} with status {context.Response.StatusCode}";
            Log.Information(responseInfo);
        }
    }
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//Add support to logging request with SERILOG
app.UseSerilogRequestLogging();

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
