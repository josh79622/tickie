using Microsoft.EntityFrameworkCore;
using Tickie.Manager.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
