using Carter;
using intelli_dev_store.Extensions;
using Wolverine.Http;

ServiceExtensions.ConfigureBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilogLogging();
builder.Services.AddOpenApi();

var configuration = builder.Configuration;
builder.Services.AddApplicationServices(configuration);
builder.Services.AddEndpointsApiExplorer();
builder.AddWolverineWithRabbitMq();
builder.Services.AddSwaggerGen();
builder.Services.AddWolverineHttp();

var app = builder.Build();
app.UseSerilogRequestLoggingMiddleware();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapCarter();
app.MapWolverineEndpoints();
app.UseHttpsRedirection();

app.Run();
