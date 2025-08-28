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
builder.Services.AddMemoryCache(options =>
{
    // SizeLimit: Sets total cache capacity in arbitrary units (default = unlimited).
    options.SizeLimit = 1024;
});

builder.Services.AddTransient<IProductService, ProductService>();

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