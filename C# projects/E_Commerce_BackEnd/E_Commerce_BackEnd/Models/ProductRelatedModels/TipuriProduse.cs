using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace E_Commerce_BackEnd.Models.ProductRelatedModels;

public class TipuriProduse 
{
    //Attributes
    public int IdTipProdus { get; init; }
    
    [StringLength(40)]
    public string Categorie { get; init; } = null!;
    
    public ICollection<TipuriPeProduse>? TpTipuriPeProduse { get; }


    public TipuriProduse()
    {
        
    }

    public TipuriProduse( string categorie,
        ICollection<TipuriPeProduse>? tpTipuriPeProduse)
    {
        Categorie = categorie;
        TpTipuriPeProduse = tpTipuriPeProduse == null ? [] : new HashSet<TipuriPeProduse>(tpTipuriPeProduse);

    }

   
}