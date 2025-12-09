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

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.MapHub<LeilaoHub>("/hub/leilao");

app.Run();
