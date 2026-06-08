using EventBus.Messages.Events;
using MassTransit;

namespace Ordering.API.EventBusConsumer;

public class BasketOrderingConsumerV2Fault : IConsumer<Fault<BasketCheckoutEventV2>>
{
    private readonly ILogger<BasketOrderingConsumerV2Fault> _logger;

    public BasketOrderingConsumerV2Fault(ILogger<BasketOrderingConsumerV2Fault> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<Fault<BasketCheckoutEventV2>> context)
    {
        var exceptions = string.Join("; ", context.Message.Exceptions.Select(e => e.Message));
        _logger.LogError(
            "POISON PILL: BasketCheckoutEventV2 permanently failed after all retries. " +
            "CorrelationId: {CorrelationId}, UserName: {UserName}, Exceptions: {Exceptions}",
            context.Message.Message.CorrelationId,
            context.Message.Message.UserName,
            exceptions);

        return Task.CompletedTask;
    }
}
