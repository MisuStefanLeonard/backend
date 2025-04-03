using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ShoppingCartDtos;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.VouchereDtos;
using E_Commerce_BackEnd.Models.ProductRelatedModels.JSON_Models;

namespace E_Commerce_BackEnd.Models.DTO.AdminRelatedDtos.ProductBillingDto;

public class OrderDetailsForBillingDto
{
    public int OrderId { get; set; }
    public DateTime OrderDate { get; set; }
    public string OrderName { get; set; } = null!;
    public string OrderPrename { get; set; } = null!;
    public string OrderPhoneNumber { get; set; } = null!;
    public string OrderEmail { get; set; } = null!;
    public NumarFactura? OrderBillNumber { get; set; } = null!;
    public IList<GroupedCartItems> Items { get; init; } = new List<GroupedCartItems>();
    public AdreseDto? ClientDeliveryAddress { get; set; } 
    public AdreseDto? ClientBillingAddress { get; set; } 
}