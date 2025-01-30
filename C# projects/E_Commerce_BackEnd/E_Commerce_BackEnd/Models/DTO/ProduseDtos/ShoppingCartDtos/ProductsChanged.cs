namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.ShoppingCartDtos;

public class ProductsChanged
{
    public string NumeProdus { get; set; } = null!;
    public decimal PretVechiGeneral { get; set; }
    public decimal PretNouGeneral { get; set; }
    // case where it is a cuvertura with a dimension.
    public string? Lungime { get; set; }
    public string? Latime { get; set; }
    //
    public decimal PretRejansaVechi { get; set; }
    public decimal PretRejansaNou { get; set; }
    //
    public decimal PretTipLinieVechi { get; set; }
    public decimal PretTipLinieNou { get; set; }
    //
    public decimal PretBazaVechi { get; set; }
    public decimal PretBazaNou { get; set; }
    
}