using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace E_Commerce_BackEnd.Models.ProductRelatedModels;

public class TipuriPeProduse
{
    // Attributes
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdTipPeProdus { get; set; }
    
    // Foreign Keys
    public int IdTipProdus { get; set; }
    public  TipuriProduse TppTipProdus { get; set; } = null!;
    
    public int IdProdus { get; set; }
    public  Produse  TppProdus { get; set; } = null!;
}