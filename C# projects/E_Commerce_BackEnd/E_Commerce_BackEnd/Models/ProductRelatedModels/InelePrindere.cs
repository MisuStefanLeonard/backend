using System.ComponentModel.DataAnnotations;
using Sqids;

namespace E_Commerce_BackEnd.Models.ProductRelatedModels;

public class InelePrindere 
{
   
    public int IdInel { get; init; }
    public string EncodedIdInel => SqidsEncoder.Encode(IdInel);
    private static readonly SqidsEncoder<int> SqidsEncoder = new();
    [StringLength(20)] 
    public string CuloareInel { get; init; } = null!;
    
    [StringLength(100)] 
    public string? CaleRelativa { get; set; }
    public bool IsDeleted { get; set; }
    
    public ICollection<Manopere>? InelPeManopere { get; }

    public InelePrindere()
    {
        
    }
    
    public InelePrindere(ICollection<Manopere>? inelPeManopere)
    {
        InelPeManopere = inelPeManopere;
    }

}