using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace E_Commerce_BackEnd.Models.ProductRelatedModels.JSON_Models;

public class Culoare
{
    [JsonPropertyName("culoare_en")]
    [JsonProperty(PropertyName ="culoare_en")]
    public string CuloareEngleza { get; set; } = null!;
    [JsonPropertyName("culoare_ro")]
    [JsonProperty(PropertyName ="culoare_ro")]
    public string CuloareRomana { get; set; } = null!;

}