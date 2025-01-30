using System.ComponentModel.DataAnnotations;
using E_Commerce_BackEnd.Models.UserRelatedModels;

namespace E_Commerce_BackEnd.Models.ProductRelatedModels;

public class CosCumparaturi
{
    public int IdProdusInCos { get; init; } 
    public int CantitateProdus { get; set; }
    public decimal PretProdus { get; set; }
    public string? InaltimeAleasaPentruSet { get; init; }
    public string IdentificatorSet { get; init; } = null!;
    public DateTime ExpiresAt { get; set; }
    public Guid SessionId { get; init; }
    public Conturi? Cont { get; init; } 
    public int? IdCont { get; init; }
    
    public Produse? Produs { get; init; } 
    public int? IdProdus { get; init; }

    public Culori Culoare { get; init; } = null!;
    public int IdCuloare { get; init; }

    public Dimensiuni? Dimensiune { get; init; } 
    public int? IdDimensiune { get; init; }

    public Seturi? Set { get; init; } 
    public int? IdSet { get; init; }

    public Manopere? Manopera { get; init; } 
    public int? IdManopera { get; set; }


}