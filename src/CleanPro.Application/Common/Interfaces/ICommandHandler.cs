namespace CleanPro.Application.Common.Interfaces;

public interface ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TCommand, TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
