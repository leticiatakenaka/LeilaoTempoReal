using LeilaoTempoReal.API.BackgroundServices;
using LeilaoTempoReal.API.Hubs;
using LeilaoTempoReal.API.Services;
using LeilaoTempoReal.Application.Common;
using LeilaoTempoReal.Application.Services;
using LeilaoTempoReal.Dominio.Interfaces;
using LeilaoTempoReal.Infraestrutura.Dados;
using LeilaoTempoReal.Infraestrutura.Repositorios;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

ThreadPool.SetMinThreads(200, 200);

builder.Services.AddDbContext<LeilaoDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("LeilaoDb")));

builder.Services.AddScoped<ILeilaoRepository, LeilaoRepository>();

var redisConnection = ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis"));
builder.Services.AddSingleton<IConnectionMultiplexer>(redisConnection);
builder.Services.AddOpenApi();
builder.Services.AddSignalR();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<ILeilaoService, LeilaoService>(); 
builder.Services.AddScoped<INotificador, NotificadorSignalR>();
builder.Services.AddSingleton<ILanceChannel, LanceChannel>();
builder.Services.AddHostedService<PersistenciaLanceWorker>();
builder.Services.AddHostedService<LeilaoFinalizadoWorker>();
builder.Services.AddDbContext<LeilaoDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("LeilaoDb"),
        sqlServerOptionsAction: sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);
        });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontEndAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200") 
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); 
    });
});

var app = builder.Build();

app.UseCors("FrontEndAngular");
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.MapHub<LeilaoHub>("/hub/leilao");

app.Run();
