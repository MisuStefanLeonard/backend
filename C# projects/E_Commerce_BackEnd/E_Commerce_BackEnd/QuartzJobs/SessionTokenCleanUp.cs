using E_Commerce_BackEnd.Models.ProductRelatedModels;
using E_Commerce_BackEnd.Models.UserRelatedModels;
using E_Commerce_BackEnd.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Quartz;

namespace E_Commerce_BackEnd.QuartzJobs;

public sealed class SessionTokenCleanUp : IJob
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SessionTokenCleanUp> _logger;

    public SessionTokenCleanUp( IUnitOfWork unitOfWork, ILogger<SessionTokenCleanUp> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        IDbContextTransaction? cleanUpSessionTokenTransaction = null;
        try
        {
            _logger.LogInformation("Starting session tokens clean up job!");
            cleanUpSessionTokenTransaction = await _unitOfWork.BeginTransactionAsync();
            var findSessionTokensThatExpired = await _unitOfWork.Repository<RememberUser>()
                .FindQueryable(userSession => userSession.ExpiresAt <= DateTime.UtcNow)
                .ToListAsync();

            await _unitOfWork.Repository<RememberUser>().DeleteRangeAsync(findSessionTokensThatExpired);
            await _unitOfWork.CommitTransactionAsync(cleanUpSessionTokenTransaction);
            _logger.LogInformation("Ending  session tokens up job!");

        }
        catch (Exception e)
        {
            if (cleanUpSessionTokenTransaction != null)
            {
                await _unitOfWork.RollBackTransactionAsync(cleanUpSessionTokenTransaction);
            }
            _logger.LogInformation("Error thrown in seesionTokenCleanup");
            _logger.LogError(e.StackTrace);
            throw;
        }
    }
}