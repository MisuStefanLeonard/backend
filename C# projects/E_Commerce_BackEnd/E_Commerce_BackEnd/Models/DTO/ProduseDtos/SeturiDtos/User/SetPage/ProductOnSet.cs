using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ManopereDto.ManoperaForSet;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductOptionsDto;
using E_Commerce_BackEnd.Models.ProductRelatedModels.JSON_Models;


namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.SeturiDtos.User.SetPage;

public class ProductOnSet
{
    public int? IdProdus { get; init; }
    public string? CodProdusDto { get; init; }
    public string? DescriereDto { get; init; }
    public Descriere? DescriereJsonDto { get; init; }
    public string? NumeProdusDto { get; init; }
    public Nume? NumeJsonDto { get; init; }
    public string? CompozitieDto { get; init; }
    public Compozitie? CompozitieJsonDto { get; init; }
    public byte TvaDto { get; init; }
    public string? IngrijireDto { get; init; }
    public Ingrijire? IngrijireJsonDto { get; init; }
    public bool? FataReversibilaDto { get; init; }
    public string TipulProdusuluiDto { get; init; } = null!;
    public TipProdus TipulProdusuluiJsonDto { get; init; } = null!;

    public string? NumeProducatorDto { get; init; }
    
    public IList<CuloriDto> SelectedColors { get; init; } = new List<CuloriDto>();
    public IList<DimensiuniDto> SelectedDimensions { get; init; } = new List<DimensiuniDto>();
    public IList<StandardManopereOnSet> SelectedManopere { get; init; } = new List<StandardManopereOnSet>();
    
   
}