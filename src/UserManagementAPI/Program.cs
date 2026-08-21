var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Serves the generated OpenAPI document at /openapi/v1.json ...
    app.MapOpenApi();
    // ... and the interactive Swagger UI that reads it at /swagger.
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "User Management API v1"));
}

app.UseHttpsRedirection();

app.Run();
