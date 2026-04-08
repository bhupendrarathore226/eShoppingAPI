using System.Net;
using Common.Logging.Apim;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Ordering.Application.Commands;
using Ordering.Application.Queries;
using Ordering.Application.Responses;

namespace Ordering.API.Controllers;

public class OrderController : ApiController
{
    private readonly IMediator _mediator;
    private readonly ILogger<OrderController> _logger;
    private readonly IApimRequestContext _apimContext;

    public OrderController(
        IMediator mediator,
        ILogger<OrderController> logger,
        IApimRequestContext apimContext)
    {
        _mediator = mediator;
        _logger = logger;
        _apimContext = apimContext;
    }

    [HttpGet("{userName}", Name = "GetOrdersByUserName")]
    [ProducesResponseType(typeof(IEnumerable<OrderResponse>), (int) HttpStatusCode.OK)]
    [ProducesResponseType((int) HttpStatusCode.Forbidden)]
    public async Task<ActionResult<IEnumerable<OrderResponse>>> GetOrdersByUserName(string userName)
    {
        // Defence-in-depth: a user can only see their own orders.
        // APIM has already validated the JWT; X-User-Id is the confirmed subject.
        if (!string.IsNullOrEmpty(_apimContext.UserId)
            && !string.Equals(_apimContext.UserId, userName, StringComparison.OrdinalIgnoreCase)
            && !_apimContext.HasRole("admin"))
        {
            _logger.LogWarning(
                "GetOrdersByUserName forbidden: token UserId={UserId} tried to access orders for {TargetUser}",
                _apimContext.UserId, userName);
            return Forbid();
        }

        var query = new GetOrderListQuery(userName);
        var orders = await _mediator.Send(query);
        return Ok(orders);
    }
    //Just for testing locally as it will be processed in queue
    [HttpPost(Name = "CheckoutOrder")]
    [ProducesResponseType((int) HttpStatusCode.OK)]
    public async Task<ActionResult<int>> CheckoutOrder([FromBody] CheckoutOrderCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }
    
    [HttpPut(Name = "UpdateOrder")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesDefaultResponseType]
    public async Task<ActionResult> UpdateOrder([FromBody] UpdateOrderCommand command)
    {
        var result = await _mediator.Send(command);
        return NoContent();
    }
    
    [HttpDelete("{id}",Name = "DeleteOrder")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesDefaultResponseType]
    public async Task<ActionResult> DeleteOrder(int id)
    {
        var cmd = new DeleteOrderCommand() {Id = id};
        await _mediator.Send(cmd);
        return NoContent();
    }
    
}