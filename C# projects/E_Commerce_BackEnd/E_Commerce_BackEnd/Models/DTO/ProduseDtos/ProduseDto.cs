using E_Commerce_BackEnd.Models.ProductRelatedModels.JSON_Models;

namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos;

public class ProduseDto
{
    public string? CodProdusDto { get; init; }
    public string? DescriereDto { get; init; }
    public Descriere? DescriereJsonDto { get; set; }
    public string? NumeProdusDto { get; init; }
    public Nume NumeProdusJsonDto { get; set; } = null!;
    public string? CompozitieDto { get; init; }
    public Compozitie? CompozitieJsonDto { get; set; }
    public byte TvaDto { get; init; }
    public string? IngrijireDto { get; init; }
    public Ingrijire? IngrijireJsonDto { get; set; }
    public bool? FataReversibilaDto { get; init; }
    public ushort? StocDto { get; init; }
    public bool IsDeletedDto { get; set; }
    public bool ActivInMagazinDto { get; set; }
    public decimal PretBazaDto { get; set; }
    public decimal PretBazaRedusDto { get; set; }
    public string TipProdusDto { get; set; } = null!;
    public TipProdus TipProdusJsonDto { get; set; } = null!;
    public bool ProdusLimitatDto { get; set; }
    public bool ActiveazaInNoutati { get; set; }
    public decimal InaltimeMaximaDto { get; set; }

}