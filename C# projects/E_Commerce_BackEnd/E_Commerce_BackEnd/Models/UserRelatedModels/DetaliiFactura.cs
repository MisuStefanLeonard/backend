using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using E_Commerce_BackEnd.Models.OrderRelatedModels;

namespace E_Commerce_BackEnd.Models.UserRelatedModels;
public class DetaliiFactura 
{
    // Attributes
    public int IdDetaliu { get; init; }
    
    [StringLength(12)]
    public string? Cif { get; set; }
    
    [StringLength(50)]
    public string? NumeFirma { get; set; }
    //Foreign Keys

    public ICollection<Adrese>? DfAdrese { get; } = new HashSet<Adrese>(); 
    
}