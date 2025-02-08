using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using E_Commerce_BackEnd.Models.ProductRelatedModels;

namespace E_Commerce_BackEnd.Models.OrderRelatedModels;

public class ProduseCuComenzi
{
    // Attributes
    public int IdProduseCuComenzi { get; init; }
    public int NrBucati { get; set; }
    public decimal PretCumparat{ get; init; }
    public string? InaltimeAleasaPentruSet { get; init; }

    public string IdentificatorSet { get; init; } = null!;
    // Fk
    public int? IdSet { get; set; }
    public Seturi? Set { get; set; }
    public int IdProdus { get; set; }
    public Produse Produs { get; set; } = null!;
    public int IdComanda { get; set; }
    public Comenzi Comanda { get; set; } = null!;
    public int IdCuloare { get; set; }
    public Culori PcCuloare { get; set; } = null!;
    public int? IdDimensiune { get; set; }
    public Dimensiuni? PcDimensiune { get; set; } 
    public int? IdManopera { get; set; }
    public Manopere? PcManopera { get; set; }
    

}