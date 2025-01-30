using Newtonsoft.Json;

namespace E_Commerce_BackEnd.Models.DTO.Recaptcha;

public class RecaptchaResponse
{
    [JsonProperty("success")]
    public bool Success { get; set; }

    [JsonProperty("challenge_ts")]
    public DateTime ChallengeTimestamp { get; set; }

    [JsonProperty("hostname")]
    public string Hostname { get; set; } = null!;

    [JsonProperty("score")]
    public float Score { get; set; }

    [JsonProperty("action")]
    public string Action { get; set; }  = null!;

    [JsonProperty("error-codes")]
    public string[] ErrorCodes { get; set; }  = null!;
}