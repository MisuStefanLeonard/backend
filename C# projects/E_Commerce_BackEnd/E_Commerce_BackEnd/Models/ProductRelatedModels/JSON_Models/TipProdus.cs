using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace E_Commerce_BackEnd.Models.ProductRelatedModels.JSON_Models;

public class TipProdus
{
    [JsonPropertyName("tip_en")]
    [JsonProperty(PropertyName ="tip_en")]
    public string TipProdusEngleza { get; set; } = null!;
    [JsonPropertyName("tip_ro")]
    [JsonProperty(PropertyName ="tip_ro")]
    public string TipProdusRomana { get; set; } = null!;
   
}