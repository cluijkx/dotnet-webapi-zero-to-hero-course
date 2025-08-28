using DistributedCaching.Models;
using DistributedCaching.Persistence;
using DistributedCaching.Services;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    // Connect to a PostgreSQL database with Npgsql
    options.UseNpgsql(builder.Configuration.GetConnectionString("dotnetSeries"));
});

builder.Services.AddTransient<IProductService, ProductService>();

// Cache Aside Pattern: Distributed Caching > Data is stored external to the application.
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost";

    options.ConfigurationOptions = new ConfigurationOptions()
    {
        AbortOnConnectFail = true,
        EndPoints = { options.Configuration }
    };
});

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.DisplayRequestDuration());
}

app.MapDelete("/products/{id:guid}", async (Guid id, IProductService service) =>
{
    bool deleted = await service.DeleteProduct(id);

    if (!deleted)
    {
        return Results.NotFound(new { message = $"Product with id '{id}' not found." });
    }

    return Results.NoContent();
});

app.MapGet("/products", async (IProductService service) =>
{
    List<ProductDto> products = await service.GetProducts();

    return Results.Ok(products);
});

app.MapGet("/products/{id:guid}", async (Guid id, IProductService service) =>
{
    ProductDto? product = await service.GetProduct(id);

    if (product == null)
    {
        return Results.NotFound();
    }

    return Results.Ok(product);
});

app.MapPost("/products", async (ProductCreateDto request, IProductService service) =>
{
    ProductDto? product = await service.AddProduct(request);

    if (product == null)
    {
        return Results.Problem(
            detail: "The product could not be created due to invalid data.",
            statusCode: StatusCodes.Status400BadRequest,
            title: "Product creation failed"
        );
    }

    return Results.Created($"/products/{product.Id}", product);
});

app.MapPut("/products/{id:guid}", async (Guid id, ProductUpdateDto request, IProductService service) =>
{
    ProductDto? product = await service.UpdateProduct(id, request);

    if (product is null)
    {
        return Results.NotFound(new { message = $"Product with id '{id}' not found." });
    }

    return Results.Ok(product);
});

app.UseHttpsRedirection();

app.Run();