using NetLine.Domain.Entities;

namespace NetLine.Web.Services;

/// <summary>
/// Service for managing alert notifications in the UI.
/// Handles toast notifications and alert state management.
/// </summary>
public class AlertNotificationService
{
    private List<AlertNotification> _notifications = new();
    
    public event Action? OnNotificationAdded;
    public event Action? OnNotificationRemoved;

    public IReadOnlyList<AlertNotification> GetNotifications() => _notifications.AsReadOnly();

    public void AddNotification(DeviceAlert alert)
    {
        var notification = new AlertNotification
        {
            Id = Guid.NewGuid(),
            Alert = alert,
            CreatedAt = DateTime.Now,
            IsRead = false
        };

        _notifications.Insert(0, notification);
        OnNotificationAdded?.Invoke();

        // Auto-remove after 6 seconds
        _ = AutoDismissAsync(notification.Id);
    }

    public void AddSuccess(string title, string message)
    {
        var notification = new AlertNotification
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.Now,
            IsRead = false,
            Title = title,
            Message = message,
            IconClass = "bi-check-circle-fill",
            BgClass = "bg-success",
            TextClass = "text-white"
        };

        _notifications.Insert(0, notification);
        OnNotificationAdded?.Invoke();

        _ = AutoDismissAsync(notification.Id);
    }

    public void RemoveNotification(Guid id)
    {
        _notifications.RemoveAll(n => n.Id == id);
        OnNotificationRemoved?.Invoke();
    }

    public void ClearAll()
    {
        _notifications.Clear();
        OnNotificationRemoved?.Invoke();
    }

    private async Task AutoDismissAsync(Guid id)
    {
        await Task.Delay(6000);
        RemoveNotification(id);
    }
}

public class AlertNotification
{
    public Guid Id { get; set; }
    public DeviceAlert? Alert { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }

    public string? Title { get; set; }
    public string? Message { get; set; }
    public string? IconClass { get; set; }
    public string? BgClass { get; set; }
    public string? TextClass { get; set; }

    public string GetIcon() => Alert is null
        ? IconClass ?? "bi-info-circle-fill"
        : Alert.Type switch
        {
            AlertType.WentOffline => "bi-x-circle-fill",
            AlertType.CameOnline => "bi-check-circle-fill",
            _ => "bi-info-circle-fill"
        };

    public string GetBgClass() => Alert is null
        ? BgClass ?? "bg-info"
        : Alert.Type switch
        {
            AlertType.WentOffline => "bg-danger",
            AlertType.CameOnline => "bg-success",
            _ => "bg-info"
        };

    public string GetTextClass() => Alert is null
        ? TextClass ?? "text-dark"
        : Alert.Type switch
        {
            AlertType.WentOffline => "text-white",
            AlertType.CameOnline => "text-white",
            _ => "text-dark"
        };
}