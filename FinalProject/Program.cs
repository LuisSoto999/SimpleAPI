var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

var app = builder.Build();



//Copilot: Changed the list to a dictionary for faster lookups
var UserDict = new Dictionary<int, User>
{
    [1] = new User { Id = 1, Name = "One", Email = "First@email.com" },
    [2] = new User { Id = 2, Name = "Two", Email = "Second@email.com" },
};

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

app.MapGet("/", () => "This is root");

app.MapGet("/exception", () =>
{
    throw new Exception("This is a test exception!");
});

app.MapGet("/users", () => {
    return UserDict.Values;
});

// GET: Retrieve a specific user by ID
app.MapGet("/users/{id:int}", (int id) =>
{   
    //Copilot: Changed to use dictionary for faster lookup
    return UserDict.TryGetValue(id, out var user)
        ? Results.Ok(user)
        : Results.NotFound($"User with ID {id} not found.");
});

// POST: Add a new user
app.MapPost("/users", (User newUser) =>
{
    //Copilot: Added a check for empty name and email
    if (string.IsNullOrWhiteSpace(newUser.Name))
        return Results.BadRequest("Name cannot be empty.");
    if (string.IsNullOrWhiteSpace(newUser.Email) || !newUser.Email.Contains("@"))
        return Results.BadRequest("A valid email is required.");

    //Copilot: Changed to use dictionary for faster lookup
    int newId = UserDict.Any() ? UserDict.Keys.Max() + 1 : 1;
    newUser.Id = newId;
    UserDict[newId] = newUser;
    return Results.Created($"/users/{newUser.Id}", newUser);
});

// PUT: Update an existing user's details
app.MapPut("/users/{id:int}", (int id, User updatedUser) =>
{
    //Copilot: Changed to use dictionary for faster lookup
    if (!UserDict.TryGetValue(id, out var user))
        //Copilot: Added a message for not found
        return Results.NotFound($"User with ID {id} not found.");
    //Copilot: Added a check for empty name and email
    if (string.IsNullOrWhiteSpace(updatedUser.Name))
        return Results.BadRequest("Name cannot be empty.");
    if (string.IsNullOrWhiteSpace(updatedUser.Email) || !updatedUser.Email.Contains("@"))
        return Results.BadRequest("A valid email is required.");
    user.Name = updatedUser.Name;
    user.Email = updatedUser.Email;
    return Results.Ok(user);
});

// DELETE: Remove a user by ID
app.MapDelete("/users/{id:int}", (int id) =>
{
    //Copilot: Changed to use dictionary for faster lookup
    if (!UserDict.Remove(id))
        return Results.NotFound($"User with ID {id} not found.");
    return Results.NoContent();
});


app.Use(async (context, next) =>
{
    // Example: expected token value
    const string validToken = "mysecrettoken";

    // Check for Authorization header
    if (!context.Request.Headers.TryGetValue("Authorization", out var token) ||
        token != $"Bearer {validToken}")
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsync("Unauthorized: Invalid or missing token.");
        return;
    }

    await next();
});

app.Use(async (context, next) =>
{
    // Log the incoming request
    Console.WriteLine($"Request: {context.Request.Method} {context.Request.Path}");

    // Copy original response body stream
    var originalBodyStream = context.Response.Body;
    using var responseBody = new MemoryStream();
    context.Response.Body = responseBody;

    await next();

    // Read the response
    context.Response.Body.Seek(0, SeekOrigin.Begin);
    var responseText = await new StreamReader(context.Response.Body).ReadToEndAsync();
    context.Response.Body.Seek(0, SeekOrigin.Begin);

    // Log the response status code and body
    Console.WriteLine($"Response: {context.Response.StatusCode} {responseText}");

    // Copy the contents of the new memory stream (which contains the response) to the original stream
    await responseBody.CopyToAsync(originalBodyStream);
    context.Response.Body = originalBodyStream;
});

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        var errorResponse = new
        {
            error = "An unexpected error occurred.",
            detail = ex.Message
        };
        var json = System.Text.Json.JsonSerializer.Serialize(errorResponse);
        await context.Response.WriteAsync(json);
        // Log the exception (optional)
        Console.WriteLine($"[Error] {ex}");
    }
});

app.Run();

// Define a simple User class
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}


