namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.ManopereDto;

public class ManopereListingDto
{
   public string EncodedIdManoperaDto { get; init; } = null!;
   public string NumeManoperaDto { get; init; } = null!;
   public string TipLinieDto { get; init; } = null!; // colt
   public string? TipInelDto { get; init; } 
   public string TipCusaturaDto { get; init; } = null!; // galerie
   
}