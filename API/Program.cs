using System.Text;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using Npgsql;
using Repositories;
using Npgsql;
using Elastic.Clients.Elasticsearch;
using tmp;
using Elastic.Transport;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<ITaskInterface, TaskRepository>();
builder.Services.AddSingleton<ElasticsearchService>();
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
builder.Services.AddScoped<ITaskInterface, TaskRepository>();
builder.Services.AddSingleton<NpgsqlConnection>((ServiceProvider) =>
{
    var connectionString = ServiceProvider.GetRequiredService<IConfiguration>().GetConnectionString("pgconn");
    return new NpgsqlConnection(connectionString);
});
// Add CORS services
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder =>
        {
            builder.AllowAnyOrigin() // Allow requests from any origin
                   .AllowAnyMethod() // Allow all HTTP methods (GET, POST, PUT, DELETE, etc.)
                   .AllowAnyHeader(); // Allow all headers
        });
});
var app = builder.Build();
// Use CORS middleware
app.UseCors("AllowAllOrigins");
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.MapControllers();
app.UseHttpsRedirection();

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

// Run the indexing process on startup
await IndexDataOnStartup();



app.Run();