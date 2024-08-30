using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace E_Commerce_BackEnd.Models.ProductRelatedModels;

public class TipuriProduse 
{
    //Attributes
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdTipProdus { get; init; }

    [StringLength(20)] 
    public string TipProdus { get; init; }

    [StringLength(40)]
    public string Categorie { get; init; } = null!;
    
    public ICollection<TipuriPeProduse>? TpTipuriPeProduse { get; }


    public TipuriProduse()
    {
        
    }

    public TipuriProduse(string tipProdus, string categorie,
        ICollection<TipuriPeProduse>? tpTipuriPeProduse)
    {
        TipProdus = tipProdus;
        Categorie = categorie;
        TpTipuriPeProduse = tpTipuriPeProduse == null ? [] : new HashSet<TipuriPeProduse>(tpTipuriPeProduse);

    }

   
}