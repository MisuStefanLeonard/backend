

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using E_Commerce_BackEnd.Models.OrderRelatedModels;

namespace E_Commerce_BackEnd.Models.ProductRelatedModels;

public class AsociereSeturi
{
    // Attributes
    public int IdAsociereSet { get; init; }
    
    // Foreign Keys
    public int IdProdus { get; init; }
    public  Produse Produs { get; init; } = null!;
    public int IdSet { get; init; }
    public  Seturi Set { get; init; } = null!;
    public int? IdCuloare { get; init; }
    public Culori? AsCuloare { get; init; } 
    public int? IdDimensiune { get; init; }
    public Dimensiuni? AsDimensiune { get; init; } 
    public int? IdManopera { get; init; }
    public Manopere? Manopera { get; init; }
    
}