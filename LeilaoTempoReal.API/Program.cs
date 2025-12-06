using LeilaoTempoReal.API.Hubs;
using LeilaoTempoReal.Infraestrutura.Dados;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<LeilaoDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("LeilaoDb")));

var redisConnection = ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis"));

builder.Services.AddSingleton<IConnectionMultiplexer>(redisConnection);
builder.Services.AddOpenApi();
builder.Services.AddSignalR();

var app = builder.Build();

app.MapGet("/teste-redis", async (IConnectionMultiplexer mux) => {
    var db = mux.GetDatabase();
    await db.StringSetAsync("teste", "Funcionou! O Redis está vivo.");
    var valor = await db.StringGetAsync("teste");
    return Results.Ok(valor.ToString());
});

app.MapHub<LeilaoHub>("/hub/leilao");

app.Run();
