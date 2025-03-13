using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace E_Commerce_BackEnd.Models.ProductRelatedModels.JSON_Models;

public class Descriere
{
    [JsonPropertyName("descriere_en")]
    [JsonProperty(PropertyName ="descriere_en")]
    public string? DescriereEngleza { get; set; }
    [JsonPropertyName("descriere_ro")]
    [JsonProperty(PropertyName ="descriere_ro")]
    public string? DescriereRomana { get; set; }
    

}