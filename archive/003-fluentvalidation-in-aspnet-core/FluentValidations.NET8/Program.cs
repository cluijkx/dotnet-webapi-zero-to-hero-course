using FluentValidation;
using FluentValidation.Results;
using FluentValidations.NET8.Requests;
using FluentValidations.NET8.Validators;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// register single validator
//builder.Services.AddScoped<IValidator<UserRegistrationRequest>, UserRegistrationValidator>();

// register multiple validators
builder.Services.AddValidatorsFromAssemblyContaining<UserRegistrationValidator>();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/register", async (UserRegistrationRequest request, IValidator<UserRegistrationRequest> validator) =>
{
    ValidationResult validationResult = await validator.ValidateAsync(request);

    if (!validationResult.IsValid)
    {
        return Results.ValidationProblem(validationResult.ToDictionary());
    }

    return Results.Accepted();
});

app.Run();