

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace E_Commerce_BackEnd.Models.ProductRelatedModels;

public class AsociereSeturi
{
    // Attributes
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdAsociereSet { get; set; }
    
    // Foreign Keys
    public int IdProdus { get; set; }
    public  Produse Produs { get; set; } = null!;
    
    public int IdSet { get; set; }
    public  Seturi Set { get; set; } = null!;
    
    
}