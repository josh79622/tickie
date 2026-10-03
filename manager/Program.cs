using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using Tickie.Manager.Data;
using Tickie.Manager.Entities;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var connectionString = 
    builder.Configuration.GetConnectionString("Tickie")
    ??
    throw new InvalidOperationException("Connection string 'Tickie' not found.");

builder.Services.AddDbContext<TickieDbContext>(
    options => 
        options
        .UseSqlite(connectionString)
);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TickieDbContext>();

    var setting = db.UserSettings.Find(1);
    if (setting == null)
    {
        var newSetting = new UserSettings
        {
            Id = 1,
            DefaultAgent = "Manual"
        };
        db.UserSettings.Add(newSetting);
        db.SaveChanges();
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
