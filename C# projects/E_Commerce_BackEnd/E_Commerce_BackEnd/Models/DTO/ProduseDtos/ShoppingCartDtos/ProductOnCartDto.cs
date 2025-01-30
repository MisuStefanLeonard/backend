namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.ShoppingCartDtos;

public class ProductOnCartDto
{
    public int IdProdus { get; init; }
    public int IdCuloare { get; init; }
    public int? IdDimensiune { get; init; } // optional
    public string? LungimeSina { get; init; } // optional
    public string? Inaltime { get; init; } // optional
    public bool? Pereche { get; init; } // optional
    public int IdRejansa { get; init; } // optional
    public int IdInelPrindere { get; init; }// optional
    public int IdTipLinie { get; init; }// optional
    public decimal PretCurent { get; set; }
    public decimal MaterialFolosit { get; init; }// optional
    public decimal PretCurentTipLinie { get; set; }// optional
    public decimal PretCurentTipGalerie { get; set; }// optional
    public string CurrentCurrency { get; init; } = null!;
}