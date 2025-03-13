using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using E_Commerce_BackEnd.Models.ProductRelatedModels.JSON_Models;

namespace E_Commerce_BackEnd.Models.ProductRelatedModels;

public class TipuriProduse 
{
    //Attributes
    public int IdTipProdus { get; init; }
    public Categorie CategorieJson { get; set; } = null!;
    
    public ICollection<TipuriPeProduse>? TpTipuriPeProduse { get; }
    

   
}