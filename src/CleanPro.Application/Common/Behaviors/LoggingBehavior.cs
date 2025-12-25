using System.Diagnostics;
using CleanPro.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace CleanPro.Application.Common.Behaviors;

public sealed class LoggingBehavior<TCommand, TResult> : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TCommand, TResult>
{
    private readonly ICommandHandler<TCommand, TResult> _next;
    private readonly ILogger<LoggingBehavior<TCommand, TResult>> _logger;

    public LoggingBehavior(
        ICommandHandler<TCommand, TResult> next,
        ILogger<LoggingBehavior<TCommand, TResult>> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        var commandName = typeof(TCommand).Name;

        _logger.LogInformation("Handling command {CommandName}", commandName);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await _next.HandleAsync(command, cancellationToken);

            stopwatch.Stop();

            _logger.LogInformation(
                "Command {CommandName} handled successfully in {ElapsedMilliseconds}ms",
                commandName,
                stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "Command {CommandName} failed after {ElapsedMilliseconds}ms",
                commandName,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}

public sealed class QueryLoggingBehavior<TQuery, TResult> : IQueryHandler<TQuery, TResult>
    where TQuery : IQuery<TQuery, TResult>
{
    private readonly IQueryHandler<TQuery, TResult> _next;
    private readonly ILogger<QueryLoggingBehavior<TQuery, TResult>> _logger;

    public QueryLoggingBehavior(
        IQueryHandler<TQuery, TResult> next,
        ILogger<QueryLoggingBehavior<TQuery, TResult>> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default)
    {
        var queryName = typeof(TQuery).Name;

        _logger.LogInformation("Handling query {QueryName}", queryName);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await _next.HandleAsync(query, cancellationToken);

            stopwatch.Stop();

            _logger.LogInformation(
                "Query {QueryName} handled successfully in {ElapsedMilliseconds}ms",
                queryName,
                stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "Query {QueryName} failed after {ElapsedMilliseconds}ms",
                queryName,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}
