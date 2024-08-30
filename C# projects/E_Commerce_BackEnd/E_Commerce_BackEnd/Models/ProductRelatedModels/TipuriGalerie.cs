using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace E_Commerce_BackEnd.Models.ProductRelatedModels;

public class TipuriGalerie
{
   

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdTipGalerie { get; init; }

    [StringLength(30)] 
    public string NumeTipGalerie { get; init; } = null!;
    
    public decimal PretTipGalerie { get; init; }
    
    public ICollection<Manopere>? TipGalerieManopere { get; }

    public TipuriGalerie()
    {
        
    }
    
    public TipuriGalerie(ICollection<Manopere>? tipGalerieManopere)
    {
        TipGalerieManopere = tipGalerieManopere;
    }

    
}