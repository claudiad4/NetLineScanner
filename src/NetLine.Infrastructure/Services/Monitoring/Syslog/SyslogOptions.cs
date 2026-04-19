namespace NetLine.Infrastructure.Services.Monitoring.Syslog;

public sealed class SyslogOptions
{
    public int Port { get; set; } = 514;
    public string BindAddress { get; set; } = "0.0.0.0";
    public int MaxLinesPerDevice { get; set; } = 50;
}
