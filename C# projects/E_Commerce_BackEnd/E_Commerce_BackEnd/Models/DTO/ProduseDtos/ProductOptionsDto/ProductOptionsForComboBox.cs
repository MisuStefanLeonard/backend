namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductOptionsDto;

public class ProductOptionsForComboBox
{
    public IList<KeyValuePair<string?,int>>? ManuFacturersDto { get; set; } = new List<KeyValuePair<string?,int>>();
    public IList<VouchereDto> VoucherCodes { get; set; } = new List<VouchereDto>();
    public IList<string> ProductTypesForBox { get; set; } = new List<string>();
    public IList<string> ProductCategoriesForBox { get; set; } = new List<string>();
    public IList<string> LatimiForBox { get; set; } = new List<string>();
    public IList<string> LungimiForBox { get; set; } = new List<string>();
    public List<string?> RecomandariForBox { get; set; } = [];
    public List<string?> CoduriCuloriForBox { get; set; } = [];
    public IList<string> CuloriForBox { get; set; } = new List<string>();
    public IList<string> DirectoriesInBucket { get; set; } = new List<string>();

}