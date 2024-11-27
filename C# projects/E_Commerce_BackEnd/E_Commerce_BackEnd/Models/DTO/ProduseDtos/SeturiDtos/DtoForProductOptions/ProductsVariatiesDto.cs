using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ManopereDto.ManoperaForSet;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductOptionsDto;

namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.SeturiDtos.DtoForProductOptions;

public class ProductsVariatiesDto
{
    public IList<DimensiuniDto> DimensionVariaties { get; } = new List<DimensiuniDto>();
    public IList<CuloriDto> ColorsVariaties { get; } = new List<CuloriDto>();
    public IList<StandardManopereOnSet>? StandardManopere { get; set; } = new List<StandardManopereOnSet>();
}