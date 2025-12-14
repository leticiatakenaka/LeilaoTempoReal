using LeilaoTempoReal.Application.Common;
using LeilaoTempoReal.Application.Services;
using LeilaoTempoReal.Dominio.Entidades;
using LeilaoTempoReal.Dominio.Interfaces;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace LeilaoTempoReal.Tests;

public class LeilaoServiceTests
{
    private readonly Mock<ILeilaoRepository> _repoMock;
    private readonly Mock<IDatabase> _databaseMock;
    private readonly Mock<INotificador> _notificadorMock;
    private readonly Mock<ILanceChannel> _channelMock;
    private readonly Mock<IConnectionMultiplexer> _connectionMock;
    private readonly LeilaoService _service;

    public LeilaoServiceTests()
    {
        _repoMock = new Mock<ILeilaoRepository>();
        _databaseMock = new Mock<IDatabase>();
        _notificadorMock = new Mock<INotificador>();
        _channelMock = new Mock<ILanceChannel>();
        _connectionMock = new Mock<IConnectionMultiplexer>();

        _connectionMock.Setup(c => c.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                       .Returns(_databaseMock.Object);

        _service = new LeilaoService(
            _notificadorMock.Object,
            _repoMock.Object,
            _connectionMock.Object,
            _channelMock.Object
        );
    }

    [Fact]
    public async Task DarLanceAsync_DeveRetornarSucesso_QuandoLanceForAceitoPeloRedis()
    {
        var leilaoId = Guid.NewGuid();
        var leilao = new Leilao("PS5", 50m, DateTime.Now.AddDays(1));

        _repoMock.Setup(r => r.ObterPorIdAsync(leilaoId)).ReturnsAsync(leilao);

        _databaseMock.Setup(d => d.ScriptEvaluateAsync(
            It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisResult.Create(1));

        var resultado = await _service.DarLanceAsync(leilaoId, 100m, "usuario_teste");

        Assert.True(resultado.IsSuccess);

        _notificadorMock.Verify(n => n.NotificarNovoLance(leilaoId, "usuario_teste", 100m), Times.Once);
        _channelMock.Verify(c => c.TentarEscreverLance(leilaoId, 100m, "usuario_teste"), Times.Once);
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
    }

    [Fact]
    public async Task DarLanceAsync_DeveFalhar_QuandoLeilaoJaEstiverEncerrado()
    {
        var leilaoId = Guid.NewGuid();

        var leilaoVencido = new Leilao("PS5", 50m, DateTime.Now.AddDays(1));

        var propDataFim = typeof(Leilao).GetProperty(nameof(Leilao.DataFim));
        propDataFim.SetValue(leilaoVencido, DateTime.Now.AddDays(-1));

        _repoMock.Setup(r => r.ObterPorIdAsync(leilaoId)).ReturnsAsync(leilaoVencido);

        var resultado = await _service.DarLanceAsync(leilaoId, 100m, "usuario_teste");

        Assert.False(resultado.IsSuccess);
        Assert.Equal("O leilão já encerrou.", resultado.Message);

        _databaseMock.Verify(d => d.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()), Times.Never);
    }

    [Fact]
    public async Task DarLanceAsync_DeveFalhar_QuandoValorForBaixoRejeitadoPeloRedis()
    {
        var leilaoId = Guid.NewGuid();
        var leilao = new Leilao("PS5", 50m, DateTime.Now.AddDays(1));
        _repoMock.Setup(r => r.ObterPorIdAsync(leilaoId)).ReturnsAsync(leilao);

        _databaseMock.Setup(d => d.ScriptEvaluateAsync(
            It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisResult.Create(2));

        var resultado = await _service.DarLanceAsync(leilaoId, 100m, "usuario_teste");

        Assert.False(resultado.IsSuccess);
        Assert.Contains("Lance rejeitado", resultado.Message);

        _notificadorMock.Verify(n => n.NotificarNovoLance(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<decimal>()), Times.Never);
        _channelMock.Verify(c => c.TentarEscreverLance(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>()), Times.Never);
    }
}