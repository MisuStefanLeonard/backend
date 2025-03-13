using System.ComponentModel.DataAnnotations;
using E_Commerce_BackEnd.Models.ProductRelatedModels.JSON_Models;
using Sqids;

namespace E_Commerce_BackEnd.Models.ProductRelatedModels;

public class TipuriGalerie 
{
    
    public int IdTipGalerie { get; init; }
    public string EncodedIdTipGalerie => SqidsEncoder.Encode(IdTipGalerie);
    private static readonly SqidsEncoder<int> SqidsEncoder = new();
    public Nume NumeTipGalerieJson { get; init; } = null!;
    public decimal IncretireRejansa { get; init; }
    public decimal PretTipGalerie { get; init; }
    public bool IsDeleted { get; set; }
    public bool IsLocked { get; set; }
    public bool SePrindeCuInele { get; set; }
    [StringLength(100)] 
    public string? CaleRelativa { get; set; }

    public ICollection<Manopere>? TipGalerieManopere { get; } = new List<Manopere>();
    
}