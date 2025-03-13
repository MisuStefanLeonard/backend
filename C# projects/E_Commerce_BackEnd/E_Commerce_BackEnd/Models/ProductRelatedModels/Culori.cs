using System.ComponentModel.DataAnnotations;
using E_Commerce_BackEnd.Models.OrderRelatedModels;
using E_Commerce_BackEnd.Models.ProductRelatedModels.JSON_Models;

namespace E_Commerce_BackEnd.Models.ProductRelatedModels;

public class Culori 
{
    // Attributes
    
    public int IdCuloare { get; init; }
    
    public Culoare NumeCuloareJson { get; set; } = null!;
    
    // Foreign keys
    public int IdCodCuloare { get; init; }
    public CodCulori CodCuloare { get; } = null!;

    public ICollection<ProduseCuCulori>? CProduseCuCulori { get; } 
    public ICollection<AsociereSeturi>? CAsociereSeturi { get; }
    public ICollection<ProduseCuComenzi>? CProduseCuComenzi { get; }
    public ICollection<CosCumparaturi>? CuloriPeCosCumparaturi { get; }

    

}