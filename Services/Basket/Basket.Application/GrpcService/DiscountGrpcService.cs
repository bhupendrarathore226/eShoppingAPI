using Discount.Grpc.Protos;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Basket.Application.GrpcService;

public class DiscountGrpcService
{
    private readonly DiscountProtoService.DiscountProtoServiceClient _discountProtoServiceClient;
    private readonly ILogger<DiscountGrpcService> _logger;

    public DiscountGrpcService(
        DiscountProtoService.DiscountProtoServiceClient discountProtoServiceClient,
        ILogger<DiscountGrpcService> logger)
    {
        _discountProtoServiceClient = discountProtoServiceClient;
        _logger = logger;
    }

    public async Task<CouponModel> GetDiscount(string productName)
    {
        var discountRequest = new GetDiscountRequest { ProductName = productName };
        try
        {
            return await _discountProtoServiceClient.GetDiscountAsync(discountRequest);
        }
        catch (RpcException ex)
        {
            _logger.LogWarning(ex,
                "Discount service unavailable for {ProductName}. Applying zero discount.",
                productName);
            return new CouponModel { ProductName = productName, Amount = 0, Description = "No Discount" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error calling discount service for {ProductName}. Applying zero discount.",
                productName);
            return new CouponModel { ProductName = productName, Amount = 0, Description = "No Discount" };
        }
    }
}