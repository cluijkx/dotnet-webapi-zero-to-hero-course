using GlobalExceptionHandling;
using GlobalExceptionHandling.Exceptions;
using GlobalExceptionHandling.Handlers;
using Microsoft.AspNetCore.Diagnostics;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

builder.Services.AddExceptionHandler<ProductNotFoundExceptionHandler>();
builder.Services.AddExceptionHandler<StockExhaustedExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// default exception handling middleware in .NET - UseExceptionHandler
//app.UseExceptionHandler(options =>
//{
//    options.Run(async context =>
//    {
//        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
//        context.Response.ContentType = "application/json";

//        IExceptionHandlerFeature? exception = context.Features.Get<IExceptionHandlerFeature>();

//        if (exception != null)
//        {
//            string message = $"{exception.Error.Message}";

//            await context.Response.WriteAsync(message).ConfigureAwait(false);
//        }
//    });
//});

// custom exception handling middleware in .NET - UseMiddleware (old method)
//app.UseMiddleware<ErrorHandlerMiddleware>();

// custom exception handling middleware in .NET - UseMiddleware (recommended)
app.UseExceptionHandler();

//app.MapGet("/", () =>{ throw new Exception("An error occurred..."); });

app.MapGet("/product-not-found", () =>
{
    throw new ProductNotFoundException(Guid.NewGuid());
});

app.MapGet("/stock-exhausted", () =>
{
    throw new StockExhaustedException(Guid.NewGuid());
});

app.MapGet("/unexpected", () =>
{
    throw new InvalidOperationException("Something unexpected happened.");
});

app.Run();