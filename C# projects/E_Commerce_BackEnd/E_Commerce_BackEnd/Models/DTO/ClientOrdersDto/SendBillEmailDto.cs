namespace E_Commerce_BackEnd.Models.DTO.ClientOrdersDto;

public class SendBillEmailDto
{
    public string Currency { get; set; } = null!;
    public string Email { get; init; } = null!;
    public int OrderId { get; init; }
}