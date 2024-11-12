namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.BulkOperationsDto;

public class BulkOperationsDto
{
    public IList<object>? SelectedItemsToDoBulkOperations { get; init; } = new List<object>();

}