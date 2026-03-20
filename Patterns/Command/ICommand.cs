namespace SUMMS.Api.Patterns.Command;

public interface ICommand<TResult>
{
    Task<TResult> ExecuteAsync(CancellationToken cancellationToken = default);
}
