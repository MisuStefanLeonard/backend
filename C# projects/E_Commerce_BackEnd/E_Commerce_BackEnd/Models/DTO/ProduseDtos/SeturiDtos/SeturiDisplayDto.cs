namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.SeturiDtos;

public class SeturiDisplayDto
{
    public int IdSetDto { get; init; }
    public string EncodedIdSetDto { get; init; } = null!;
    public string NumeSetDto { get; set; } = null!;
    public string DescriereSetDto { get; set; } = null!;
    
    public decimal PretSetDto { get; set; }

    public decimal PretRedusSetDto { get; set; }
    
    public bool SetActivInMagazin { get; set; }
    
}