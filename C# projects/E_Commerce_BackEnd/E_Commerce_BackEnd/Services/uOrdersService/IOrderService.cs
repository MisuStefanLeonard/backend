using E_Commerce_BackEnd.Models.DTO.AdminRelatedDtos.OrdersDto;
using E_Commerce_BackEnd.Models.DTO.ClientOrdersDto;

namespace E_Commerce_BackEnd.Services.uOrdersService;

public interface IOrderService
{
    public Task<IList<ClientOrder>> GetClientOrders(int accountId,string currency = "RON");
    public Task<KeyValuePair<int , string>> PlaceOrder(int? accountId, Guid sessionId, PlaceOrderDto orderToBePlaced,string currency = "RON");
    public Task<int> ConfirmPage(string confirmationId, int orderId);
    public Task<KeyValuePair<int,Stream?>> ReturnPdfBill(int orderId, string currency = "RON");
    public Task<int> SendBillOnMail(int orderId, string email, string currency = "RON");
}