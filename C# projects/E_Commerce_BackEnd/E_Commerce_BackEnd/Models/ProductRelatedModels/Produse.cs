using System.ComponentModel.DataAnnotations;
using E_Commerce_BackEnd.Models.OrderRelatedModels;
using E_Commerce_BackEnd.Models.ProductRelatedModels.JSON_Models;
using E_Commerce_BackEnd.Models.UserRelatedModels;

namespace E_Commerce_BackEnd.Models.ProductRelatedModels;

public class Produse 
{
    // Attributes
    
    public int IdProdus { get; init; }
    [StringLength(40)] 
    public string CodProdus { get; init; } = null!;
    public Descriere? DescriereJson { get; set; }
    public Nume NumeProdusJson { get; set; } = null!;
    public Compozitie? CompozitieJson { get; set; }
    public byte Tva { get; init; }
    public Ingrijire? IngrijireJson { get; set; }
    public bool? FataReversibila { get; init; } 
    public ushort? Stoc { get; init; }
    public TipProdus TipulProdusuluiJson { get; set; } = null!;
    public bool IsDeleted { get; set; }
    public bool ActivInMagazin { get; set; }
    public decimal PretDeBaza { get; set; } // caz in care avem o perdea/draperie
    public decimal PretDeBazaRedus { get; set; } // caz in care avem perdea/draperie
    public bool IsLocked { get; set; }
    public bool AfiseazaInNoutati { get; set; } 
    public bool ProdusLimitat { get; set; }
    public decimal InaltimeMaxima { get; init; }
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
    public ICollection<Reviews>? ProductReviews { get; } 

}