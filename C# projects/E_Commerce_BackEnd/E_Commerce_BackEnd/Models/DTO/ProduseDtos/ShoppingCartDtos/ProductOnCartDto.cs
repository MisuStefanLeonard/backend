namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.ShoppingCartDtos;

public class ProductOnCartDto
{
    public int? IdProdus { get; init; }
    public string? EncodedIdSet { get; init; }
    public int IdCuloare { get; init; }
    public int IdDimensiune { get; init; }
    public int? IdManopera { get; init; }
    public decimal PretCurent { get; init; }
}