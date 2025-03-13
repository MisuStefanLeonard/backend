using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace E_Commerce_BackEnd.Models.ProductRelatedModels.JSON_Models;

public class Ingrijire
{
    [JsonPropertyName("ingrijire_en")]
    [JsonProperty(PropertyName ="ingrijire_en")]
    public string? IngrijireEngleza { get; set; }
    [JsonPropertyName("ingrijire_ro")]
    [JsonProperty(PropertyName ="ingrijire_ro")]
    public string? IngrijireRomana { get; set; }
   
}