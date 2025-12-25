namespace CleanPro.Application.Common.Interfaces;

public interface IQueryHandler<TQuery, TResult>
    where TQuery : IQuery<TQuery, TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}
