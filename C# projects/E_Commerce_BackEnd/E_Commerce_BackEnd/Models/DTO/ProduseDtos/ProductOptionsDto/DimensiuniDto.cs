namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductOptionsDto;

public class DimensiuniDto
{
    public string LungimeDto { get; set; } = null!;
    public string LatimeDto { get; set; } = null!;
    public string RecomandarePat { get; set; } = null!;
    public decimal PretDto { get; set; } 
    public decimal PretRedusDto { get; set; } 
    
    public bool JustAdded { get; set; }
    
    public bool? PerdeaEstePerecheDto { get; set; }

}