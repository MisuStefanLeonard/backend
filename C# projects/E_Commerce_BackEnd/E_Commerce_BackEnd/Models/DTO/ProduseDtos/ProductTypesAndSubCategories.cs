using E_Commerce_BackEnd.Models.ProductRelatedModels.JSON_Models;

namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos;

public class ProductTypesAndSubCategories
{
    // public IList<string> ProductTypes { get; set; } = new List<string>();
    // public IList<string> Categories { get; set; } = new List<string>();
    public IList<TipProdus> ProductTypesJson { get; set; } = new List<TipProdus>();
    // public IList<Categorie> CategoriesJson { get; set; } = new List<Categorie>();
}