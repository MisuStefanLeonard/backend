

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace E_Commerce_BackEnd.Models.ProductRelatedModels;

public class Imagini
{
    // Attributes
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdImagine { get; init; }
    
    [StringLength(100)]
    public string? CaleImagine { get; init; }

    [StringLength(70)] 
    public string FisierInBucket { get; set; } = null!;
    
    public int IdProdusCuCuloare { get; init; }
    
    // Foreign Keys
    public ProduseCuCulori ProdusCuCuloare { get; } = null!;

    public Imagini()
    {
        
    }

    public Imagini(string? caleImagine, int idProdusCuCuloare, ProduseCuCulori produsCuCuloare)
    {
        CaleImagine = caleImagine;
        IdProdusCuCuloare = idProdusCuCuloare;
        ProdusCuCuloare = produsCuCuloare;
    }
    
    
   
}