using E_Commerce_BackEnd.Models.Enums;
using Sqids;

namespace E_Commerce_BackEnd.Models.DTO.AdminRelatedDtos.OrdersDto;

public class MainOrdersDisplayDto
{
    public int IdComandaDto { get; init; }
    public string EncodedIdComandaDto => SqidsEncoder.Encode(IdComandaDto);
    public int IdContDto { get; init; }
    public string EncodedIdContDto => SqidsEncoder.Encode(IdContDto);
    private static readonly SqidsEncoder<int> SqidsEncoder = new();
    public DateTime DataEmitereComandaDto { get; init; }
    public StatusComanda StatusComandaDto { get; set; }
    public TipPlata TipPlataDto { get; set; }
    public string AwbComandaDto { get; init; } = null!;
    public bool IsSet { get; init; }
    public decimal? PretTotalComanda { get; set; } = 0;
}