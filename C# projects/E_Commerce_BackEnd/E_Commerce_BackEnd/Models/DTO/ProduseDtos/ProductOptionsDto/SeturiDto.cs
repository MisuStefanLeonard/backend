namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductOptionsDto;

public class SeturiDto
{
    public string NumeSetDto { get; set; } = null!;
    
    public string DescriereSetDto { get; set; } = null!;
    
    public decimal PretSetDto { get; set; }

    public decimal PretRedusSetDto { get; set; }
    
    public decimal SetActivInMagazin { get; set; }
    public bool JustAdded { get; set; } = false;
}