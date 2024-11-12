using Sqids;

namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.TipuriGalerieDtos;

public class TipuriGalerieDisplayDto
{
    public int IdTipGalerieDto { get; init; }
    public string EncodedIdTipGalerieDto => SqidsEncoder.Encode(IdTipGalerieDto);
    private static readonly SqidsEncoder<int> SqidsEncoder = new();
    public string NumeTipGalerieDto { get; init; } = null!;
    public decimal IncretireDto { get; init; }
    public decimal PretTipGalerieDto { get; init; }
    public bool SePrindeCuIneleDto { get; init; }
}