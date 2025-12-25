namespace CleanPro.Application.Common.Interfaces;

public interface IQuery<TQuery, TResult>
    where TQuery : IQuery<TQuery, TResult>
{
}
