using System.Collections.Concurrent;

namespace Api.Agente.Host.Endpoints;

internal sealed class SofiaA2aTaskStore
{
    private readonly ConcurrentDictionary<string, object> _tasks = new(StringComparer.Ordinal);

    public void Put(string taskId, object task) => _tasks[taskId] = task;

    public bool TryGet(string taskId, out object? task)
    {
        if (_tasks.TryGetValue(taskId, out var value))
        {
            task = value;
            return true;
        }

        task = null;
        return false;
    }

    public bool TryRemove(string taskId, out object? task)
    {
        return _tasks.TryRemove(taskId, out task);
    }
}
