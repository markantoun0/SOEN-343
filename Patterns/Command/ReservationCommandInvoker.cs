namespace SUMMS.Api.Patterns.Command;

public class ReservationCommandInvoker
{
    public Task<TResult> ExecuteAsync<TResult>(
        ICommand<TResult> command,
        CancellationToken cancellationToken = default)
    {
        return command.ExecuteAsync(cancellationToken);
    }
}
