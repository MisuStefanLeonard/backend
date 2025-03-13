using E_Commerce_BackEnd.Models.ProductRelatedModels.JSON_Models;
using Sqids;

namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.InelePrindereDtos;

public class IneleDisplayDto
{
    public int IdInelDto { get; init; }
    public string EncodedIdInelDto => SqidsEncoder.Encode(IdInelDto);
    private static readonly SqidsEncoder<int> SqidsEncoder = new();
    public string CuloareInelDto { get; init; } = null!;
    public Culoare CuloareInelJsonDto { get; init; } = null!;

}