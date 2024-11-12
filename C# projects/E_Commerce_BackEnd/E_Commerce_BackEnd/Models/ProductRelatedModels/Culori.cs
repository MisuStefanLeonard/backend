
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using E_Commerce_BackEnd.Models.OrderRelatedModels;

namespace E_Commerce_BackEnd.Models.ProductRelatedModels;

public class Culori 
{
    // Attributes
    
    public int IdCuloare { get; init; }

    [StringLength(20)] 
    public string NumeCuloare { get; init; } = null!;
    
    // Foreign keys
    public int IdCodCuloare { get; init; }
    public CodCulori CodCuloare { get; } = null!;

    public ICollection<ProduseCuCulori>? CProduseCuCulori { get; } 
    public ICollection<AsociereSeturi>? CAsociereSeturi { get; }
    public ICollection<ProduseCuComenzi>? CProduseCuComenzi { get; }
    public ICollection<CosCumparaturi>? CuloriPeCosCumparaturi { get; }

    

}