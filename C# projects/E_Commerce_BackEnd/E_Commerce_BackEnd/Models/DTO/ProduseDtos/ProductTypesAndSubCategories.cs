namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos;

public class ProductTypesAndSubCategories
{
    public IList<string> ProductTypes { get; set; } = new List<string>();
    public IList<string> Categories { get; set; } = new List<string>();
}