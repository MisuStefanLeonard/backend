namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.ShoppingCartDtos;

public class UpdatedCartDto
{
    public IList<ProductsChanged> UpdatedProducts { get; init; } = new List<ProductsChanged>();
    public IList<SetOnCartDto> RemovedSetItems { get; init; } = new List<SetOnCartDto>();
    public IList<ProductOnCartDto> RemovedProductItems { get; init; } = new List<ProductOnCartDto>();


}