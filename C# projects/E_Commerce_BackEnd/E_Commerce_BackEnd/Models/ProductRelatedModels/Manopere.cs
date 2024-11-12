using E_Commerce_BackEnd.Models.OrderRelatedModels;

namespace E_Commerce_BackEnd.Models.ProductRelatedModels;

public class Manopere 
{
    public int IdManopera { get; init; }
    public int? IdInelPrindere { get; init; }
    public InelePrindere? InelPrindereLaManopera { get; init; }
    public int IdTipLinie { get; init; }
    public TipuriLinie TipLinieLaManopera { get; init; } = null!;
    public int IdTipGalerie { get; init; }
    public TipuriGalerie TipGalerieLaManopera { get; init; } = null!;
    public ICollection<ProduseCuComenzi>? ManopereCuComenzi { get;  }
    public ICollection<CosCumparaturi>? ManoperePeCos { get; }
    public decimal PretCurentTipLinie { get; init; }
    public decimal PretCurentTipGalerie { get; init; }
    public decimal MaterialFolosit { get; init; }
    
}