namespace Intellidevstore.Libs.Shared.Messages;

/// <summary>
/// Dispatcher for sending commands and queries to their handlers.
/// Zero reflection - handlers are resolved directly from DI container.
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
    Task<TResponse> Send<TCommand, TResponse>(
        TCommand command,
        CancellationToken cancellationToken = default
    )
        where TCommand : ICommand<TResponse>;

    /// <summary>
    /// Send a command without expecting a response
    /// </summary>
    Task Send(ICommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// Strongly-typed dispatcher that avoids reflection entirely.
/// Handlers are resolved using compile-time generic parameters.
/// Uses dynamic dispatch to resolve the correct generic method at runtime
/// without using reflection APIs.
/// </summary>
public sealed class StronglyTypedDispatcher : IDispatcher
{
    private readonly IServiceProvider _serviceProvider;

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

        return SendQuery((dynamic)query, cancellationToken);
    }

    public Task<TResponse> Send<TCommand, TResponse>(
        TCommand command,
        CancellationToken cancellationToken = default
    )
        where TCommand : ICommand<TResponse>
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        return SendCommand<TCommand, TResponse>(command, cancellationToken);
    }

    public Task Send(ICommand command, CancellationToken cancellationToken = default)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        return SendCommand((dynamic)command, cancellationToken);
    }

    // Private generic methods that get resolved at compile-time through dynamic dispatch
    private Task<TResponse> SendQuery<TQuery, TResponse>(
        TQuery query,
        CancellationToken cancellationToken
    )
        where TQuery : IQuery<TResponse>
    {
        var handler =
            _serviceProvider.GetService(typeof(IQueryHandler<TQuery, TResponse>))
            as IQueryHandler<TQuery, TResponse>;
        if (handler == null)
        {
            throw new InvalidOperationException(
                $"No handler registered for query type {typeof(TQuery).Name}"
            );
        }

        return handler.Handle(query, cancellationToken);
    }

    private Task<TResponse> SendCommand<TCommand, TResponse>(
        TCommand command,
        CancellationToken cancellationToken
    )
        where TCommand : ICommand<TResponse>
    {
        var handler =
            _serviceProvider.GetService(typeof(ICommandHandler<TCommand, TResponse>))
            as ICommandHandler<TCommand, TResponse>;
        if (handler == null)
        {
            throw new InvalidOperationException(
                $"No handler registered for command type {typeof(TCommand).Name}"
            );
        }

        return handler.Handle(command, cancellationToken);
    }

    private Task SendCommand<TCommand>(TCommand command, CancellationToken cancellationToken)
        where TCommand : ICommand
    {
        var handler =
            _serviceProvider.GetService(typeof(ICommandHandler<TCommand>))
            as ICommandHandler<TCommand>;
        if (handler == null)
        {
            throw new InvalidOperationException(
                $"No handler registered for command type {typeof(TCommand).Name}"
            );
        }

        return handler.Handle(command, cancellationToken);
    }
}
