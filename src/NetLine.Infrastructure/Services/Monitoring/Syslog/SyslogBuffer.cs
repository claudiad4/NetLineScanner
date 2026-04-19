using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace NetLine.Infrastructure.Services.Monitoring.Syslog;

public sealed class SyslogBuffer : ISyslogBuffer
{
    private readonly int _maxLines;
    private readonly ConcurrentDictionary<string, Queue<string>> _byIp = new();

    public SyslogBuffer(IOptions<SyslogOptions> options)
    {
        var cap = options.Value.MaxLinesPerDevice;
        _maxLines = cap > 0 ? cap : 50;
    }

    public void Append(string sourceIp, string line)
    {
        var queue = _byIp.GetOrAdd(sourceIp, _ => new Queue<string>(_maxLines));
        lock (queue)
        {
            queue.Enqueue(line);
            while (queue.Count > _maxLines)
            {
                queue.Dequeue();
            }
        }
    }

    public IReadOnlyList<string> Snapshot(string sourceIp)
    {
        if (!_byIp.TryGetValue(sourceIp, out var queue))
        {
            return Array.Empty<string>();
        }

        lock (queue)
        {
            return queue.ToArray();
        }
    }
}
