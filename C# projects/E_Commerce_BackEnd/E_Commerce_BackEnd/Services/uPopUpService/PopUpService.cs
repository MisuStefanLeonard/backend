using E_Commerce_BackEnd.Models.ConfigurationModels;
using E_Commerce_BackEnd.Models.DTO.AdminRelatedDtos.PopUps;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.VouchereDtos;
using E_Commerce_BackEnd.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;

namespace E_Commerce_BackEnd.Services.uPopUpService;

public class PopUpService : IPopUpService
{
    private const string PopUpsCacheKey = "popups_cache";

    private readonly ILogger<PopUpService> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMemoryCache _cache;

    public PopUpService(ILogger<PopUpService> logger, IUnitOfWork unitOfWork, IMemoryCache cache)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<IList<PopUpListDto>?> GetAllPopUps()
    {
        try
        {
            var popUps = await _cache.GetOrCreateAsync(PopUpsCacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                var repository = _unitOfWork.Repository<PopUps>();
                return await repository
                    .GetSimpleQueryable()
                    .Select(p => new PopUpListDto
                    {
                        IdPopUp = p.IdPopUp,
                        DescriereJson = p.DescriereJson,
                        TitluJson = p.TitluJson,
                        Voucher = p.IdVoucher != null ? new PopUpVoucher
                            {
                                IdVoucher = p.Voucher!.IdVoucher,
                                CodVoucherDto = p.Voucher.CodVoucher,
                                ReducereDto = p.Voucher.Reducere * 100
                            }
                            : null,
                        IsActive = p.IsActive
                    })
                    .ToListAsync();
            });
            
            if (popUps.IsNullOrEmpty())
            {
                _logger.LogWarning("EMPTY CACHE ON POPUPS");
            }

            return popUps;
        }
        catch (Exception e)
        {
            _logger.LogError("Error occurred when fetching pop-ups from db");
            _logger.LogError(e.Message);
            return null;
        }
    }

    public async Task<KeyValuePair<int,int>> CreatePopUp(PopUpDto popUpDto)
    {
        IDbContextTransaction? transaction = null;
        try
        {
            _logger.LogInformation("Creating pop up..");
            transaction = await _unitOfWork.BeginTransactionAsync();
            var popUp = new PopUps
            {
                DescriereJson = popUpDto.DescriereJson,
                TitluJson = popUpDto.TitluJson,
                IdVoucher = popUpDto.IdVoucher,
                Voucher = null,
                IsActive = popUpDto.IsActive
            };
            await _unitOfWork.Repository<PopUps>().AddAsync(popUp);
            await _unitOfWork.CommitTransactionAsync(transaction);

            // Refresh cache immediately
            await RefreshCacheAsync();

            return new KeyValuePair<int, int>(1 , popUp.IdPopUp);
        }
        catch (Exception e)
        {
            if (transaction != null)
                await _unitOfWork.RollBackTransactionAsync(transaction);

            _logger.LogError("Error occurred when creating pop-up");
            _logger.LogError(e.Message);
            _logger.LogError(e.StackTrace);
            return new KeyValuePair<int, int>(-1 ,-1);
        }
    }

    public async Task<int> UpdatePopUp(PopUpDto popUpDto)
    {
        IDbContextTransaction? transaction = null;
        try
        {
            _logger.LogInformation("Updating pop up..");
            transaction = await _unitOfWork.BeginTransactionAsync();
            
            var findPopUpToModify = await _unitOfWork.Repository<PopUps>()
                .FindQueryable(p => p.IdPopUp == popUpDto.IdPopUp).FirstAsync();
            
            findPopUpToModify.IsActive = popUpDto.IsActive;
            findPopUpToModify.DescriereJson = popUpDto.DescriereJson;
            findPopUpToModify.TitluJson = popUpDto.TitluJson;
            findPopUpToModify.IdVoucher = popUpDto.IdVoucher;
            
            await _unitOfWork.Repository<PopUps>().UpdateAsync(findPopUpToModify);
            await _unitOfWork.CommitTransactionAsync(transaction);

            // Refresh cache immediately
            await RefreshCacheAsync();

            return 1;
        }
        catch (Exception e)
        {
            if (transaction != null)
                await _unitOfWork.RollBackTransactionAsync(transaction);

            _logger.LogError("Error occurred when updating pop-up");
            _logger.LogError(e.Message);
            _logger.LogError(e.StackTrace);
            return -1;
        }
    }

    public async Task<int> DeletePopUp(int id)
    {
        IDbContextTransaction? transaction = null;
        try
        {
            _logger.LogInformation("Deleting pop up..");
            transaction = await _unitOfWork.BeginTransactionAsync();
            var popUpToDelete = await _unitOfWork.Repository<PopUps>().GetByIdAsync(id);

            if (popUpToDelete == null)
            {
                _logger.LogInformation($"Pop up with id {id} not found...");
                await _unitOfWork.CommitTransactionAsync(transaction);
                return -2;
            }

            await _unitOfWork.Repository<PopUps>().DeleteAsync(popUpToDelete);
            await _unitOfWork.CommitTransactionAsync(transaction);

            // Refresh cache immediately
            await RefreshCacheAsync();

            return 1;
        }
        catch (Exception e)
        {
            if (transaction != null)
                await _unitOfWork.RollBackTransactionAsync(transaction);

            _logger.LogError("Error occurred when deleting pop-up");
            _logger.LogError(e.Message);
            _logger.LogError(e.StackTrace);
            return -1;
        }
    }

    public async Task<int> DeactivatePopUp(int id)
    {
        IDbContextTransaction? transaction = null;
        try
        {
            _logger.LogInformation("Deactivating pop up..");
            transaction = await _unitOfWork.BeginTransactionAsync();
            var popUpToDelete = await _unitOfWork.Repository<PopUps>().GetByIdAsync(id);

            if (popUpToDelete == null)
            {
                _logger.LogInformation($"Pop up with id {id} not found...");
                await _unitOfWork.CommitTransactionAsync(transaction);
                return -2;
            }

            popUpToDelete.IsActive = false;
            
            await _unitOfWork.Repository<PopUps>().UpdateAsync(popUpToDelete);
            await _unitOfWork.CommitTransactionAsync(transaction);

            // Refresh cache immediately
            await RefreshCacheAsync();

            return 1;
        }
        catch (Exception e)
        {
            if (transaction != null)
                await _unitOfWork.RollBackTransactionAsync(transaction);

            _logger.LogError("Error occurred when deleting pop-up");
            _logger.LogError(e.Message);
            _logger.LogError(e.StackTrace);
            return -1;
        }
    }

    private async Task RefreshCacheAsync()
    {
        try
        {
            var repository = _unitOfWork.Repository<PopUps>();
            var allPopUps = await repository
                .GetSimpleQueryable()
                .Select(p => new PopUpListDto
                {
                    IdPopUp = p.IdPopUp,
                    DescriereJson = p.DescriereJson,
                    TitluJson = p.TitluJson,
                    Voucher = p.IdVoucher != null ? new PopUpVoucher
                        {
                            IdVoucher = p.Voucher!.IdVoucher,
                            CodVoucherDto = p.Voucher.CodVoucher,
                            ReducereDto = p.Voucher.Reducere * 100
                        }
                        : null,
                    IsActive = p.IsActive
                })
                .ToListAsync();
            
            _cache.Set(PopUpsCacheKey, allPopUps, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });
        }
        catch (Exception e)
        {
            _logger.LogError("Error occurred when refreshing pop-ups cache");
            _logger.LogError(e.Message);
        }
    }
}