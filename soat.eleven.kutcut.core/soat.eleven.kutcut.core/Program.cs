using Asp.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using soat.eleven.kutcut.application.Interfaces;
using soat.eleven.kutcut.application.NotificationService;
using soat.eleven.kutcut.application.Processors;
using soat.eleven.kutcut.application.Services;
using soat.eleven.kutcut.core.api.Middlewares;
using soat.eleven.kutcut.domain.Notifications;
using soat.eleven.kutcut.domain.Services;
using soat.eleven.kutcut.infra.Configuration;
using soat.eleven.kutcut.infra.Context;
using soat.eleven.kutcut.infra.Models;
using soat.eleven.kutcut.infra.queues;
using soat.eleven.kutcut.infra.queues.Interfaces;
using soat.eleven.kutcut.infra.Repository;
using soat.eleven.kutcut.infra.Services;
using soat.eleven.kutcut.infra.Storage;

var builder = WebApplication.CreateBuilder(args);

// === Database ===
// Add services to the container.

// Database Context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// === Repositories ===
builder.Services.AddScoped<IRepository<VideoModel>, Repository<VideoModel>>();

// === Application Services ===
builder.Services.AddScoped<IVideoService, VideoService>();
builder.Services.AddSingleton<IFileStorageService>(
    new LocalFileStorageService(builder.Environment.ContentRootPath));

// === API Versioning ===
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// === Controllers ===
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

#region Background Service

// === Swagger ===
// Configuration Settings

//builder.Services.Configure<RabbitMQSettings>(
//    builder.Configuration.GetSection("RabbitMQ"));
//builder.Services.Configure<EmailSettings>(
//    builder.Configuration.GetSection("EmailSettings"));
//builder.Services.Configure<AuthServiceSettings>(
//    builder.Configuration.GetSection("AuthService"));

//// HTTP Client Factory
//builder.Services.AddHttpClient();

//// RabbitMQ Services
//builder.Services.AddSingleton<RabbitMQConnectionFactory>();
//builder.Services.AddSingleton<IMessageListener, MessageListener>();

//// Domain Services
//builder.Services.AddSingleton<IUserSerivce, HttpUserService>();
//builder.Services.AddSingleton<IUserNotificaton, EmailNotificationService>();

//// Application Services
//builder.Services.AddSingleton<IVideoMessageFactory, VideoMessageService>();
//builder.Services.AddSingleton<IVideoNotificationProcessor, VideoNotificationProcessor>();

//// Background Services
//builder.Services.AddHostedService<BackgroundNotificationService>();

#endregion

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "KutCut API",
        Version = "v1",
        Description = "API de gerenciamento de vídeos"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Insira o token JWT no formato: Bearer {token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// === Middleware de exceção global ===
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

// === Swagger UI ===
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "KutCut API v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
