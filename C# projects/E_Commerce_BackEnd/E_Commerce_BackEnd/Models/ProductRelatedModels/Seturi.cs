using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using E_Commerce_BackEnd.Models.OrderRelatedModels;
using E_Commerce_BackEnd.Models.UserRelatedModels;
using Sqids;

namespace E_Commerce_BackEnd.Models.ProductRelatedModels;

public class Seturi
{
    // Attributes
    public int IdSet { get; init; }

    private static readonly SqidsEncoder<int> SqidsEncoder = new ();
    public string EncodedIdSet => SqidsEncoder.Encode(IdSet);
    [StringLength(100)] 
    public string NumeSet { get; init; } = null!;
    [StringLength(150)] 
    public string DescriereSet { get; init; } = null!;
    public decimal PretSet { get; init; }
    public decimal PretRedusSet { get; init; }
    public bool SetActivInMagazin { get; set; }
    public bool IsDeleted { get; set; }
    public ICollection<AsociereSeturi>? SAsociereSeturi { get; init; } = new List<AsociereSeturi>();
    public ICollection<ProduseCuComenzi>? CombinatieSetPeComanda { get; init; } = new List<ProduseCuComenzi>();
    public ICollection<CosCumparaturi>? SeturiPeCos { get; init; } = new List<CosCumparaturi>();
    public ICollection<Reviews>? ReviewPeSet { get; init; } = new List<Reviews>();

}