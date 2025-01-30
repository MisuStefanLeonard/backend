using E_Commerce_BackEnd.Models.ConfigurationModels;
using E_Commerce_BackEnd.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace E_Commerce_BackEnd.Services.uGeneralService;

public class GeneralSettingsService : IGeneralSettingsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GeneralSettingsService> _logger;


    public GeneralSettingsService(IUnitOfWork unitOfWork, ILogger<GeneralSettingsService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<int> SaveGeneralSettings(IDictionary<string, string> generalSettingsDict)
    {
        IDbContextTransaction? updateGeneralSettingsTransaction = null;
        try
        {
            var attributeNamesValues = new List<string>();
            var attributeNamesList = new List<string>();
            attributeNamesList.AddRange(generalSettingsDict.Keys);
            attributeNamesValues.AddRange(generalSettingsDict.Values);

            foreach (var pair in generalSettingsDict)
            {
                _logger.LogInformation($"{pair.Key} : {pair.Value}");
            }

            updateGeneralSettingsTransaction = await _unitOfWork.BeginTransactionAsync();
            var generalSettingsRepository = _unitOfWork.Repository<GlobalConfigs>();

            var findSettingsToModify = await generalSettingsRepository
                .GetSimpleQueryable()
                .Where(g => attributeNamesList.Contains(g.NumeAtributGlobal))
                .ToListAsync();

            if (findSettingsToModify.Count < 0)
            {
                throw new Exception("Empty db attributes");
            }

            foreach (var setting in findSettingsToModify)
            {
                _logger.LogInformation(setting.NumeAtributGlobal);
                _logger.LogInformation($"{findSettingsToModify.Count}");
            }
            
            var configsToModify = new List<GlobalConfigs>();
            for (var i = 0 ; i < findSettingsToModify.Count ; i++ )
            {
                if (findSettingsToModify[i].ValoareAtributGlobal == attributeNamesValues[i]) continue;
                
                findSettingsToModify[i].ValoareAtributGlobal = attributeNamesValues[i];
                configsToModify.Add(findSettingsToModify[i]);
            }

            if (configsToModify.Count > 0)
            {
                await generalSettingsRepository.UpdateRangeAsync(configsToModify);
            }

            await _unitOfWork.CommitTransactionAsync(updateGeneralSettingsTransaction);
            return 1;
        }
        catch (Exception e)
        {
            if (updateGeneralSettingsTransaction != null)
            {
                await _unitOfWork.RollBackTransactionAsync(updateGeneralSettingsTransaction);
                _logger.LogError("Error occured. Rolling back transaction");
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
            var generalSettingsRepository = _unitOfWork.Repository<GlobalConfigs>();

            var getGeneralSettings = await generalSettingsRepository
                .GetSimpleQueryable()
                .ToDictionaryAsync(group => group.NumeAtributGlobal , group => group.ValoareAtributGlobal);

            return getGeneralSettings;
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            _logger.LogError(e.StackTrace);
            return new Dictionary<string, string>();
        }
       
    }
}