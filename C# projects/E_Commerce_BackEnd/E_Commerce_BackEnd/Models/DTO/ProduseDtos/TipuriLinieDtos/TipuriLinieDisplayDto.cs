using Sqids;

namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.TipuriLinieDtos;

public class TipuriLinieDisplayDto
{
    public int IdTipLinieDto { get; init; }
    
    public string EncodedIdTipLinie => SqidsEncoder.Encode(IdTipLinieDto);
    private static readonly SqidsEncoder<int> SqidsEncoder = new();
    
    public string NumeTipLinieDto { get; set; } = null!;
    
    public decimal PretPeTipLinieDto { get; set; }
    
}