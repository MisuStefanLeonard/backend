

namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.ShoppingCartDtos;

public class SetOnCartDto
{
    public IList<ProductInfoOnCart> ProductsInCart { get; init; } = new List<ProductInfoOnCart>();
    public string? EncodedIdSet { get; init; }
}

public class ProductInfoOnCart
{
    public int? IdProdus { get; init; }
    public int IdCuloare { get; init; }
    public int IdDimensiune { get; init; }
    public int? IdManopera { get; init; }
    public decimal PretCurent { get; init; }
}