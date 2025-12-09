using System.Threading.Channels;

namespace LeilaoTempoReal.Application.Common;

public interface ILanceChannel
{
    bool TentarEscreverLance(Guid leilaoId, decimal valor, string usuario);
    IAsyncEnumerable<LanceQueuedItem> ReadAllAsync(CancellationToken ct);
}

public record LanceQueuedItem(Guid LeilaoId, decimal Valor, string Usuario);

public class LanceChannel : ILanceChannel
{
    private readonly Channel<LanceQueuedItem> _channel = Channel.CreateUnbounded<LanceQueuedItem>();

    public bool TentarEscreverLance(Guid leilaoId, decimal valor, string usuario)
    {
        return _channel.Writer.TryWrite(new LanceQueuedItem(leilaoId, valor, usuario));
    }

    public IAsyncEnumerable<LanceQueuedItem> ReadAllAsync(CancellationToken ct)
    {
        return _channel.Reader.ReadAllAsync(ct);
    }
}