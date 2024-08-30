using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using E_Commerce_BackEnd.Models.OrderRelatedModels;
using E_Commerce_BackEnd.Models.ProductVouchersModels;

namespace E_Commerce_BackEnd.Models.ProductRelatedModels;

public class Produse 
{
    // Attributes
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
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
    public decimal Greutate { get; init; }
    public bool? FataReversibila { get; init; }
    public ushort? Stoc { get; init; }

    [StringLength(20)]
    public string TipulProdusului { get; init; } = null!;
    public bool IsDeleted { get; set; }
    public bool ActivInMagazin { get; set; }
    
    

    // Foreign keys
    public int? IdProducator { get; init; }
    public Producatori? Producator { get; init; }
    
    
    // Many-To-Many mapping

    public  ICollection<ProduseCuVouchere>? PvProduse { get;  }
    public  ICollection<ProduseCuComenzi>? ComenziProduse { get; }
    public  ICollection<AsociereSeturi>? PAsociereSeturi { get;  } 
    public  ICollection<TipuriPeProduse>? PTipuriPeProduse { get; }
    public ICollection<ProduseCuDimensiuni>? PProduseCuDimensiuni { get; } 
    public ICollection<ProduseCuCulori>? PProduseCuCulori { get; }

    public Produse()
    {
        
    }

   
    public Produse(string codProdus,string? descriere, string? numeProdus,
        string? compozitie,byte tva, 
        string? ingrijire, decimal greutate, 
        bool? fataReversibila, ushort? stoc,bool isDeleted ,bool activInMagazin,int? idProducator, 
        Producatori? producator, ICollection<ProduseCuVouchere>? pvProduse, 
        ICollection<ProduseCuComenzi>? produseCuComenzi, ICollection<AsociereSeturi>? asociereSeturi , 
         ICollection<TipuriPeProduse>? pTipuriPeProduse,
        ICollection<ProduseCuDimensiuni>? pProduseCuDimensiuni, ICollection<ProduseCuCulori> pProduseCuCulori)
    {
        CodProdus = codProdus;
        Descriere = descriere;
        NumeProdus = numeProdus;
        Compozitie = compozitie;
        Tva = tva;
        Ingrijire = ingrijire;
        Greutate = greutate;
        FataReversibila = fataReversibila;
        Stoc = stoc;
        IsDeleted = isDeleted;
        ActivInMagazin = activInMagazin;
        IdProducator = idProducator;
        Producator = producator;
        PProduseCuCulori = pProduseCuCulori;
        PvProduse = pvProduse == null ? [] : new HashSet<ProduseCuVouchere>(pvProduse);
        ComenziProduse = produseCuComenzi == null ? [] : new HashSet<ProduseCuComenzi>(produseCuComenzi);
        PAsociereSeturi = asociereSeturi == null ? [] : new HashSet<AsociereSeturi>(asociereSeturi);
        PTipuriPeProduse = pTipuriPeProduse == null ? [] : new HashSet<TipuriPeProduse>(pTipuriPeProduse);
        PProduseCuDimensiuni = pProduseCuDimensiuni == null ? [] : new HashSet<ProduseCuDimensiuni>(pProduseCuDimensiuni);

    }


  
}