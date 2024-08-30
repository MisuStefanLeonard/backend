namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductOptionsDto;

public class VouchereDto
{
    public string CodVoucherDto { get; set; } = null!;
    public decimal ReducereDto { get; set; }
    public DateTime ExpirareDto { get; set; }
    public bool JustAdded { get; set; } 
}