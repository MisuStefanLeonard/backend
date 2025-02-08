namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.ShoppingCartDtos;

public class CartInfo
{
    public IList<GroupedCartItems> Items { get; init; } = new List<GroupedCartItems>();
    public IList<GroupedCartItems> ModifiedCartItems { get; set; } = new List<GroupedCartItems>();
    public IList<AdreseDto>? ClientsDeliveryAddresses { get; set; } = [];
    public IList<AdreseDto>? ClientsBillingAddresses { get; set; } = [];
   
    public UserPersonalInfo? UserOrderDetails { get; set; }
    public decimal PretTotal { get; init; }
    public decimal TotalProduse { get; init; }
    public bool IsLoggedIn { get; set; } = false;
}