using E_Commerce_BackEnd.Models.Enums;
using Sqids;

namespace E_Commerce_BackEnd.Models.DTO.AdminRelatedDtos.Accounts;

public class ConturiDisplayDto
{
    public int IdContDto { get; init; } 
    
    public string EncodedIdContDto => SqidsEncoder.Encode(IdContDto);
    public string? NumeDto { get; set; }
    public string? PrenumeDto { get; set; }
    public bool ContActivDto { get; set; }
    public DateTime DataCreareDto { get; set; }
    public string? EmailDto { get; set; } 
    public string? UsernameDto { get; set; } 
    public string? RolDto { get; set; }
    public TipConturi TipContDto { get; set; }
    
    private static readonly SqidsEncoder<int> SqidsEncoder = new();
}