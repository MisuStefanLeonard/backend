using System.ComponentModel.DataAnnotations;
using E_Commerce_BackEnd.Models.ProductRelatedModels.JSON_Models;
using Sqids;

namespace E_Commerce_BackEnd.Models.ProductRelatedModels;

public class InelePrindere 
{
   
    public int IdInel { get; init; }
    public string EncodedIdInel => SqidsEncoder.Encode(IdInel);
    private static readonly SqidsEncoder<int> SqidsEncoder = new();
    public Culoare CuloareInelJson { get; init; } = null!;
    
    [StringLength(100)] 
    public string? CaleRelativa { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsLocked { get; set; }

    
    public ICollection<Manopere>? InelPeManopere { get; }

    public InelePrindere()
    {
        
    }
    
    public InelePrindere(ICollection<Manopere>? inelPeManopere)
    {
        InelPeManopere = inelPeManopere;
    }

}