using E_Commerce_BackEnd.Models.DTO.AdminRelatedDtos.Accounts;
using E_Commerce_BackEnd.Models.DTO.AdminRelatedDtos.Dashboard;
using E_Commerce_BackEnd.Models.DTO.AdminRelatedDtos.Dashboard.GoogleAnalyticsDTO;
using E_Commerce_BackEnd.Models.DTO.AdminRelatedDtos.OrdersDto;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos;
using E_Commerce_BackEnd.Models.Enums;

namespace E_Commerce_BackEnd.Services.uAdminService;

public interface IAdminService
{
    public Task<int> AdminLogIn(string key);
    public Task<IList<ProduseDtoForAdminListing>?> GetProductsForDtoAdminListing();
    public Task<ProduseDtoForAdminModification?> GetProductForAdminPage(string codProdus);
    public Task<int> SaveAdminPersonalDataModification(ConturiDtoForModification updatedData);
    public Task<int> EmailChangedByAdmin(string emailData, string currenCacheKeyRequest);
    public Task<int> ModifyAddressState(string alias, bool addressState,TipAdrese tipAdresa ,int accountId);
    public Task<int> ModifyAddresses(ConturiDtoForModification updatedAddreses);
    public Task<IList<MainOrdersDisplayDto>> GetAllOrders();
    public Task<DashboardGeneralData> GetMainDashboardData(DateTime? lowerInterval , DateTime? upperInterval);
    public Task<GaDashboardDto> GetGoogleAnalyticsData(string? lowerInterval , string? upperInterval);
}