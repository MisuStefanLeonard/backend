using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using E_Commerce_BackEnd.Models.OrderRelatedModels;
using E_Commerce_BackEnd.Models.ProductVouchersModels;
using E_Commerce_BackEnd.Models.UserRelatedModels;

namespace E_Commerce_BackEnd.Models.ProductRelatedModels;

public class Produse 
{
    // Attributes
    
    public int IdProdus { get; init; }

    [StringLength(40)] 
    public string CodProdus { get; init; } = null!;

    [StringLength(150)]
    public string? Descriere { get; init; }
    
    [StringLength(50)]
    public string? NumeProdus { get; init; }

    [StringLength(50)]
    public string? Compozitie { get; init; }
    public byte Tva { get; init; }
    
    [StringLength(150)]
    public string? Ingrijire { get; init; }
    public bool? FataReversibila { get; init; }
    public ushort? Stoc { get; init; }
    [StringLength(20)]
    public string TipulProdusului { get; init; } = null!;
    public bool IsDeleted { get; set; }
    public bool ActivInMagazin { get; set; }
    public decimal PretDeBaza { get; set; } // caz in care avem o perdea/draperie
    public decimal PretDeBazaRedus { get; set; } // caz in care avem perdea/draperie
    public bool IsLocked { get; set; }
    public bool AfiseazaInNoutati { get; set; } 
    public bool ProdusLimitat { get; set; }

    // Foreign keys
    public int? IdProducator { get; init; }
    public Producatori? Producator { get; init; }
    
    
    // Many-To-Many mapping
    public ICollection<CosCumparaturi>? ProduseInCos { get; } 
    public  ICollection<ProduseCuComenzi>? ComenziProduse { get; } 
    public  ICollection<AsociereSeturi>? PAsociereSeturi { get;  } 
    public  ICollection<TipuriPeProduse>? PTipuriPeProduse { get; } 
    public ICollection<ProduseCuDimensiuni>? PProduseCuDimensiuni { get; }  
    public ICollection<ProduseCuCulori>? PProduseCuCulori { get; }
    public ICollection<Reviews>? ProductReviews { get; } = null!;

}