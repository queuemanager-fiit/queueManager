using Application.Interfaces;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Table;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
builder.Services.AddScoped<Application.Interfaces.IUserRepository, Infrastructure.Repositories.UserRepository>();
builder.Services.AddScoped<Application.Interfaces.IGroupRepository, Infrastructure.Repositories.GroupRepository>();
builder.Services.AddScoped<Application.Interfaces.IUnitOfWork, Infrastructure.Repositories.UnitOfWork>();
builder.Services.AddScoped<Application.Interfaces.IEventRepository, Infrastructure.Repositories.EventRepository>();
builder.Services.AddScoped<Application.Interfaces.IEventCategoryRepository, Infrastructure.Repositories.EventCategoryRepository>(); 
builder.Services.AddHostedService<Infrastructure.Services.ExpiredEventService>();
builder.Services.AddSingleton<Schedule>(new Schedule("C:\\Users\\Пользователь\\queueManager\\Infrastructure\\Parser\\РасписаниеФИИТ2025осень.xlsx"));
builder.Services.AddHostedService<AutoEventCreationService>();

var app = builder.Build();



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // app.UseSwagger();
    // app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast")
    .WithOpenApi();
app.MapControllers();
app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}