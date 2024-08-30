using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace E_Commerce_BackEnd.Models.ProductRelatedModels;

public class ProduseCuCulori
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdProdusCuCuloare { get; init; }
    public int IdProdus { get; set; } 
    public Produse Produse { get; set; } = null!;
    
    public int IdCuloare { get; set; }
    public Culori Culoare { get; set; } = null!;

    public ICollection<Imagini>? ImagProduseCuCulori { get;} = new HashSet<Imagini>();


}