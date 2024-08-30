using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace E_Commerce_BackEnd.Models.ProductRelatedModels;

public class TipuriLinie
{

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdTipLinie { get; init; }

    [StringLength(30)] 
    public string NumeTipLinie { get; set; } = null!;
    
    public decimal PretPeTipLinie { get; set; }

    public ICollection<Manopere>? TipLiniePeManopere { get; }

    public TipuriLinie()
    {
        
    }
    
    public TipuriLinie(ICollection<Manopere>? tipLiniePeManopere)
    {
        TipLiniePeManopere = tipLiniePeManopere;
    }
    
    
}