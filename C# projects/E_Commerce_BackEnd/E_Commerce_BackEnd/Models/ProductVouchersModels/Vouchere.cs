using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using E_Commerce_BackEnd.Models.OrderRelatedModels;
using MySqlConnector;


namespace E_Commerce_BackEnd.Models.ProductVouchersModels;

public class Vouchere 
{
    // Attributes
    public int IdVoucher { get; init; }
    [StringLength(10)]
    public string CodVoucher { get; init; } = null!;
    public decimal Reducere { get; init; }
    public DateTime DataExpirare { get; set; }
    public bool IsDeleted { get; set; }
    // Foreign Keys
    public ICollection<Comenzi>? VVoucherePeComenzi { get; }
    
    
    
}