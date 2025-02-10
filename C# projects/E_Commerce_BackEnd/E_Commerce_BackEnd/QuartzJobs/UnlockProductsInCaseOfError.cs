using E_Commerce_BackEnd.Models.ProductRelatedModels;
using E_Commerce_BackEnd.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Quartz;

namespace E_Commerce_BackEnd.QuartzJobs;

public sealed class UnlockProductsInCaseOfError : IJob
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SessionTokenCleanUp> _logger;

    public UnlockProductsInCaseOfError(IUnitOfWork unitOfWork, ILogger<SessionTokenCleanUp> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        IDbContextTransaction? unlockCleanUpTransaction = null;
        try
        {
            unlockCleanUpTransaction = await _unitOfWork.BeginTransactionAsync();
            var lockedProducts = await _unitOfWork.Repository<Produse>()
                .FindQueryable(p => p.IsLocked)
                .ToListAsync();

            if (lockedProducts.Count > 0)
            {
                foreach (var product in lockedProducts)
                {
                    product.IsLocked = false;
                }

                await _unitOfWork.Repository<Produse>().AddRangeAsync(lockedProducts);
            }
            
            var lockedSets = await _unitOfWork.Repository<Seturi>()
                .FindQueryable(s => s.IsLocked)
                .ToListAsync();

            if (lockedSets.Count > 0)
            {
                foreach (var set in lockedSets)
                {
                    set.IsLocked = false;
                }

                await _unitOfWork.Repository<Seturi>().AddRangeAsync(lockedSets);
            }
            
            
            var lockedRings = await _unitOfWork.Repository<InelePrindere>()
                .FindQueryable(ringTypes => ringTypes.IsLocked)
                .ToListAsync();

            if (lockedRings.Count > 0)
            {
                foreach (var ring in lockedRings)
                {
                    ring.IsLocked = false;
                }

                await _unitOfWork.Repository<InelePrindere>().AddRangeAsync(lockedRings);
            }
            
            var lockedGalleryTypes = await _unitOfWork.Repository<TipuriGalerie>()
                .FindQueryable(galleryType => galleryType.IsLocked)
                .ToListAsync();

            if (lockedGalleryTypes.Count > 0)
            {
                foreach (var galleryType in lockedGalleryTypes)
                {
                    galleryType.IsLocked = false;
                }

                await _unitOfWork.Repository<TipuriGalerie>().AddRangeAsync(lockedGalleryTypes);
            }
            
            var lockedLiningTypes = await _unitOfWork.Repository<TipuriLinie>()
                .FindQueryable(lineType => lineType.IsLocked)
                .ToListAsync();

            if (lockedLiningTypes.Count > 0)
            {
                foreach (var lineType in lockedLiningTypes)
                {
                    lineType.IsLocked = false;
                }

                await _unitOfWork.Repository<TipuriLinie>().AddRangeAsync(lockedLiningTypes);
            }
            
            
             
            var lockedManoperas = await _unitOfWork.Repository<Manopere>()
                .FindQueryable(manopera => manopera.IsLocked)
                .ToListAsync();

            if (lockedManoperas.Count > 0)
            {
                foreach (var manopera in lockedManoperas)
                {
                    manopera.IsLocked = false;
                }

                await _unitOfWork.Repository<Manopere>().AddRangeAsync(lockedManoperas);
            }


            await _unitOfWork.CommitTransactionAsync(unlockCleanUpTransaction);
            
        }
        catch (Exception e)
        {
            if (unlockCleanUpTransaction != null)
            {
                await _unitOfWork.RollBackTransactionAsync(unlockCleanUpTransaction);
            }
            _logger.LogError("Error thrown in UnlockingTransaction");
            _logger.LogError(e.Message);
            _logger.LogError(e.StackTrace);

        }
       
    }
}