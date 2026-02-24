namespace Nexus.Application.Interfaces.Services;

public interface ISearchService
{
    Task IndexDocumentAsync(object document);
}
