using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using E_Commerce_BackEnd.Models.ProductRelatedModels;

namespace E_Commerce_BackEnd.Models.ProductVouchersModels;

public class ProduseCuVouchere
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdProdusCuVoucher { get; init; }
    
    public int IdProdus { get; set; } 
    public int IdVoucher { get; set; }

    public  Produse PvProdus { get; set; } = null!;
    public Vouchere PvVoucher { get; set; } = null!;

}