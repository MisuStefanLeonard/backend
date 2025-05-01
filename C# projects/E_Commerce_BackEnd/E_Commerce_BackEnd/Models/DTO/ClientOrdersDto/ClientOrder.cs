using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ShoppingCartDtos;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.VouchereDtos;
using E_Commerce_BackEnd.Models.Enums;
using E_Commerce_BackEnd.Models.ProductRelatedModels.JSON_Models;
using Sqids;

namespace E_Commerce_BackEnd.Models.DTO.ClientOrdersDto;

public class ClientOrder
{
    public IList<GroupedCartItems> Items { get; init; } = new List<GroupedCartItems>();
    public AdreseDto? ClientDeliveryAddress { get; set; } 
    public AdreseDto? ClientBillingAddress { get; set; } 
    public UserPersonalInfo? UserOrderDetails { get; set; }
    public DateTime OrderDate { get; init; }
    public string EncodedIdComandaDto => SqidsEncoder.Encode(OrderId);
    private static readonly SqidsEncoder<int> SqidsEncoder = new();
    public int OrderId { get; init; }
    public StatusComanda OrderStatus { get; init; }
    public TipPlata OrderPayment { get; init; }
    public string OrderTrackingString { get; init; } = null!;
    public NumarFactura OrderBillNumber { get; init; } = null!;
    public bool IsCancelableDto { get; set; }
    public VouchereDto? OrderVoucher { get; init; }
    public decimal PretTotal { get; set; }
    public decimal TotalProduse { get; set; }
  
}