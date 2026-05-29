namespace JobSearch.Application.Automation;

public interface IJobImportService
{
    Task ImportAsync(CancellationToken cancellationToken = default);
}
