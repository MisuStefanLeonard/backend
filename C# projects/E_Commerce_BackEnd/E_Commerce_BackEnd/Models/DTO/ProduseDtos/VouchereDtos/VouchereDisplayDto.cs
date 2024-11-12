using Sqids;

namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.VouchereDtos;

public class VouchereDisplayDto
{
    public int IdVoucherDto { get; set; }
    public string EncodedIdVoucherDto => _sqidsEncoder.Encode(IdVoucherDto);
    private readonly SqidsEncoder<int> _sqidsEncoder = new ();
    public string CodVoucherDto { get; set; } = null!;
    public decimal ReducereDto { get; set; }
    public DateTime DataExpirareDto { get; set; }
    public string Expirat { get; set; } = null!;
}