using EventBus.Messages.Events;
using MassTransit;

namespace Ordering.API.EventBusConsumer;

public class BasketOrderingConsumerFault : IConsumer<Fault<BasketCheckoutEvent>>
{
    private readonly ILogger<BasketOrderingConsumerFault> _logger;

    public BasketOrderingConsumerFault(ILogger<BasketOrderingConsumerFault> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<Fault<BasketCheckoutEvent>> context)
    {
        var exceptions = string.Join("; ", context.Message.Exceptions.Select(e => e.Message));
        _logger.LogError(
            "POISON PILL: BasketCheckoutEvent permanently failed after all retries. " +
            "CorrelationId: {CorrelationId}, UserName: {UserName}, Exceptions: {Exceptions}",
            context.Message.Message.CorrelationId,
            context.Message.Message.UserName,
            exceptions);

        return Task.CompletedTask;
    }
}
