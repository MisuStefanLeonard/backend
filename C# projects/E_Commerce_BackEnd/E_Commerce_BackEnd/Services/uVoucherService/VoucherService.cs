using AutoMapper;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.BulkOperationsDto;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.VouchereDtos;
using E_Commerce_BackEnd.Models.ProductVouchersModels;
using E_Commerce_BackEnd.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.IdentityModel.Tokens;

namespace E_Commerce_BackEnd.Services.uVoucherService;

public class VoucherService : IVoucherService
{
    
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<VoucherService> _logger;
    private readonly IMapper _mapper;

    public VoucherService(IUnitOfWork unitOfWork, ILogger<VoucherService> logger, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<IList<VouchereDisplayDto>?> GetAllVouchers()
    {
        var vouchersRepository = _unitOfWork.Repository<Vouchere>();
        
        var listOfVouchers =  await vouchersRepository.GetAllAsync();

        var toDtoList = _mapper.Map<IList<VouchereDisplayDto>>(listOfVouchers);
       
        return toDtoList;
    }

    public async Task<IList<string>?> GetAllVoucherCodes()
    {
        var vouchersRepository = _unitOfWork.Repository<Vouchere>();

        var listOfVoucherNames = await vouchersRepository
            .GetSimpleQueryable()
            .Select(v => v.CodVoucher)
            .ToListAsync();

        return listOfVoucherNames;
    }

    public async Task<VouchereDto?> GetCurrentVoucher(int idVoucher)
    {
        var vouchersRepository = _unitOfWork.Repository<Vouchere>();

        var voucherToReturn = await vouchersRepository
            .FindQueryable(v => v.IdVoucher == idVoucher)
            .FirstOrDefaultAsync();

        if (voucherToReturn is null)
        {
            _logger.LogError("Voucher to be returned not found");
            return null;
        }

        var voucherDto = new VouchereDto
        {
            CodVoucherDto = voucherToReturn.CodVoucher,
            ReducereDto = voucherToReturn.Reducere * 100,
            DataExpirareDto = voucherToReturn.DataExpirare.ToLocalTime()
        };

        return voucherDto;
    }

    public async Task<int?> DeleteVoucher(int idVoucher)
    {
        IDbContextTransaction? deleteTransaction = null;
        var vouchereRepository = _unitOfWork.Repository<Vouchere>();
        try
        {
            deleteTransaction = await _unitOfWork.BeginTransactionAsync();
            var voucherToBeDeleted = await vouchereRepository
                .FindQueryable(tl => tl.IdVoucher == idVoucher)
                    .Include(v => v.VVoucherePeComenzi)
                .FirstOrDefaultAsync();

            if (voucherToBeDeleted is null)
            {
                throw new Exception("Voucher  to be deleted is not in the database");
            }

            if (voucherToBeDeleted.VVoucherePeComenzi.IsNullOrEmpty())
            {
                await vouchereRepository.DeleteAsync(voucherToBeDeleted);
            }
            else
            {
                voucherToBeDeleted.IsDeleted = true;
                await vouchereRepository.UpdateAsync(voucherToBeDeleted);
            }
            
            await _unitOfWork.CommitTransactionAsync(deleteTransaction);
            return 1;

        }
        catch (Exception e)
        {
            if (deleteTransaction is not null)
            {
                await _unitOfWork.RollBackTransactionAsync(deleteTransaction);
            }
            _logger.LogError(e.Message);
            return -1;
        }
    }

    public async Task<int> DeleteSelected(BulkOperationsDto deleteSelected)
    {
        var vouchereRepository = _unitOfWork.Repository<Vouchere>();

        IDbContextTransaction? deleteBulkTransaction = null;

        try
        {
            deleteBulkTransaction = await _unitOfWork.BeginTransactionAsync();
            List<Vouchere> vouchereToBeDeleted = [];
            List<Vouchere> vouchereToBeUpdatedToBeDeleted = [];

            foreach (var item in deleteSelected.SelectedItemsToDoBulkOperations!)
            {
                var voucherToBeDeleted = await vouchereRepository
                    .FindQueryable(v => v.IdVoucher == int.Parse(item.ToString()!))
                        .Include(v => v.VVoucherePeComenzi)
                    .FirstOrDefaultAsync();

                if (voucherToBeDeleted is not null)
                {
                    var hasOrdersOnVoucher = voucherToBeDeleted.VVoucherePeComenzi.IsNullOrEmpty();
                    if (hasOrdersOnVoucher)
                    {
                        vouchereToBeDeleted.Add(voucherToBeDeleted);
                    }
                    else
                    {
                        vouchereToBeUpdatedToBeDeleted.Add(voucherToBeDeleted);
                    }
                }
                else
                {
                    _logger.LogInformation("Tip galerie already has been deleted (bulk operation delete)");
                }

            }

            if (vouchereToBeUpdatedToBeDeleted.Count != 0)
            {
                await vouchereRepository.UpdateRangeAsync(vouchereToBeUpdatedToBeDeleted);
            }
            
            if (vouchereToBeDeleted.Count != 0)
            {
                await vouchereRepository.DeleteRangeAsync(vouchereToBeDeleted);
            }
            await _unitOfWork.CommitTransactionAsync(deleteBulkTransaction);
            return 1;
        }
        catch (Exception e)
        {
            if (deleteBulkTransaction is not null)
            {
                await _unitOfWork.RollBackTransactionAsync(deleteBulkTransaction);
            }
            _logger.LogError(e.Message);
            return -1;
        }
    }

    public async Task<int> ModifyOrAddVoucher(VouchereDto vouchereDto, int idVoucher, bool isAdded)
    {
        var vouchereRepository = _unitOfWork.Repository<Vouchere>();
        IDbContextTransaction? addOrUpdateTransaction = null;
        try
        {
            addOrUpdateTransaction = await _unitOfWork.BeginTransactionAsync();

            if (isAdded)
            {

                // Add new voucher
                var newVoucher = new Vouchere
                {
                    CodVoucher = vouchereDto.CodVoucherDto.ToUpper(),
                    Reducere = vouchereDto.ReducereDto / 100,
                    DataExpirare = vouchereDto.DataExpirareDto.Date,
                    IsDeleted = false
                };
                
                await vouchereRepository.AddAsync(newVoucher);
            }
            else
            {
                // Update existing voucher
                var voucherToBeModified = await vouchereRepository
                    .FindQueryable(v => v.IdVoucher == idVoucher)
                    .FirstOrDefaultAsync();

                if (voucherToBeModified == null)
                {
                    throw new NullReferenceException("voucher to be modified is null");
                }

                _mapper.Map(vouchereDto, voucherToBeModified);
                await vouchereRepository.UpdateAsync(voucherToBeModified);
            }

            // Commit the transaction if everything is successful
            await _unitOfWork.CommitTransactionAsync(addOrUpdateTransaction);
            return 1;
        }
        catch (Exception ex)
        {
            // Rollback the transaction and handle exceptions
            if (addOrUpdateTransaction != null)
            {
                await _unitOfWork.RollBackTransactionAsync(addOrUpdateTransaction);
            }

            _logger.LogError(ex.Message);

            return ex switch
            {
                NullReferenceException => -1,
                _ => -3
            };
        }
    }
}