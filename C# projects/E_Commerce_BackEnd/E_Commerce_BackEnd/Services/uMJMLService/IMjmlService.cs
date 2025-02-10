namespace E_Commerce_BackEnd.Services.uMJMLService;

public interface IMjmlService
{
    public Task<string?> ConvertMjmlToHtml(string mjmlString);
}