namespace Nexus.Application.Interfaces.Hubs;

public interface IHubContext<T> where T : class
{
    Task SendAsync(string method, object data);
}

public class NotificationHub { }
