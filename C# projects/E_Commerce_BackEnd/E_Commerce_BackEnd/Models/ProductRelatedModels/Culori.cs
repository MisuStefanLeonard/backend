
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using E_Commerce_BackEnd.Models.OrderRelatedModels;

namespace E_Commerce_BackEnd.Models.ProductRelatedModels;

public class Culori 
{
    // Attributes
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdCuloare { get; init; }

    [StringLength(20)] 
    public string NumeCuloare { get; init; } = null!;
    
    // Foreign keys
    public int IdCodCuloare { get; init; }
    public CodCulori CodCuloare { get; } = null!;

    public ICollection<ProduseCuCulori>? CProduseCuCulori { get; }
    

    public Culori()
    {
       
    }

    public Culori(string numeCuloare, int idCodCuloare, 
        CodCulori codCuloare, ICollection<ProduseCuCulori>? cProduseCuCulori)
    {
        NumeCuloare = numeCuloare;
        IdCodCuloare = idCodCuloare;
        CodCuloare = codCuloare;
        CProduseCuCulori = cProduseCuCulori == null ? [] : new HashSet<ProduseCuCulori>(cProduseCuCulori);

    }
    

}