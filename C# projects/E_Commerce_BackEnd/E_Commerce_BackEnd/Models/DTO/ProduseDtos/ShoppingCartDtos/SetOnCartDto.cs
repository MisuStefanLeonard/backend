

namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.ShoppingCartDtos;

public class SetOnCartDto
{
    public IList<ProductInfoOnCart> ProductsInCart { get; init; } = new List<ProductInfoOnCart>();
    public string? EncodedIdSet { get; init; }
    public string CurrentCurrency { get; init; } = null!;
    public decimal PretCurent { get; set; }
}

public class ProductInfoOnCart
{
    public int IdProdus { get; init; }
    public int IdCuloare { get; init; }
    public int? IdDimensiune { get; init; }
    public int? IdManopera { get; init; }
    public string? PrefferedHeight { get; init; }
    
}