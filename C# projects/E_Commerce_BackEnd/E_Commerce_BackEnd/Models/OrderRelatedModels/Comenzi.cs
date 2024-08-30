using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using E_Commerce_BackEnd.Models.Enums;
using E_Commerce_BackEnd.Models.ProductRelatedModels;
using E_Commerce_BackEnd.Models.UserRelatedModels;

namespace E_Commerce_BackEnd.Models.OrderRelatedModels;


public class Comenzi : ICloneable
{
    // Attributes
    
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdComanda { get; init; }
    public DateTime DataEmitereComanda { get; init; }
    public StatusComanda StatusComanda { get; init; }
    public TipPlata TipPlata { get; init; }
    
    // Foreign Keys
   
    public int IdDetaliu { get; set; }
    public  DetaliiFactura CDetaliiFactura { get; }


    // Many to Many mappings
    
    public  ICollection<ProduseCuComenzi>? PcComenzi { get; }

    public Comenzi() { }
    
    public Comenzi(DateTime dataEmitereComanda, StatusComanda statusComanda, 
        TipPlata tipPlata, int idDetaliu, DetaliiFactura cDetaliiFactura, 
         ICollection<ProduseCuComenzi>? pcComenzi)
    {
        
        DataEmitereComanda = dataEmitereComanda;
        StatusComanda = statusComanda;
        TipPlata = tipPlata;
        IdDetaliu = idDetaliu;
        CDetaliiFactura = cDetaliiFactura;
        PcComenzi = pcComenzi == null ? [] : new HashSet<ProduseCuComenzi>(pcComenzi);
    }
    
    public Comenzi(Comenzi other)
    {
        IdComanda = other.IdComanda;
        DataEmitereComanda = other.DataEmitereComanda;
        StatusComanda = other.StatusComanda;
        TipPlata = other.TipPlata;
        IdDetaliu = other.IdDetaliu;
        CDetaliiFactura = other.CDetaliiFactura;
        PcComenzi = new HashSet<ProduseCuComenzi>(other.PcComenzi);
    }

    public object Clone()
    {
        return new Comenzi(this);
    }

}