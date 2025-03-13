using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace E_Commerce_BackEnd.Models.ProductRelatedModels.JSON_Models;

public class Nume
{
    [JsonPropertyName("nume_en")]
    [JsonProperty(PropertyName ="nume_en")]
    public string NumeEngleza { get; set; } = null!;
    [JsonPropertyName("nume_ro")]

    [JsonProperty(PropertyName ="nume_ro")]
    public string NumeRomana { get; set; } = null!;
   

}