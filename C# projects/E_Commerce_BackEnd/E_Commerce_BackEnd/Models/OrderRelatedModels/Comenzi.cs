using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using E_Commerce_BackEnd.Models.Enums;
using E_Commerce_BackEnd.Models.ProductRelatedModels;
using E_Commerce_BackEnd.Models.ProductVouchersModels;
using E_Commerce_BackEnd.Models.UserRelatedModels;

namespace E_Commerce_BackEnd.Models.OrderRelatedModels;


public class Comenzi 
{
    // Attributes
    
    public int IdComanda { get; init; }
    public DateTime DataEmitereComanda { get; init; }
    public StatusComanda StatusComanda { get; init; }
    public TipPlata TipPlata { get; init; }
    [StringLength(50)]
    public string AwbComanda { get; set; } = null!;
    public decimal PretTransport { get; init; }
    public string NumePeComanda { get; init; } = null!;
    public string PrenumePeComanda { get; init; } = null!;
    public string NrTelefonPeComanda { get; init; } = null!;
    public string EmailPeComanda { get; init; } = null!;
    public string UniqueConfirmationToken { get; init; } = null!;
    public bool UniqueConfirmationTokenUsed { get; set; }
    public bool IsOrderPayed { get; set; }
    public bool IsCancelable { get; set; }
    
    // Foreign Keys
   
    public int IdAdresaLivrare { get; set; }
    public Adrese CAdresaLivrare { get; } = null!;
    public int IdAdresaFacturare { get; set; }
    public Adrese CAdresaFacturare { get; } = null!;
    public int? IdVoucher { get; set; }
    public Vouchere? VoucherPeComanda { get; } 
    
    // Many to Many mappings

    public ICollection<ProduseCuComenzi>? PcComenzi { get; } = new List<ProduseCuComenzi>();




}