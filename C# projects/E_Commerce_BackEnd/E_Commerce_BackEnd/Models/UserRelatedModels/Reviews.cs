using E_Commerce_BackEnd.Models.ProductRelatedModels;

namespace E_Commerce_BackEnd.Models.UserRelatedModels;

public class Reviews
{
    public int IdRecenzie { get; init; }
    public int? IdProdus { get; init; }
    public Produse? Produs { get; init; } 
    public int IdCont { get; init; }
    public Conturi Cont { get; init; } = null!;
    public int? IdSet { get; init; }
    public Seturi? Set { get; init; } 
    public int NumarStele { get; init; }
    public string TextRecenzie { get; init; } = null!;
}