using Carter;
using intelli_dev_store.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
var configuration = builder.Configuration;
builder.Services.AddApplicationServices(configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapCarter();
app.UseHttpsRedirection();

app.Run();
