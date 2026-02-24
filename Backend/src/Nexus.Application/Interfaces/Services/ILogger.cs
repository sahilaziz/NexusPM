namespace Nexus.Application.Interfaces.Services;

public interface ILogger<T>
{
    void LogInformation(string message);
    void LogWarning(string message);
    void LogError(string message, Exception? exception = null);
}
