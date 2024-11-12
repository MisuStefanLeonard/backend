namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.SeturiDtos.DtoForProductOptions;

public class SelectProducts
{
    public string Title { get; set; } = null!;
    public IList<ProdusDto> Children { get; set; } = new List<ProdusDto>();
}

public class ProdusDto
{
    public string Id { get; set; } = null!;
    public string Title { get; set; } = null!;
}