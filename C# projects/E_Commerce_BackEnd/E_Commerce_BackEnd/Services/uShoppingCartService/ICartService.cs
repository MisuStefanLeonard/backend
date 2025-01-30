using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ShoppingCartDtos;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.VouchereDtos;

namespace E_Commerce_BackEnd.Services.uShoppingCartService;

public interface ICartService
{
    public Task<KeyValuePair<int, string>> AddOrUpdateCart(Guid sessionId,int? idCont , SetOnCartDto? setOnCartItem , ProductOnCartDto? productOnCart);
    public Task<int> DeleteFromCart(UpdateQuantityInfo productInfo);
    public Task<Tuple<int,CartInfo?>> CheckCartAtCheckout(int? idCont, Guid sessionId ,  DateTime dateTimeSinceCookieWasAdded,DateTime timeToUpdate,string currency = "RON" );
    public Task<CartInfo> GetCartItems(int? idCont , Guid sessionId , string currency = "RON");
    public Task<int> ModifyQuantity(UpdateQuantityInfo productInfo);
    public Task<KeyValuePair<int, VouchereDto?>> GetVoucherInfo(int? idCont  ,CheckVoucher voucherData);
  
}