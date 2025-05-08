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
    private static readonly List<string> InvoiceCredentials = ["smart_bill_username", "smart_bill_password" , "cif"];



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
            // Retrieve current settings from cache or from the database.
            var cachedSettings = await _cache.GetOrCreateAsync("app_settings", async entry => await GetGeneralSettingsData());

            updateGeneralSettingsTransaction = await _unitOfWork.BeginTransactionAsync();
            var generalSettingsRepository = _unitOfWork.Repository<GlobalConfigs>();

            // Get database records that match the keys in the incoming dictionary.
            var existingSettings = await generalSettingsRepository
                .GetSimpleQueryable()
                .Where(g => generalSettingsDict.Keys.Contains(g.NumeAtributGlobal))
                .ToListAsync();

            var configsToUpdate = new List<GlobalConfigs>();
            // Update existing settings.
            foreach (var setting in existingSettings)
            {
                if (generalSettingsDict.TryGetValue(setting.NumeAtributGlobal, out var newValue) &&
                    setting.ValoareAtributGlobal.Trim() != newValue.Trim())
                {
                    setting.ValoareAtributGlobal = newValue.Trim();
                    configsToUpdate.Add(setting);

                    // Update cache value.
                    if (cachedSettings != null)
                    {
                        cachedSettings[setting.NumeAtributGlobal] = newValue.Trim();
                    }
                }
            }

            // Identify unsaved keys (i.e. settings not in the DB).
            var unsavedKeys = generalSettingsDict.Keys.Except(existingSettings.Select(s => s.NumeAtributGlobal)).ToList();
            var configsToAdd = new List<GlobalConfigs>();
            foreach (var key in unsavedKeys)
            {
                var newSetting = new GlobalConfigs
                {
                    NumeAtributGlobal = key,
                    ValoareAtributGlobal = generalSettingsDict[key].Trim()
                };

                configsToAdd.Add(newSetting);

                // Update cache with new setting.
                if (cachedSettings != null)
                {
                    cachedSettings[key] = generalSettingsDict[key];
                }
            }

            // Update the existing settings.
            if (configsToUpdate.Count > 0)
            {
                await generalSettingsRepository.UpdateRangeAsync(configsToUpdate);
            }

            // Add the new settings.
            if (configsToAdd.Count > 0)
            {
                await generalSettingsRepository.AddRangeAsync(configsToAdd);
            }

            await _unitOfWork.CommitTransactionAsync(updateGeneralSettingsTransaction);

            // Update the cache.
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

    public async Task<KeyValuePair<string, string>> GetSpecifiedSetting(string setting)
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

        var kvPairToReturn = settings!
            .FirstOrDefault(kv => kv.Key == setting);

        if (InvoiceCredentials.Contains(kvPairToReturn.Key))
        {
            return new KeyValuePair<string, string>("", "");
        }

        return kvPairToReturn.Key == null ? new KeyValuePair<string, string>("", "")
            : new KeyValuePair<string, string>(kvPairToReturn.Key, kvPairToReturn.Value);
    }
}