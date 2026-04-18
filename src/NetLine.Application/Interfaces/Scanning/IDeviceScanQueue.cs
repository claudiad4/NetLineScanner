namespace NetLine.Application.Interfaces.Scanning;

public interface IDeviceScanQueue
{
    ValueTask EnqueueAsync(int deviceId, CancellationToken cancellationToken = default);
    IAsyncEnumerable<int> ReadAllAsync(CancellationToken cancellationToken);
}
