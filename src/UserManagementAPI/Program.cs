using UserManagementAPI.Endpoints;
using UserManagementAPI.Middleware;
using UserManagementAPI.Storage;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// The user records live in memory for the lifetime of the process, so the store is a singleton.
builder.Services.AddSingleton<UserStore>();

var app = builder.Build();

// -------------------------------------------------------------------------------------------
//  Middleware pipeline.
//
//  Order matters, because each component wraps the ones registered after it. A request travels
//  down this list and the response travels back up it. The order below is the one the assignment
//  specifies: error handling first, authentication next, logging last.
//
//      1. Error handling  — outermost, so it can catch anything thrown by the two below it and
//                           by the endpoints, and turn it into a consistent JSON response.
//      2. Authentication  — rejects a request with a missing or invalid token before any further
//                           work is done on it.
//      3. Logging         — innermost, so the status code it records is the one the endpoint
//                           actually produced.
//
//  See docs/middleware-pipeline.md for what this ordering means in practice, including the one
//  behavior it produces that is worth knowing about.
// -------------------------------------------------------------------------------------------
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<TokenAuthenticationMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Serves the generated OpenAPI document at /openapi/v1.json ...
    app.MapOpenApi();
    // ... and the interactive Swagger UI that reads it at /swagger.
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "User Management API v1"));
}

app.UseHttpsRedirection();

// All user routes are registered from Endpoints/UserEndpoints.cs.
app.MapUserEndpoints();

app.Run();
