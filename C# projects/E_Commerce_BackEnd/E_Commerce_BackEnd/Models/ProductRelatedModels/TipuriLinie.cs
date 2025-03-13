using System.ComponentModel.DataAnnotations;
using E_Commerce_BackEnd.Models.ProductRelatedModels.JSON_Models;
using Sqids;

namespace E_Commerce_BackEnd.Models.ProductRelatedModels;

public class TipuriLinie 
{

    public int IdTipLinie { get; init; }
    
    public string EncodedIdTipLinie => SqidsEncoder.Encode(IdTipLinie);
    private static readonly SqidsEncoder<int> SqidsEncoder = new();
    public Nume NumeTipLinieJson { get; init; } = null!;
    public decimal PretPeTipLinie { get; set; }

    [StringLength(100)] 
    public string? CaleRelativa { get; set; } 
    public bool IsDeleted { get; set; }
    public bool IsLocked { get; set; }
    public ICollection<Manopere>? TipLiniePeManopere { get; }

    public TipuriLinie()
    {
        
    }
    
    public TipuriLinie(ICollection<Manopere>? tipLiniePeManopere)
    {
        TipLiniePeManopere = tipLiniePeManopere;
    }
    
    
}