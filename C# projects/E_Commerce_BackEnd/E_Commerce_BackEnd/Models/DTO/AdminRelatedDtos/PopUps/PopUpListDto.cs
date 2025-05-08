using E_Commerce_BackEnd.Models.DTO.ProduseDtos.VouchereDtos;
using E_Commerce_BackEnd.Models.ProductRelatedModels.JSON_Models;

namespace E_Commerce_BackEnd.Models.DTO.AdminRelatedDtos.PopUps;

public class PopUpListDto
{
    public int IdPopUp { get; init; }
    public Descriere DescriereJson { get; set; } = null!;
    public Nume TitluJson { get; set; } = null!;
    public PopUpVoucher? Voucher { get; set; } = null!;
    public bool IsActive { get; set; }
}