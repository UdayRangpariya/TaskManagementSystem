using Npgsql;
using Repositories.Interface;
using Repositories.Implementation; 
using Repositories.Implementation.AdminRepo;
using Repositories.Implementation.UserRepo;
using Repositories.Model.AdminModels;
using API.Services;
using API.Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Microsoft.Net.Http.Headers;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using tmp;
using System.Security.Claims;



var builder = WebApplication.CreateBuilder(args);

// Register Services
builder.Services.AddScoped<IAuthInterface>(provider =>
    new Auth(builder.Configuration.GetConnectionString("pgconn")));
builder.Services.AddSingleton<RabbitMQService>();
builder.Services.AddSingleton<RedisService>();

builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<NotificationInterface, NotificationRepo>();
builder.Services.AddScoped<AdminInterface, AdminRepo>();
builder.Services.AddScoped<UserInterface, UserRepo>();
builder.Services.AddSingleton<ITaskInterface, TaskRepository>();
builder.Services.AddSingleton<ElasticsearchService>();

// Register SignalR
builder.Services.AddSignalR();
builder.Services.AddSingleton(provider =>
{
    var configuration = builder.Configuration;
    var settings = new ElasticsearchClientSettings(new
    Uri(configuration["Elasticsearch:Uri"]))
    .ServerCertificateValidationCallback(CertificateValidations.AllowAll)
    .DefaultIndex(configuration["Elasticsearch:DefaultIndex"])
    .Authentication(new
    BasicAuthentication(configuration["Elasticsearch:Username"],
    configuration["Elasticsearch:Password"]))
    .DisableDirectStreaming();
    return new ElasticsearchClient(settings);
});


// Register PostgreSQL Enum Mapping
NpgsqlConnection.GlobalTypeMapper.MapEnum<task_status>("task_status");

// Register Database Connection
builder.Services.AddTransient<NpgsqlConnection>(_ =>
    new NpgsqlConnection(builder.Configuration.GetConnectionString("pgconn")));

// Add Controllers & Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false, // Set to true if you want to enforce issuer validation
        ValidateAudience = false, // Set to true if you want to enforce audience validation
        ValidateLifetime = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "TaskTrackProSecureKeyWithMinimum32Chars"))
    };
    // Add SignalR token handling
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var token = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(token) && path.StartsWithSegments("/notificationHub"))
            {
                context.Token = token;
            }
            return Task.CompletedTask;
        }
    };
});

// Configure Swagger with JWT authentication
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TaskTrackPro API",
        Version = "v1",
        Description = "Advanced Task Management System API"
    });
    c.AddSecurityDefinition("token", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer",
        In = ParameterLocation.Header,
        Name = HeaderNames.Authorization,
        Description = "Please enter a valid token"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "token"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Add CORS with specific origins
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecific", policy =>
    {
        policy.WithOrigins("http://localhost:5205") // MVC frontend
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // Required for SignalR with auth
    });
});

var app = builder.Build();

// Middleware Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseCors("AllowSpecific");

app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();

// Map SignalR Hub
app.MapHub<NotificationHub>("/notificationHub");
app.MapHub<ChatHub>("/chatHub");

app.MapControllers();
async Task IndexDataOnStartup()
{
    using var scope = app.Services.CreateScope();
    var taskRepo = scope.ServiceProvider.GetRequiredService<ITaskInterface>(); // Your service to fetch tasks
    var esService = scope.ServiceProvider.GetRequiredService<ElasticsearchService>(); // Service to interact with Elasticsearch

    try
    {
        // Create the index in Elasticsearch if it doesn't exist already
        await esService.CreateIndexAsync();

        // Fetch tasks from your database
        var tasks = await taskRepo.GetAllTasks(); // Assuming you have a method to get all tasks

        if (tasks.Count() > 0)
        {
            // Iterate over each task and index it in Elasticsearch
            foreach (var task in tasks)
            {
                await esService.IndexTaskAsync(task); // Index individual task in Elasticsearch
            }

        }
        else
        {
            Console.WriteLine("No tasks found in the database.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error indexing tasks: {ex.Message}");
    }
}
// await IndexDataOnStartup();

app.Run();

app.Use(async (context, next) =>
{
    var user = context.User;
    string? userIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    int userId = int.TryParse(userIdClaim, out int id) ? id : 0;

    if (userId > 0)
    {
        using var scope = app.Services.CreateScope();
        var consumerService = scope.ServiceProvider.GetRequiredService<RabbitMQConsumerService>();
        consumerService.StartConsumerForUser(userId);
    }

    await next();
});
