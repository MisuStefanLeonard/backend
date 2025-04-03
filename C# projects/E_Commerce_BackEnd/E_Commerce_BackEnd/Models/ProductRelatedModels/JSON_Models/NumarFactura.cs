using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace E_Commerce_BackEnd.Models.ProductRelatedModels.JSON_Models;

public class NumarFactura
{
    [JsonPropertyName("numar_en")]
    [JsonProperty(PropertyName ="numar_en")]
    public string? NumarEngleza { get; set; }
    [JsonPropertyName("numar_ro")]
    [JsonProperty(PropertyName ="numar_ro")]
    public string? NumarRomana { get; set; }
}