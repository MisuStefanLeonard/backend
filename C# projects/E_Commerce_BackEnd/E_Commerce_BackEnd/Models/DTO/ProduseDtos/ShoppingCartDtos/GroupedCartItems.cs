namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.ShoppingCartDtos;

public class GroupedCartItems
{
    public string Key { get; init; } = null!;
    public IList<CartItems> CartItems { get; init; } = new List<CartItems>();
    
}