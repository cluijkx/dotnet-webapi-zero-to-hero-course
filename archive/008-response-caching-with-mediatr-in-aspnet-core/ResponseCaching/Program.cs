using MediatR;
using ResponseCaching.Caching;
using ResponseCaching.Features.Products.Commands.Create;
using ResponseCaching.Features.Products.Commands.Delete;
using ResponseCaching.Features.Products.Commands.Update;
using ResponseCaching.Features.Products.Dtos;
using ResponseCaching.Features.Products.Notifications;
using ResponseCaching.Features.Products.Queries.Get;
using ResponseCaching.Features.Products.Queries.List;
using ResponseCaching.Persistence;
using System.Reflection;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>();

builder.Services.AddMediatR(configuration =>
{
    configuration.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());

    configuration.AddOpenBehavior(typeof(CachingBehavior<,>));
});

// Cache Aside Pattern: Distributed Caching > Data is stored in-memory per application instance.
builder.Services.AddDistributedMemoryCache();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.DisplayRequestDuration();
    });
}

app.MapGet("/products/{id:guid}", async (Guid id, ISender mediator) =>
{
    ProductDto product = await mediator.Send(new GetProductQuery(id));

    if (product == null)
    {
        return Results.NotFound();
    }

    return Results.Ok(product);
});

app.MapGet("/products", async (ISender mediator) =>
{
    List<ProductDto> products = await mediator.Send(new ListProductsQuery());

    return Results.Ok(products);
});

app.MapPost("/products", async (CreateProductCommand command, IMediator mediator) =>
{
    ProductDto product = await mediator.Send(command);

    if (product == null)
    {
        return Results.Problem(
            detail: "The product could not be created due to invalid data.",
            statusCode: StatusCodes.Status400BadRequest,
            title: "Product creation failed"
        );
    }

    await mediator.Publish(new ProductCreatedNotification(product.Id));

    return Results.Created($"/products/{product.Id}", product);
});

app.MapDelete("/products/{id:guid}", async (Guid id, ISender mediator) =>
{
    bool deleted = await mediator.Send(new DeleteProductCommand(id));

    if (!deleted)
    {
        return Results.NotFound(new { message = $"Product with id '{id}' not found." });
    }

    return Results.NoContent();
});

app.MapPut("/products/{id:guid}", async (Guid id, UpdateProductCommand command, IMediator mediator) =>
{
    ProductDto? product = await mediator.Send(new UpdateProductWithIdCommand(id, command));

    if (product is null)
    {
        return Results.NotFound(new { message = $"Product with id '{id}' not found." });
    }

    return Results.Ok(product);
});

app.UseHttpsRedirection();

app.Run();