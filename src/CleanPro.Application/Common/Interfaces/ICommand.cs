namespace CleanPro.Application.Common.Interfaces;

public interface ICommand<TCommand, TResult>
    where TCommand : ICommand<TCommand, TResult>
{
}
