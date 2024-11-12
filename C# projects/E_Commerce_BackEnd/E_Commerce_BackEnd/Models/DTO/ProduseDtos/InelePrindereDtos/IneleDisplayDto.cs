using Sqids;

namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.InelePrindereDtos;

public class IneleDisplayDto
{
    public int IdInelDto { get; init; }
    public string EncodedIdInelDto => _sqidsEncoder.Encode(IdInelDto);
    private static SqidsEncoder<int> _sqidsEncoder = new();
    public string CuloareInelDto { get; init; } = null!;
  
}