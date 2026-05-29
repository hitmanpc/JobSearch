namespace JobSearch.Application.Services;

public interface IJobImportService
{
    Task ImportAsync(CancellationToken cancellationToken = default);
}
