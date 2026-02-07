using Microsoft.EntityFrameworkCore;
using soat.eleven.kutcut.application.NotificationService;
using soat.eleven.kutcut.domain.Notifications;
using soat.eleven.kutcut.domain.Services;
using soat.eleven.kutcut.infra.Configuration;
using soat.eleven.kutcut.infra.Context;
using soat.eleven.kutcut.infra.queues;
using soat.eleven.kutcut.infra.queues.Interfaces;
using soat.eleven.kutcut.infra.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Database Context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configuration Settings
builder.Services.Configure<RabbitMQSettings>(
    builder.Configuration.GetSection("RabbitMQ"));
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));
builder.Services.Configure<AuthServiceSettings>(
    builder.Configuration.GetSection("AuthService"));

// HTTP Client Factory
builder.Services.AddHttpClient();

// RabbitMQ Services
builder.Services.AddSingleton<RabbitMQConnectionFactory>();
builder.Services.AddSingleton<IMessageListener, MessageListener>();

// Domain Services
builder.Services.AddScoped<IUserSerivce, HttpUserService>();
builder.Services.AddScoped<IUserNotificaton, EmailNotificationService>();

// Background Services
builder.Services.AddHostedService<BackgroundNotificationService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
