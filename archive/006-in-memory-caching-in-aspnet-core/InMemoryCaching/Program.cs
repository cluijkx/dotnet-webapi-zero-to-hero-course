using InMemoryCaching.Models;
using InMemoryCaching.Persistence;
using InMemoryCaching.Services;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    // Connect to a PostgreSQL database with Npgsql
    options.UseNpgsql(builder.Configuration.GetConnectionString("dotnetSeries"));
});

// Cache Aside Pattern: In-Memory Caching > Data is cached within the server’s memory
builder.Services.AddMemoryCache();

builder.Services.AddTransient<IProductService, ProductService>();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.DisplayRequestDuration());
}

app.MapGet("/products", async (IProductService service) =>
{
    List<Product> products = await service.GetAll();

    return Results.Ok(products);
});

app.MapGet("/products/{id:guid}", async (Guid id, IProductService service) =>
{
    Product? product = await service.Get(id);

    if (product == null)
    {
        return Results.NotFound();
    }

    return Results.Ok(product);
});

app.MapPost("/products", async (ProductCreationDto product, IProductService service) =>
{
    await service.Add(product);

    return Results.Created();
});

app.UseHttpsRedirection();

app.Run();