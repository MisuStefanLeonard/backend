using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using E_Commerce_BackEnd.Models.OrderRelatedModels;

namespace E_Commerce_BackEnd.Models.ProductRelatedModels;

public class InelePrindere
{
    

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdInel { get; init; }

    [StringLength(20)] 
    public string CuloareInel { get; init; } = null!;
    
    public decimal PretPerMetruInele { get; init; } 
    
    public ICollection<Manopere>? InelPeManopere { get; }

    public InelePrindere()
    {
        
    }
    
    public InelePrindere(ICollection<Manopere>? inelPeManopere)
    {
        InelPeManopere = inelPeManopere;
    }

}