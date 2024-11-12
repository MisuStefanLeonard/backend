using E_Commerce_BackEnd.Models.DTO.ProduseDtos.BulkOperationsDto;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.VouchereDtos;

namespace E_Commerce_BackEnd.Services.uVoucherService;

public interface IVoucherService
{
    public Task<IList<VouchereDisplayDto>?> GetAllVouchers();
    public Task<IList<string>?> GetAllVoucherCodes();
    public Task<VouchereDto?> GetCurrentVoucher(int idVoucher);
    public Task<int?> DeleteVoucher(int idVoucher); 
    public Task<int> DeleteSelected(BulkOperationsDto deleteSelected);
    public Task<int> ModifyOrAddVoucher(VouchereDto vouchereDto, int idVoucher , bool isAdded);
}