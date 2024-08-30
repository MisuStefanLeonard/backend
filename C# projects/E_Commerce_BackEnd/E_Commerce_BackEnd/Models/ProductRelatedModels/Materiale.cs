using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace E_Commerce_BackEnd.Models.ProductRelatedModels;

public class Materiale
{
    
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdMaterial { get; init; }

    [StringLength(50)] 
    public string NumeMaterial { get; set; } = null!;
    
    public decimal PretMaterial { get; set; }
    
    public decimal PretMaterialRedus { get; set; }
    
    public ICollection<Manopere>? MaterialPeManopere { get; }

    public Materiale()
    {
        
    }
    
    public Materiale(ICollection<Manopere> materialPeManopere)
    {
        MaterialPeManopere = materialPeManopere;
    }

}