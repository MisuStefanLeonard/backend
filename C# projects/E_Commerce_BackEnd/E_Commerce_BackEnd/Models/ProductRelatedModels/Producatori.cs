

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace E_Commerce_BackEnd.Models.ProductRelatedModels;

public class Producatori 
{
    // Attributes
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdProducator { get; init; }
    
    [StringLength(30)]
    public string? NumeProducator { get; init; }

    // Entity mappings
    public ICollection<Produse>? ProducatoriProduse { get;}

    public Producatori()
    {
        
    }

    public Producatori(string? numeProducator, ICollection<Produse>? producatoriProduse)
    {
       
        NumeProducator = numeProducator;
        ProducatoriProduse = producatoriProduse == null ? [] : new HashSet<Produse>(producatoriProduse);
    }
    
}