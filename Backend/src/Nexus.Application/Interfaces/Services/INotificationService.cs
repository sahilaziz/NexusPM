namespace Nexus.Application.Interfaces.Services;

public interface INotificationService
{
    Task SendNotificationAsync(string userId, string message);
}
