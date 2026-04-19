namespace NetLine.Infrastructure.Services.Monitoring.Syslog;

public interface ISyslogBuffer
{
    void Append(string sourceIp, string line);

    IReadOnlyList<string> Snapshot(string sourceIp);
}
