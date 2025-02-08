using E_Commerce_BackEnd.Models.DTO.ProduseDtos.VouchereDtos;
using E_Commerce_BackEnd.Models.Enums;

namespace E_Commerce_BackEnd.Models.DTO.AdminRelatedDtos.OrdersDto;

public class PlaceOrderDto
{
    public TipPlata TipPlata { get; init; }
    public decimal PretTransport { get; init; }
    public decimal PretTotal { get; set; }
    public string NumePeComanda { get; init; } = null!;
    public string PrenumePeComanda { get; init; } = null!;
    public string NrTelefonPeComanda { get; init; } = null!;
    public string EmailPeComanda { get; init; } = null!;
    public VouchereDto? VoucherAplicat { get; init; }
    public AdreseDto AdresaLivrare { get; init; } = null!;
    public AdreseDto AdresaFacturare { get; init; } = null!;

}