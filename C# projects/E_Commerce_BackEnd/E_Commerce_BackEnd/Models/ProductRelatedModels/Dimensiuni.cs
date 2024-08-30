

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace E_Commerce_BackEnd.Models.ProductRelatedModels;

public class Dimensiuni 
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdDimensiune { get; init; }

    [StringLength(4)] 
    public string Lungime { get; init; } = null!;
    
    [StringLength(4)]
    public string Latime { get; init; } = null!;

    public bool? PerdeaEstePereche { get; set; }
    
    [StringLength(15)]
    public string? RecomandarePat { get; set; }

    public ICollection<ProduseCuDimensiuni>? DProduseCuDimensiuni { get; }

    public Dimensiuni()
    {
        
    }

    public Dimensiuni(string lungime, string latime,string? recomandarePat ,ICollection<ProduseCuDimensiuni>? dProduseCuDimensiuni)
    {
        Lungime = lungime;
        Latime = latime;
        RecomandarePat = recomandarePat;
        DProduseCuDimensiuni = dProduseCuDimensiuni == null ? [] : new HashSet<ProduseCuDimensiuni>(dProduseCuDimensiuni);
    }
    
}