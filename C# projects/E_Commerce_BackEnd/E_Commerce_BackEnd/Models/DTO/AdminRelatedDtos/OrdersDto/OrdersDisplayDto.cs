using E_Commerce_BackEnd.Models.DTO.ProduseDtos.VouchereDtos;
using E_Commerce_BackEnd.Models.Enums;
using Sqids;

namespace E_Commerce_BackEnd.Models.DTO.AdminRelatedDtos.OrdersDto;

public class OrdersDisplayDto
{
    public int IdComandaDto { get; init; }
    public string EncodedIdComandaDto => SqidsEncoder.Encode(IdComandaDto);
    private static readonly SqidsEncoder<int> SqidsEncoder = new();
    public IList<ProductsOnOrdersDto> ProduseCuComenziDto { get; set; } = new List<ProductsOnOrdersDto>();
    public IList<IGrouping<string?, ProductsOnOrdersDto>> Seturi { get; set; } = [];
    public AdreseDto? AdresaLivrareDto { get; set; } = new();
    public AdreseDto? AdresaFacturareDto { get; set; } = new();
    public DateTime DataEmitereComandaDto { get; init; }
    public StatusComanda StatusComandaDto { get; set; }
    public TipPlata TipPlataDto { get; set; }
    public string AwbComandaDto { get; init; } = null!;
    public bool IsCancelableDto { get; set; }
    public decimal? PretTotalComanda { get; set; } = 0;
    // public VouchereDto? VoucherPeComanda { get; set; }
}