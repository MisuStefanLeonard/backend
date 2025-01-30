using E_Commerce_BackEnd.Models.ProductRelatedModels;
using E_Commerce_BackEnd.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Quartz;

namespace E_Commerce_BackEnd.QuartzJobs;

public sealed class CartCleanUp : IJob
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CartCleanUp> _logger;

    public CartCleanUp( IUnitOfWork unitOfWork, ILogger<CartCleanUp> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        IDbContextTransaction? cleanUpCartTransaction = null;
        try
        {
            _logger.LogInformation("Starting cart clean up job!");
            // 15.05.2024 - item expires , 16.05.2024 current.
            cleanUpCartTransaction = await _unitOfWork.BeginTransactionAsync();
            var findItemsInCartThatAreExpired = await _unitOfWork.Repository<CosCumparaturi>()
                .FindQueryable(item => item.ExpiresAt <= DateTime.UtcNow)
                .ToListAsync();

            await _unitOfWork.Repository<CosCumparaturi>().DeleteRangeAsync(findItemsInCartThatAreExpired);
            await _unitOfWork.CommitTransactionAsync(cleanUpCartTransaction);
            _logger.LogInformation("Ending cart clean up job!");

        }
        catch (Exception e)
        {
            if (cleanUpCartTransaction != null)
            {
                await _unitOfWork.RollBackTransactionAsync(cleanUpCartTransaction);
            }
            Console.WriteLine(e);
            throw;
        }
    }
}