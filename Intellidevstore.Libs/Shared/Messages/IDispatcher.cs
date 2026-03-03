using System.Collections.Concurrent;
using System.Reflection;

namespace Intellidevstore.Libs.Shared.Messages;

/// <summary>
/// Dispatcher for sending commands and queries to their handlers.
/// Uses compiled delegates for fast invocation.
/// </summary>
public interface IDispatcher
{
    /// <summary>
    /// Send a query and get a response
    /// </summary>
    Task<TResponse> Send<TResponse>(
        IQuery<TResponse> query,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Send a command and get a response
    /// </summary>
    Task<TResponse> Send<TResponse>(
        ICommand<TResponse> command,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Send a command without expecting a response
    /// </summary>
    Task Send(ICommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// Registry for command and query handlers.
/// Scans and registers all handlers at startup.
/// </summary>
public sealed class DispatcherRegistry
{
    private readonly ConcurrentDictionary<Type, Func<object, CancellationToken, object>> _handlers =
        new();

    public void RegisterCommandHandler<TCommand, TResponse>(
        Func<TCommand, CancellationToken, Task<TResponse>> handler
    )
        where TCommand : ICommand<TResponse>
    {
        _handlers[typeof(TCommand)] = (cmd, ct) => handler((TCommand)cmd, ct);
    }

    public void RegisterQueryHandler<TQuery, TResponse>(
        Func<TQuery, CancellationToken, Task<TResponse>> handler
    )
        where TQuery : IQuery<TResponse>
    {
        _handlers[typeof(TQuery)] = (query, ct) => handler((TQuery)query, ct);
    }

    public Func<object, CancellationToken, object>? GetHandler(Type type)
    {
        return _handlers.TryGetValue(type, out var handler) ? handler : null;
    }
}

/// <summary>
/// Strongly-typed dispatcher that uses compiled delegates for fast invocation.
/// </summary>
public sealed class StronglyTypedDispatcher : IDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private static readonly ConcurrentDictionary<
        Type,
        Func<IServiceProvider, object, CancellationToken, object>
    > _commandCache = new();
    private static readonly ConcurrentDictionary<
        Type,
        Func<IServiceProvider, object, CancellationToken, object>
    > _queryCache = new();

    public StronglyTypedDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider =
            serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public Task<TResponse> Send<TResponse>(
        IQuery<TResponse> query,
        CancellationToken cancellationToken = default
    )
    {
        if (query == null)
            throw new ArgumentNullException(nameof(query));

        var queryType = query.GetType();
        var handler = _queryCache.GetOrAdd(queryType, CreateQueryHandler<TResponse>);
        return (Task<TResponse>)handler(_serviceProvider, query, cancellationToken);
    }

    public Task<TResponse> Send<TResponse>(
        ICommand<TResponse> command,
        CancellationToken cancellationToken = default
    )
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        var commandType = command.GetType();
        var handler = _commandCache.GetOrAdd(commandType, CreateCommandHandler<TResponse>);
        return (Task<TResponse>)handler(_serviceProvider, command, cancellationToken);
    }

    public Task Send(ICommand command, CancellationToken cancellationToken = default)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        var commandType = command.GetType();
        var handler = _commandCache.GetOrAdd(commandType, CreateVoidCommandHandler);
        return (Task)handler(_serviceProvider, command, cancellationToken);
    }

    private static Func<
        IServiceProvider,
        object,
        CancellationToken,
        object
    > CreateQueryHandler<TResponse>(Type queryType)
    {
        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(queryType, typeof(TResponse));
        var handleMethod = handlerType.GetMethod("Handle")!;

        return (sp, query, ct) =>
        {
            var handler = sp.GetService(handlerType);
            if (handler == null)
                throw new InvalidOperationException(
                    $"No handler registered for query type {queryType.Name}"
                );

            return handleMethod.Invoke(handler, new[] { query, ct })!;
        };
    }

    private static Func<
        IServiceProvider,
        object,
        CancellationToken,
        object
    > CreateCommandHandler<TResponse>(Type commandType)
    {
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(
            commandType,
            typeof(TResponse)
        );
        var handleMethod = handlerType.GetMethod("Handle")!;

        return (sp, command, ct) =>
        {
            var handler = sp.GetService(handlerType);
            if (handler == null)
                throw new InvalidOperationException(
                    $"No handler registered for command type {commandType.Name}"
                );

            return handleMethod.Invoke(handler, new[] { command, ct })!;
        };
    }

    private static Func<
        IServiceProvider,
        object,
        CancellationToken,
        object
    > CreateVoidCommandHandler(Type commandType)
    {
        var handlerType = typeof(ICommandHandler<>).MakeGenericType(commandType);
        var handleMethod = handlerType.GetMethod("Handle")!;

        return (sp, command, ct) =>
        {
            var handler = sp.GetService(handlerType);
            if (handler == null)
                throw new InvalidOperationException(
                    $"No handler registered for command type {commandType.Name}"
                );

            return handleMethod.Invoke(handler, new[] { command, ct })!;
        };
    }
}
