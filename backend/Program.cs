using System.Threading.Channels;
using DevOpsPortfolio.Backend.Data;
using Microsoft.EntityFrameworkCore;
using DevOpsPortfolio.Backend.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton(Channel.CreateUnbounded<int>());
builder.Services.AddSingleton(sp => sp.GetRequiredService<Channel<int>>().Reader);
builder.Services.AddSingleton(sp => sp.GetRequiredService<Channel<int>>().Writer);
builder.Services.AddHostedService<LlmProcessingService>();

builder.Services.AddHttpClient("Ollama", client =>
{
    client.BaseAddress = new Uri("http://llm:11434");
    client.Timeout = TimeSpan.FromMinutes(5);
});

builder.Services.AddHttpClient("SearXNG", client =>
{
    client.BaseAddress = new Uri("http://searxng:8080");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider
        .GetRequiredService<AppDbContext>();

    var pendingMigrations = await dbContext.Database
        .GetPendingMigrationsAsync();

    if (pendingMigrations.Any())
    {
        await dbContext.Database.MigrateAsync();
    }

    await DbSeeder.SeedAsync(dbContext);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthorization();

app.MapControllers();

app.Run();