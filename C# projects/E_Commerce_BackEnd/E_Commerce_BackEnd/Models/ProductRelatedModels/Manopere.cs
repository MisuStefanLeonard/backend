using E_Commerce_BackEnd.Models.Enums;
using E_Commerce_BackEnd.Models.OrderRelatedModels;
using E_Commerce_BackEnd.Models.ProductRelatedModels.JSON_Models;

namespace E_Commerce_BackEnd.Models.ProductRelatedModels;

public class Manopere 
{
    public int IdManopera { get; init; }
    public Nume? NumeManoperaJson { get; init; }
    public int? IdInelPrindere { get; set; }
    public InelePrindere? InelPrindereLaManopera { get; init; }
    public int IdTipLinie { get; set; }
    public TipuriLinie TipLinieLaManopera { get; init; } = null!;
    public int IdTipGalerie { get; set; }
    public TipuriGalerie TipGalerieLaManopera { get; init; } = null!;
    public decimal PretCurentTipLinie { get; init; }
    public decimal PretCurentTipGalerie { get; init; }
    public decimal MaterialFolosit { get; init; }
    public TipManopere TipManopera { get; init; }
    public bool IsLocked { get; set; }
    public string? InaltimeMaxima { get; init; }
    
    public ICollection<ProduseCuComenzi>? ManopereCuComenzi { get;  }
    public ICollection<CosCumparaturi>? ManoperePeCos { get; }
    public ICollection<AsociereSeturi>? ManopereStandardPeSet { get; }
}