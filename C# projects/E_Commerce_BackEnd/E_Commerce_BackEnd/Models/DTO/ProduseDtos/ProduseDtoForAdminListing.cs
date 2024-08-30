namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos;

public class ProduseDtoForAdminListing
{
    public string? CodProdusAdminDto { get; set; }
    public string? NumeProdusAdminDto { get; set; }
    public string? TipProdusDto { get; set; }
    public IList<string> CategoriiProdusDto { get; init; } = null!;

}