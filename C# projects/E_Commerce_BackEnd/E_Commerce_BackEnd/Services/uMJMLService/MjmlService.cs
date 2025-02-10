using System.Text;
using Newtonsoft.Json;
using RestSharp;

namespace E_Commerce_BackEnd.Services.uMJMLService;

public class MjmlService : IMjmlService
{
    private const string MjmlBackEndApiKey = "2f1e397d-1c0d-4868-aa1c-15e1df20443c";
    private const string MjmlUsernameKey = "aa493c83-801e-4e90-a3b3-3897c31a9c38";
    private const string MjmlEndpoint = "https://api.mjml.io/v1/render";
    private readonly ILogger<MjmlService> _logger;

    public MjmlService(ILogger<MjmlService> logger)
    {
        _logger = logger;
    }

    public async Task<string?> ConvertMjmlToHtml(string mjml)
    {
        _logger.LogInformation("Converting MJML to HTML");
        var client = new RestClient(MjmlEndpoint); // creating endpoint;
        var request = new RestRequest
        {
            Method = Method.Post,
        };
        _logger.LogInformation(mjml);
        request.AddHeader("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{MjmlUsernameKey}:{MjmlBackEndApiKey}")));
        request.AddHeader("Content-Type", "application/json");
        request.AddJsonBody(new {mjml});
        
        var response = await client.ExecuteAsync(request);
        _logger.LogInformation(response.ErrorMessage);

        if (response.IsSuccessful)
        {
            _logger.LogInformation("Succefully converted");
            var result = JsonConvert.DeserializeObject<MjmlApiResponse>(response.Content);
            return result?.Html;
        }
        
        _logger.LogError("Error when converting mjml to HTML");
        return null;
    }
}

public class MjmlApiResponse
{
    public string Html { get; set; } = "";
}
