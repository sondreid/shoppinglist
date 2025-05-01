using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;


const string allow_origins = "frontend_ports";

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Handleliste API", Version = "v1" });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: allow_origins,
                      policy  =>
                      {
                          policy.WithOrigins("http://localhost:4000", "http://localhost:4001", "http://frontend:4000","http://localhost:3000", "http://172.18.0.3:4000", "http://172.18.0.2:4000", "http://172.18.0.2:5058")
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                      });
});


var app = builder.Build();
//app.MapControllers();

app.UseCors(allow_origins);

app.MapGet("/", () => new { payload = "hello world" });
app.MapPost("/storeitem", async (HttpContext context) =>
{
    try 
    {
        using var reader = new StreamReader(context.Request.Body);
        var jsonBody = await reader.ReadToEndAsync();
        console.WriteLine(jsonBody);
        string filePath = "items.json";
        await File.WriteAllTextAsync(filePath, jsonBody);
        
        return Results.Json(new { success = true, message = $"Data stored successfully {jsonBody}" });
    }
    catch (Exception ex)
    {
        return Results.Json(new { success = false, message = ex.Message }, statusCode: 500);
    }
});


app.MapDelete("/deleteitem", async (HttpContext context) =>
{
    try 
    {
        string filePath = "items.json";
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            return Results.Json(new { success = true, message = "File deleted successfully" });
        }
        else
        {
            return Results.Json(new { success = false, message = "File not found" }, statusCode: 404);
        }
    }
    catch (Exception ex)
    {
        return Results.Json(new { success = false, message = ex.Message }, statusCode: 500);
    }
});


app.Use(async (context, next) =>    
{
    if(context.Request.Path.Value == "/favicon.ico")
    {
        // Favicon request, return 404
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }
    
    // No favicon, call next middleware
    await next.Invoke();
});

app.MapGet("/health", () => new { payload = "A-okay" });


if ( Environment.GetEnvironmentVariable("dev") != "true")
{
   app.Run("http://0.0.0.0:5058");
}

else
{
    app.Run("http://localhost:5058");
}

