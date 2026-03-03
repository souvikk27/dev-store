using Intellidevstore.Libs.Messaging.Command;
using Intellidevstore.Libs.Messaging.Query;
using Microsoft.Extensions.DependencyInjection;

namespace Intellidevstore.Libs.Messaging;

public static class MessagingExtension
{
    /// <summary>
    /// Registers the dispatcher and allows manual handler registration.
    /// Zero reflection approach - you manually register each handler.
    /// </summary>
    public static IServiceCollection AddLightweightCqrs(this IServiceCollection services)
    {
        services.AddScoped<IDispatcher, StronglyTypedDispatcher>();
        return services;
    }

    /// <summary>
    /// Register a query handler
    /// </summary>
    public static IServiceCollection AddQueryHandler<TQuery, TResponse, THandler>(
        this IServiceCollection services
    )
        where TQuery : IQuery<TResponse>
        where THandler : class, IQueryHandler<TQuery, TResponse>
    {
        services.AddScoped<IQueryHandler<TQuery, TResponse>, THandler>();
        return services;
    }

    /// <summary>
    /// Register a command handler that returns a response
    /// </summary>
    public static IServiceCollection AddCommandHandler<TCommand, TResponse, THandler>(
        this IServiceCollection services
    )
        where TCommand : ICommand<TResponse>
        where THandler : class, ICommandHandler<TCommand, TResponse>
    {
        services.AddScoped<ICommandHandler<TCommand, TResponse>, THandler>();
        return services;
    }

    /// <summary>
    /// Register a command handler that doesn't return a response
    /// </summary>
    public static IServiceCollection AddCommandHandler<TCommand, THandler>(
        this IServiceCollection services
    )
        where TCommand : ICommand
        where THandler : class, ICommandHandler<TCommand>
    {
        services.AddScoped<ICommandHandler<TCommand>, THandler>();
        return services;
    }

}