using LeilaoTempoReal.API.Hubs;
using LeilaoTempoReal.API.Services;
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
builder.Services.AddScoped<LeilaoService>();
builder.Services.AddScoped<INotificador, NotificadorSignalR>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.MapHub<LeilaoHub>("/hub/leilao");

app.Run();
