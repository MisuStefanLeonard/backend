using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using E_Commerce_BackEnd.Models.ProductRelatedModels;

namespace E_Commerce_BackEnd.Models.OrderRelatedModels;

public class ProduseCuComenzi
{
    // Attributes
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdProduseCuComenzi { get; set; }
    public int NrBucati { get; set; }
    // Fk
    public int IdProdus { get; set; } 
    public  Produse Produs { get; set; } = null!;
    public int IdComanda { get; set; }
    public Comenzi Comanda { get; set; } = null!;
    public int? IdDimensiune { get; set; }
    public int IdCuloare { get; set; }
    public int? IdManopera { get; set; }
    

}