using E_Commerce_BackEnd.Models.ConfigurationModels;
using E_Commerce_BackEnd.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;

namespace E_Commerce_BackEnd.Services.uGeneralService;

public class GeneralSettingsService : IGeneralSettingsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GeneralSettingsService> _logger;
    private readonly IMemoryCache _cache;


    public GeneralSettingsService(IUnitOfWork unitOfWork, ILogger<GeneralSettingsService> logger, IMemoryCache cache)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _cache = cache;
    }

    public async Task<int> SaveGeneralSettings(IDictionary<string, string> generalSettingsDict)
{
    IDbContextTransaction? updateGeneralSettingsTransaction = null;
    try
    {
        // Retrieve current settings from cache
        var cachedSettings = await _cache.GetOrCreateAsync("app_settings", async entry => await GetGeneralSettingsData());

        updateGeneralSettingsTransaction = await _unitOfWork.BeginTransactionAsync();
        var generalSettingsRepository = _unitOfWork.Repository<GlobalConfigs>();

        // Get database records that need to be updated
        var findSettingsToModify = await generalSettingsRepository
            .GetSimpleQueryable()
            .Where(g => generalSettingsDict.Keys.Contains(g.NumeAtributGlobal))
            .ToListAsync();

        if (findSettingsToModify.Count == 0)
        {
            throw new Exception("No matching attributes found in the database.");
        }

        var configsToModify = new List<GlobalConfigs>();
        foreach (var setting in findSettingsToModify)
        {
            if (generalSettingsDict.TryGetValue(setting.NumeAtributGlobal, out var newValue) &&
                setting.ValoareAtributGlobal != newValue)
            {
                setting.ValoareAtributGlobal = newValue;
                configsToModify.Add(setting);

                // **Update cache value**
                if (cachedSettings != null)
                {
                    cachedSettings[setting.NumeAtributGlobal] = newValue;
                }
            }
        }

        // Update database if necessary
        if (configsToModify.Count > 0)
        {
            await generalSettingsRepository.UpdateRangeAsync(configsToModify);
        }

        await _unitOfWork.CommitTransactionAsync(updateGeneralSettingsTransaction);

        // **Update the cache with modified settings**
        if (cachedSettings != null)
        {
            _cache.Set("app_settings", cachedSettings, TimeSpan.FromMinutes(30));
        }

        return 1;
    }
    catch (Exception e)
    {
        if (updateGeneralSettingsTransaction != null)
        {
            await _unitOfWork.RollBackTransactionAsync(updateGeneralSettingsTransaction);
            _logger.LogError("Error occurred. Rolling back transaction");
        }
        _logger.LogError(e.StackTrace);
        _logger.LogError(e.Message);

        return -2;
    }
}


    public async Task<IDictionary<string, string>> GetGeneralSettingsData()
    {
        try
        {
            var settings = await _cache.GetOrCreateAsync("app_settings", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
                var generalSettingsRepository = _unitOfWork.Repository<GlobalConfigs>();

                var getGeneralSettings = await generalSettingsRepository
                    .GetSimpleQueryable()
                    .ToDictionaryAsync(group => group.NumeAtributGlobal, group => group.ValoareAtributGlobal);

                return getGeneralSettings;
            });

            // foreach (var item in settings)
            // {
            //     _logger.LogInformation($"key : {item.Key} , value : {item.Value}");
            // }

            if (settings.IsNullOrEmpty())
            {
                _logger.LogWarning("EMPTY CACHE");
            }

            return settings ?? [];
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            _logger.LogError(e.StackTrace);
            return new Dictionary<string, string>();
        }
       
    }
}