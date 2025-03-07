using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;


const string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy  =>
                      {
                          policy.WithOrigins("http://localhost:4000", "http://localhost:3000")
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                      });
});


var app = builder.Build();

app.UseCors(MyAllowSpecificOrigins);

app.MapGet("/", () => new { payload = "hello world" });

app.Run("http://localhost:5058");