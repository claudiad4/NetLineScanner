using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NetLine.Infrastructure.Services.Monitoring.Syslog;

public sealed class SyslogReceiver : BackgroundService
{
    private readonly ISyslogBuffer _buffer;
    private readonly SyslogOptions _options;
    private readonly ILogger<SyslogReceiver> _logger;

    public SyslogReceiver(ISyslogBuffer buffer, IOptions<SyslogOptions> options, ILogger<SyslogReceiver> logger)
    {
        _buffer = buffer;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        UdpClient? udp;
        try
        {
            var bind = IPAddress.TryParse(_options.BindAddress, out var addr) ? addr : IPAddress.Any;
            udp = new UdpClient(new IPEndPoint(bind, _options.Port));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Syslog receiver could not bind {Bind}:{Port} — raw logs disabled", _options.BindAddress, _options.Port);
            return;
        }

        _logger.LogInformation("Syslog receiver listening on {Bind}:{Port}", _options.BindAddress, _options.Port);

        using (udp)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = await udp.ReceiveAsync(stoppingToken);
                    var line = Encoding.UTF8.GetString(result.Buffer).TrimEnd('\r', '\n', '\0');
                    var ip = result.RemoteEndPoint.Address.ToString();
                    _buffer.Append(ip, line);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Syslog receive error");
                }
            }
        }
    }
}
