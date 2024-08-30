using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using E_Commerce_BackEnd.Models.OrderRelatedModels;

namespace E_Commerce_BackEnd.Models.UserRelatedModels;
public class DetaliiFactura 
{
    // Attributes
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdDetaliu { get; init; }
    
    [StringLength(12)]
    public string? Cif { get; init; }
    
    [StringLength(50)]
    public string? NumeFirma { get; init; }
    //Foreign Keys
    public  Adrese Adrese { get; set; } = null!;
    public int IdAdresa { get; init; }

    public ICollection<Comenzi>? DComenzi { get; } 

    public DetaliiFactura()
    {
        
    }

    public DetaliiFactura(string? cif, DateTime dataEmitereFactura, Adrese adrese, int idAdresa, ICollection<Comenzi>? comenzi)
    {
        Cif = cif;
        Adrese = adrese;
        IdAdresa = idAdresa;
        DComenzi = comenzi == null ? [] : new HashSet<Comenzi>(comenzi);
    }

   
    
    
}