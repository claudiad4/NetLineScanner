using NetLine.Application.Models.Scanning;
using NetLine.Domain.Entities;

namespace NetLine.Application.Interfaces.Scanning;

/// <summary>
/// Read-only query for the per-component scan schedule of a device.
/// Returns, for each registered monitoring component, the time of the next scheduled run.
/// </summary>
public interface IScanScheduleQuery
{
    IReadOnlyList<DeviceScanSchedule> GetSchedule(DeviceInfo device, DateTime utcNow);
}
