using FluentValidation;
using MediatR;
using MediatRPipelineFluentValidation.Behaviors;
using MediatRPipelineFluentValidation.Exceptions;
using MediatRPipelineFluentValidation.Features.Products.Commands.Create;
using MediatRPipelineFluentValidation.Features.Products.Commands.Delete;
using MediatRPipelineFluentValidation.Features.Products.Commands.Update;
using MediatRPipelineFluentValidation.Features.Products.DTOs;
using MediatRPipelineFluentValidation.Features.Products.Notifications;
using MediatRPipelineFluentValidation.Features.Products.Queries.Get;
using MediatRPipelineFluentValidation.Features.Products.Queries.List;
using MediatRPipelineFluentValidation.Persistence;
using Microsoft.AspNetCore.Components.Forms;
using System.Reflection;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>();

builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

builder.Services.AddMediatR(configuration =>
{
    configuration.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());

    configuration.AddOpenBehavior(typeof(RequestResponseLoggingBehavior<,>));
    configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

WebApplication app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/products/{id:guid}", async (Guid id, IMediator mediator) =>
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