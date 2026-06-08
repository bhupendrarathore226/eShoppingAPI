using AutoMapper;
using EventBus.Messages.Events;
using MassTransit;
using MediatR;
using Ordering.Application.Commands;

namespace Ordering.API.EventBusConsumer;

public class BasketOrderingConsumerV2 : IConsumer<BasketCheckoutEventV2>
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly ILogger<BasketOrderingConsumerV2> _logger;

    public BasketOrderingConsumerV2(IMediator mediator, IMapper mapper, ILogger<BasketOrderingConsumerV2> logger)
    {
        _mediator = mediator;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<BasketCheckoutEventV2> context)
    {
        using var scope = _logger.BeginScope("Consuming Basket Checkout Event V2 for {CorrelationId}",
            context.Message.CorrelationId);

        if (string.IsNullOrWhiteSpace(context.Message.UserName) || context.Message.TotalPrice is null or <= 0)
        {
            _logger.LogError(
                "Invalid BasketCheckoutEventV2 received — missing required fields. CorrelationId: {CorrelationId}. Discarding message.",
                context.Message.CorrelationId);
            return; // ACK and discard — deterministic failure, no retry
        }

        try
        {
            var command = _mapper.Map<CheckoutOrderCommand>(context.Message);
            PopulateAddressDetails(command);
            var result = await _mediator.Send(command);
            _logger.LogInformation("Basket checkout V2 event completed for {CorrelationId}", context.Message.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error consuming BasketCheckoutEventV2. CorrelationId: {CorrelationId}, UserName: {UserName}",
                context.Message.CorrelationId,
                context.Message.UserName);
            throw;
        }
    }

    private static void PopulateAddressDetails(CheckoutOrderCommand command)
    {
        command.FirstName = "Rahul";
        command.LastName = "Sahay";
        command.EmailAddress = "rahulsahay@eshop.net";
        command.AddressLine = "Bangalore";
        command.Country = "India";
        command.State = "KA";
        command.ZipCode = "560001";
        command.PaymentMethod = 1;
        command.CardName = "Visa";
        command.CardNumber = "1234567890123456";
        command.Expiration = "12/25";
        command.CVV = "123";
    }
}