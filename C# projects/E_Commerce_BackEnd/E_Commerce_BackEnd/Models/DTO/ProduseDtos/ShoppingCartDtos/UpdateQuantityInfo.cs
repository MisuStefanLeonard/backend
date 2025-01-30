namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.ShoppingCartDtos;

public class UpdateQuantityInfo
{
    public int IdProdus { get; set; }
    public int? IdSet { get; set; } = null!;
    public int? IdCont { get; set; } = null;
    public Guid SessionId { get; set; } = Guid.Empty;
    public int IdCuloare { get; init; }
    public int? IdDimensiune { get; init; }
    public int? IdManopera { get; set; } = null;
    public string IdentificatorSet { get; init; } = null!;
    public bool IsDecrementing { get; init; }
}