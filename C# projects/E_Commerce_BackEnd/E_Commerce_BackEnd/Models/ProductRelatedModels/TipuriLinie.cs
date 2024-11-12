using System.ComponentModel.DataAnnotations;
using Sqids;

namespace E_Commerce_BackEnd.Models.ProductRelatedModels;

public class TipuriLinie 
{

    public int IdTipLinie { get; init; }
    
    public string EncodedIdTipLinie => SqidsEncoder.Encode(IdTipLinie);
    private static readonly SqidsEncoder<int> SqidsEncoder = new();

    [StringLength(30)] 
    public string NumeTipLinie { get; set; } = null!;
    public decimal PretPeTipLinie { get; set; }

    [StringLength(100)] 
    public string? CaleRelativa { get; set; } 
    public bool IsDeleted { get; set; }
    public ICollection<Manopere>? TipLiniePeManopere { get; }

    public TipuriLinie()
    {
        
    }
    
    public TipuriLinie(ICollection<Manopere>? tipLiniePeManopere)
    {
        TipLiniePeManopere = tipLiniePeManopere;
    }
    
    
}