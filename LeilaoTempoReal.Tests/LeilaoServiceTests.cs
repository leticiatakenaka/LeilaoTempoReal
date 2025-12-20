using LeilaoTempoReal.Application.Common;
using LeilaoTempoReal.Application.Services;
using LeilaoTempoReal.Dominio.Entidades;
using LeilaoTempoReal.Dominio.Interfaces;
using MassTransit;
using Moq;
using StackExchange.Redis;

namespace LeilaoTempoReal.Tests;

public class LeilaoServiceTests
{
    private readonly Mock<ILeilaoRepository> _repoMock;
    private readonly Mock<IDatabase> _databaseMock;
    private readonly Mock<INotificador> _notificadorMock;
    private readonly Mock<IConnectionMultiplexer> _connectionMock;
    private readonly Mock<IPublishEndpoint> _publishMock;
    private readonly LeilaoService _service;

    public LeilaoServiceTests()
    {
        _repoMock = new Mock<ILeilaoRepository>();
        _databaseMock = new Mock<IDatabase>();
        _notificadorMock = new Mock<INotificador>();
        _connectionMock = new Mock<IConnectionMultiplexer>();
        _publishMock = new Mock<IPublishEndpoint>();

        _connectionMock.Setup(c => c.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                        .Returns(_databaseMock.Object);

        _service = new LeilaoService(
            _notificadorMock.Object,
            _repoMock.Object,
            _connectionMock.Object,
            _publishMock.Object
        );
    }

    [Fact]
    public async Task DarLanceAsync_DeveRetornarSucesso_QuandoLanceForAceitoPeloRedis()
    {
        var leilaoId = Guid.NewGuid();
        var leilao = new Leilao("PS5", 50m, DateTime.Now.AddDays(1));

        _repoMock.Setup(r => r.ObterPorIdAsync(leilaoId)).ReturnsAsync(leilao);

        var retornoRedisSimulado = RedisResult.Create(new RedisResult[]
        {
            RedisResult.Create(1),   
            RedisResult.Create(100)  
        });

        _databaseMock.Setup(d => d.ScriptEvaluateAsync(
            It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(retornoRedisSimulado); 

        var resultado = await _service.DarLanceAsync(leilaoId, 100m, "usuario_teste");

        Assert.True(resultado.IsSuccess);

        _notificadorMock.Verify(n => n.NotificarNovoLance(leilaoId, "usuario_teste", 100m), Times.Once);

        _publishMock.Verify(p => p.Publish(
            It.Is<LanceCriadoEvent>(e => e.LeilaoId == leilaoId && e.Valor == 100m && e.Usuario == "usuario_teste"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DarLanceAsync_DeveFalhar_QuandoLeilaoNaoExistir()
    {
        var leilaoId = Guid.NewGuid();

        _repoMock.Setup(r => r.ObterPorIdAsync(leilaoId)).ReturnsAsync((Leilao?)null);

        var resultado = await _service.DarLanceAsync(leilaoId, 100m, "usuario_teste");

        Assert.False(resultado.IsSuccess);
        Assert.Equal("Leilão não encontrado.", resultado.Message);

        _databaseMock.Verify(d => d.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()), Times.Never);
        _notificadorMock.Verify(n => n.NotificarNovoLance(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<decimal>()), Times.Never);

        _publishMock.Verify(p => p.Publish(It.IsAny<LanceCriadoEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DarLanceAsync_DeveFalhar_QuandoValorForBaixoRejeitadoPeloRedis()
    {
        var leilaoId = Guid.NewGuid();
        var leilao = new Leilao("Item", 100m, DateTime.Now.AddDays(1));

        _repoMock.Setup(r => r.ObterPorIdAsync(leilaoId)).ReturnsAsync(leilao);

        var retornoRedisSimulado = RedisResult.Create(new RedisResult[]
        {
        RedisResult.Create(2),   
        RedisResult.Create(150)  
        });

        _databaseMock
            .Setup(d => d.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(retornoRedisSimulado); 

        var resultado = await _service.DarLanceAsync(leilaoId, 100m, "usuario");

        Assert.False(resultado.IsSuccess);
        Assert.Contains("150", resultado.Message);
    }

    [Fact]
    public async Task FinalizarLeilaoAsync_DeveRetornarSucesso_EAtualizarRedis_QuandoLeilaoEstiverAtivo()
    {
        var leilaoId = Guid.NewGuid();
        var leilao = new Leilao("Item Teste", 100m, DateTime.Now.AddDays(1));
        leilao.ReceberLance(200m, "Comprador_Vip");

        _repoMock.Setup(r => r.ObterPorIdAsync(leilaoId))
            .ReturnsAsync(leilao);

        _databaseMock
            .Setup(d => d.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<bool>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        var result = await _service.FinalizarLeilaoAsync(leilaoId);

        Assert.True(result.IsSuccess);

        var invocacao = _databaseMock.Invocations
            .FirstOrDefault(i => i.Method.Name == nameof(IDatabase.StringSetAsync));

        Assert.NotNull(invocacao);

        var keyUsada = (RedisKey)invocacao.Arguments[0];
        var jsonUsado = (RedisValue)invocacao.Arguments[1];

        Assert.Equal($"leilao:{leilaoId}", keyUsada.ToString());

        var jsonString = jsonUsado.ToString();
        Assert.Contains("Finalizado", jsonString);
        Assert.Contains("true", jsonString);
        Assert.Contains("Comprador_Vip", jsonString);

        var invocacaoExpire = _databaseMock.Invocations
            .FirstOrDefault(i => i.Method.Name == nameof(IDatabase.KeyExpireAsync));

        Assert.NotNull(invocacaoExpire);
        var keyExpire = (RedisKey)invocacaoExpire.Arguments[0];
        Assert.Equal($"leilao:{leilaoId}", keyExpire.ToString());
    }

    [Fact]
    public async Task FinalizarLeilaoAsync_DeveFalhar_QuandoLeilaoNaoExistir()
    {
        var leilaoId = Guid.NewGuid();
        _repoMock.Setup(r => r.ObterPorIdAsync(leilaoId))
            .ReturnsAsync((Leilao?)null);

        var result = await _service.FinalizarLeilaoAsync(leilaoId);

        Assert.False(result.IsSuccess);
        Assert.Equal("Leilão não encontrado.", result.Message);

        _databaseMock.Verify(d => d.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), null, false, It.IsAny<When>(), It.IsAny<CommandFlags>()), Times.Never);
        _publishMock.Verify(p => p.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FinalizarLeilaoAsync_DeveFalhar_QuandoLeilaoJaEstiverFinalizado()
    {
        var leilaoId = Guid.NewGuid();
        var leilao = new Leilao("Item Velho", 100m, DateTime.Now.AddDays(1));
        leilao.Finalizar();

        _repoMock.Setup(r => r.ObterPorIdAsync(leilaoId)).ReturnsAsync(leilao);

        var result = await _service.FinalizarLeilaoAsync(leilaoId);

        Assert.False(result.IsSuccess);
        Assert.Equal("Leilão já foi finalizado.", result.Message);

        _databaseMock.Verify(d => d.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), null, false, It.IsAny<When>(), It.IsAny<CommandFlags>()), Times.Never);
        _publishMock.Verify(p => p.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}