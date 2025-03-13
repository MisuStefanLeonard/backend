using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace E_Commerce_BackEnd.Models.ProductRelatedModels.JSON_Models;

public class Compozitie
{
    [JsonPropertyName("compozitie_en")]
    [JsonProperty(PropertyName ="compozitie_en")]
    public string? CompozitieEngleza { get; set; }
    [JsonPropertyName("compozitie_ro")]
    [JsonProperty(PropertyName ="compozitie_ro")]
    public string? CompozitieRomana { get; set; }
   

}