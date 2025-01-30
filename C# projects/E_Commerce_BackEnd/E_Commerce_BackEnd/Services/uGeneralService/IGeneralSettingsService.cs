namespace E_Commerce_BackEnd.Services.uGeneralService;

public interface IGeneralSettingsService
{
    public Task<int> SaveGeneralSettings(IDictionary<string, string> generalSettingsDict);
    public Task<IDictionary<string, string>> GetGeneralSettingsData();
}