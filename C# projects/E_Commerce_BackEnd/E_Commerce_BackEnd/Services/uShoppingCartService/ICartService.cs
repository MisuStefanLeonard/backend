using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ShoppingCartDtos;

namespace E_Commerce_BackEnd.Services.uShoppingCartService;

public interface ICartService
{
    public Task<KeyValuePair<int, string>> AddOrUpdateCart(int idCont , SetOnCartDto? setOnCartItem , ProductOnCartDto? productOnCart);
    public Task<int> DeleteFromCart(int idCont, SetOnCartDto? setOnCartItemOnDelete , ProductOnCartDto? productOnCartToDelete);
}