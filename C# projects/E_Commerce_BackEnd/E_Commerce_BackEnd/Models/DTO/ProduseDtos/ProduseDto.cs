namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos;

public class ProduseDto
{
    public string? CodProdusDto { get; init; }
    public string? DescriereDto { get; init; }
    public string? NumeProdusDto { get; init; }
    public string? CompozitieDto { get; init; }
    public byte TvaDto { get; init; }
    public string? IngrijireDto { get; init; }
    public decimal GreutateDto { get; init; }
    public bool? FataReversibilaDto { get; init; }
    public ushort? StocDto { get; init; }
    public bool IsDeletedDto { get; set; }
    public bool ActivInMagazinDto { get; set; }

    public string TipProdusDto { get; set; } = null!;
}