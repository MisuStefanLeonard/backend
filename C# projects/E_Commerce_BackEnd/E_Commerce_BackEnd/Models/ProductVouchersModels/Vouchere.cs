using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MySqlConnector;


namespace E_Commerce_BackEnd.Models.ProductVouchersModels;

public class Vouchere 
{
    // Attributes
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdVoucher { get; init; }
    
    [StringLength(10)]
    public string CodVoucher { get; init; }
    public decimal Reducere { get; init; }
    public DateTime DataExpirare { get; set; }
    // Foreign Keys
    public ICollection<ProduseCuVouchere>? VProduse { get; }

    public Vouchere()
    {
        
    }

    public Vouchere(string codVoucher, 
        decimal reducere, ICollection<ProduseCuVouchere>? vProduse)
    {
        CodVoucher = codVoucher;
        Reducere = reducere;
        if (vProduse != null)
        {
            VProduse = new HashSet<ProduseCuVouchere>(vProduse);
        }

        VProduse = new HashSet<ProduseCuVouchere>();

    }
    
}