using System.ComponentModel.DataAnnotations;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// =======================================================================
// 1. ERROR-HANDLING MIDDLEWARE
// =======================================================================
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            error = "Internal server error.",
            details = ex.Message
        });
    }
});

// =======================================================================
// 2. AUTHENTICATION MIDDLEWARE (Token-based)
// =======================================================================
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/health")) // allow health without auth
    {
        await next();
        return;
    }

    if (!context.Request.Headers.TryGetValue("Authorization", out var token))
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsJsonAsync(new { error = "Authorization header missing" });
        return;
    }

    // Simple token validation
    if (token != "Bearer mysecrettoken")
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsJsonAsync(new { error = "Invalid or expired token" });
        return;
    }

    await next();
});

// =======================================================================
// 3. LOGGING MIDDLEWARE
// =======================================================================
app.Use(async (context, next) =>
{
    var method = context.Request.Method;
    var path = context.Request.Path;

    await next();

    var status = context.Response.StatusCode;
    Console.WriteLine($"[{DateTime.UtcNow}] {method} {path} -> {status}");
});

// =======================================================================
// In-memory thread-safe store
// =======================================================================
var users = new ConcurrentDictionary<int, User>();
var nextId = 0;

// =======================================================================
// Helpers
// =======================================================================
bool IsValidEmail(string email)
{
    if (string.IsNullOrWhiteSpace(email))
        return false;

    var pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
    return Regex.IsMatch(email, pattern, RegexOptions.IgnoreCase);
}

// =======================================================================
// Minimal API endpoints
// =======================================================================

// Create a new user
app.MapPost("/users", (UserInput input) =>
{
    if (string.IsNullOrWhiteSpace(input.Username) || string.IsNullOrWhiteSpace(input.Email))
        return Results.BadRequest(new { error = "Username and Email are required." });

    if (!IsValidEmail(input.Email))
        return Results.BadRequest(new { error = "Invalid email format." });

    var id = System.Threading.Interlocked.Increment(ref nextId);
    var now = DateTime.UtcNow;

    var user = new User
    {
        Id = id,
        Username = input.Username.Trim(),
        Email = input.Email.Trim().ToLower(),
        FullName = input.FullName?.Trim(),
        CreatedAt = now,
        UpdatedAt = null
    };

    if (!users.TryAdd(id, user))
        return Results.Json(new { error = "Could not create user." }, statusCode: 500);

    return Results.Created($"/users/{id}", user);
});

// Get all users
app.MapGet("/users", () => Results.Ok(users.Values));

// Get a user by id
app.MapGet("/users/{id:int}", (int id) =>
{
    if (users.TryGetValue(id, out var user))
        return Results.Ok(user);

    return Results.NotFound(new { error = "User not found." });
});

// Update a user
app.MapPut("/users/{id:int}", (int id, UserInput input) =>
{
    if (string.IsNullOrWhiteSpace(input.Username) || string.IsNullOrWhiteSpace(input.Email))
        return Results.BadRequest(new { error = "Username and Email are required." });

    if (!IsValidEmail(input.Email))
        return Results.BadRequest(new { error = "Invalid email format." });

    if (!users.TryGetValue(id, out var existing))
        return Results.NotFound(new { error = "User not found." });

    var updated = new User
    {
        Id = id,
        Username = input.Username.Trim(),
        Email = input.Email.Trim().ToLower(),
        FullName = input.FullName?.Trim(),
        CreatedAt = existing.CreatedAt,
        UpdatedAt = DateTime.UtcNow
    };

    if (users.TryUpdate(id, updated, existing))
        return Results.Ok(updated);

    return Results.Json(new { error = "Update failed." }, statusCode: 500);
});

// Delete a user
app.MapDelete("/users/{id:int}", (int id) =>
{
    if (users.TryRemove(id, out _))
        return Results.NoContent();

    return Results.NotFound(new { error = "User not found." });
});

// Health check (no auth required)
app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

app.Run();

// =======================================================================
// Supporting types
// =======================================================================
public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string? FullName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class UserInput
{
    [Required]
    public string Username { get; set; } = default!;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = default!;

    public string? FullName { get; set; }
}
