using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace E_Commerce_BackEnd.Models.ProductRelatedModels.JSON_Models;

public class Categorie
{
    [JsonPropertyName("categorie_en")]
    [JsonProperty(PropertyName = "categorie_en")]
    public string CategorieEngleza { get; set; } = null!;
    [JsonPropertyName("categorie_ro")]
    [JsonProperty(PropertyName = "categorie_ro")]
    public string CategorieRomana { get; set; } = null!;
}