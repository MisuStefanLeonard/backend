using E_Commerce_BackEnd.Models.UserRelatedModels;

namespace E_Commerce_BackEnd.Models.ProductRelatedModels;

public class CosCumparaturi
{
    public int IdProdusInCos { get; init; } 
    public int CantitateProdus { get; set; }
    public decimal PretProdus { get; init; }

    public Conturi Cont { get; init; } = null!;
    public int IdCont { get; init; }
    
    public Produse? Produs { get; init; } 
    public int? IdProdus { get; init; }

    public Culori Culoare { get; init; } = null!;
    public int IdCuloare { get; init; }

    public Dimensiuni Dimensiune { get; init; } = null!;
    public int IdDimensiune { get; init; }

    public Seturi? Set { get; init; } 
    public int? IdSet { get; init; }

    public Manopere? Manopera { get; init; } 
    public int? IdManopera { get; init; }


}