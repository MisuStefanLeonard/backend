using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace E_Commerce_BackEnd.Models.ProductRelatedModels;

public class Manopere 
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdManopera { get; init; }
    
    public int IdMaterial { get; set; }
    public Materiale MaterialLaManopere { get; set; } = null!;
    
    public int? IdInelPrindere { get; set; }
    public InelePrindere? InelPrindereLaManopera { get; set; }
    
    public int IdTipLinie { get; set; }
    public TipuriLinie TipLinieLaManopera { get; set; } = null!;
    
    public int IdTipGalerie { get; set; }

    public TipuriGalerie TipGalerieLaManopera { get; set; } = null!;
    
    public Manopere()
    {
        
    }


    public Manopere(Materiale materialLaManopere, TipuriLinie tipLinieLaManopera)
    {
        MaterialLaManopere = materialLaManopere;
        TipLinieLaManopera = tipLinieLaManopera;
    }
    
}