namespace E_Commerce_BackEnd.Models.DTO.AdminRelatedDtos.OrdersDto;

public class ProductsOnOrdersDto
{
    // main product info
    public int IdProdusDto { get; init; }
    public string CodProdusDto { get; init; } = null!;
    public string NumeProdusDto { get; init; } = null!;
    public decimal GreutateDto { get; init; }
    public bool? FataReversibilaDto { get; init; } 
    public string? NumeProducatorDto { get; init; }
    public string TipulProdusuluiDto { get; init; } = null!;
    public decimal PretBazaDto { get; init; }
    // seturi info ( if it has)
    public string? NumeSetDto { get; init; } 
    public string? InaltimeSetDto { get; init; }
    
    // color info and color code
    public string NumeCuloareDto { get; init; } = null!;
    public string CodCuloareDto { get; init; } = null!;
    // dimensions info ( if it has)
    public string? LungimeDto { get; init; } 
    public string? LatimeDto { get; init; } 
    public string? RecomandarePatDto { get; init; } 
    public bool? PerdeaEstePerecheDto { get; init; }
    // manopera info (if it has)
    public string? TipGalerieCusaturaDto { get; init; }
    public decimal? PretTipGalerieCusaturaDto { get; init; }
    public decimal? IncretireRejansaDto { get; init; }
    public string? TipLinieCusaturaDto { get; init; }
    public decimal? PretTipLinieCusaturaDto { get; init; }
    public string? InelePrindereDto { get; init; } 
    public decimal? TotalMetruMaterial { get; init; }
    // discounts 
    public bool? VoucherFolositDto { get; init; }
    public string? CodVoucherFolositDto { get; init; }
    public decimal? ReducereVoucherDto { get; init; }
    public int NrBucatiDto { get; init; }
    
}