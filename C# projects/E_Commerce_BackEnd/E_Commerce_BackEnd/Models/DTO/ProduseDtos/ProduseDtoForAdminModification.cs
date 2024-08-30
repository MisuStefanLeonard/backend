using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductOptionsDto;

namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos;

public class ProduseDtoForAdminModification
{
    public string? CodProdusDto { get; set; }
    public string? DescriereDto { get; set; }
    public string? NumeProdusDto { get; set; }
    public string? CompozitieDto { get; set; }
    public byte TvaDto { get; set; }
    public string? IngrijireDto { get; set; }
    public decimal GreutateDto { get; set; }
    public bool? FataReversibilaDto { get; set; }
    public ushort? StocDto { get; set; }
    public bool ActivInMagazinDto { get; set; }
    public string TipulProdusuluiDto { get; set; } = null!;
    public string? NumeProducatorDto { get; set; } 
    public IList<TipuriProdusDto> TipuriProduseDto { get; set; } = [];
    public IList<DimensiuniDto>? DimensiuniProduseDto { get; set; } = [];
    public IList<CuloriDto> CuloriProdusDto { get; set; } = [];
    public IList<VouchereDto>? VouchereProdusDto { get; set; } = [];
    public IList<SeturiDto>? SeturiProdusDto { get; set; } = [];


}