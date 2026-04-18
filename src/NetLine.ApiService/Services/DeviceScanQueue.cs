using System.Threading.Channels;
using NetLine.Application.Interfaces.Scanning;

namespace NetLine.ApiService.Services;

public sealed class DeviceScanQueue : IDeviceScanQueue
{
    private readonly Channel<int> _channel = Channel.CreateUnbounded<int>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

    public ValueTask EnqueueAsync(int deviceId, CancellationToken cancellationToken = default)
        => _channel.Writer.WriteAsync(deviceId, cancellationToken);

    public IAsyncEnumerable<int> ReadAllAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAllAsync(cancellationToken);
}
