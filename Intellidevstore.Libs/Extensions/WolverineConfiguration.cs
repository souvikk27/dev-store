using Wolverine;
using Wolverine.ErrorHandling;
using Wolverine.RabbitMQ;

namespace Intellidevstore.Libs.Extensions;

public static class WolverineConfiguration
{
    public static WolverineOptions ConfigureWolverine(
        this WolverineOptions options,
        string rabbitMqConnectionString
    )
    {
        // Configure RabbitMQ transport with connection URI
        // Format: amqp://user:password@host:port/vhost
        var rabbitMqUri = new Uri(rabbitMqConnectionString);

        options.UseRabbitMq(rabbitMqUri).AutoProvision().UseConventionalRouting();

        // Configure message handler discovery - scan this assembly for handlers
        options.Discovery.IncludeAssembly(typeof(WolverineConfiguration).Assembly);
        // Configure policies for durability
        options.Policies.UseDurableLocalQueues();
        options.Policies.UseDurableOutboxOnAllSendingEndpoints();

        // Configure retry policies with exponential backoff
        options
            .OnException<Exception>()
            .RetryWithCooldown(
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(30)
            )
            .Then.MoveToErrorQueue();

        return options;
    }
}
