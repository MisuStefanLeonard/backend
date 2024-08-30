using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace E_Commerce_BackEnd.Models.ProductRelatedModels;

public class ProduseCuDimensiuni
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdProdusCuDimensiune { get; init; }
    
    // Foreign Keys
    public decimal Pret { get; set; }
    public decimal PretRedus { get; set; }
    public int? IdDimensiune { get; set; }
    public Dimensiuni? PdDimensiune { get; set; } = null!;
    
    public int IdProdus { get; set; }
    public  Produse  PdProduse { get; set; } = null!;

    
}