
namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.SeturiDtos;

public class SetModificationDto
{
    public IList<ProductForSetDto> ProductsOnSet { get; set; } = new List<ProductForSetDto>();
    public string NumeSetDto { get; set; } = null!;
    public string DescriereSetDto { get; set; } = null!;
    public decimal PretSetDto { get; set; }
    public decimal PretRedusSetDto { get; set; }
}