using System.Collections.Concurrent;

namespace Api.Agente.Infrastructure.Jobs;

public sealed class ExecutionCancellationRegistry
{
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _running = new();

    public ExecutionCancellationScope Register(Guid executionId, CancellationToken stoppingToken)
    {
        var source = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        _running[executionId] = source;
        return new ExecutionCancellationScope(this, executionId, source);
    }

    public bool TryCancel(Guid executionId)
    {
        if (!_running.TryGetValue(executionId, out var source))
        {
            return false;
        }

        try
        {
            source.Cancel();
            return true;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }

    private void Unregister(Guid executionId, CancellationTokenSource source)
    {
        _running.TryRemove(new KeyValuePair<Guid, CancellationTokenSource>(executionId, source));
        source.Dispose();
    }

    public sealed class ExecutionCancellationScope(
        ExecutionCancellationRegistry registry,
        Guid executionId,
        CancellationTokenSource source) : IDisposable
    {
        public CancellationToken Token => source.Token;

        public void Dispose() => registry.Unregister(executionId, source);
    }
}
