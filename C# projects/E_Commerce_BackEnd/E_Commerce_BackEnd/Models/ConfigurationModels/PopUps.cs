using E_Commerce_BackEnd.Models.ProductRelatedModels.JSON_Models;
using E_Commerce_BackEnd.Models.ProductVouchersModels;

namespace E_Commerce_BackEnd.Models.ConfigurationModels;

public class PopUps
{
    public int IdPopUp { get; init; }
    public Descriere DescriereJson { get; set; } = null!;
    public Nume TitluJson { get; set; } = null!;
    public int? IdVoucher { get; set; }
    public Vouchere? Voucher { get; set; } 
    public bool IsActive { get; set; }
}