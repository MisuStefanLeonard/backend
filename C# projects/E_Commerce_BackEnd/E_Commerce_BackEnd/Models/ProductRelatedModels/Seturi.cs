using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace E_Commerce_BackEnd.Models.ProductRelatedModels;

public class Seturi : ICloneable
{
    // Attributes
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdSet { get; init; }
    
    [StringLength(50)]
    public string NumeSet { get; init; }
    
    [StringLength(50)]
    public string DescriereSet { get; init; }

    public  ICollection<AsociereSeturi>? SAsociereSeturi { get; }

    public Seturi()
    {
        
    }

    public Seturi(string numeSet, string descriereSet, ICollection<AsociereSeturi>? sAsociereSeturi)
    {
        NumeSet = numeSet;
        DescriereSet = descriereSet;
        SAsociereSeturi = sAsociereSeturi == null ? [] : new HashSet<AsociereSeturi>(sAsociereSeturi);
    }

    public Seturi(Seturi other)
    {
        ArgumentNullException.ThrowIfNull(other);
        
        IdSet = other.IdSet;
        NumeSet = other.NumeSet;
        DescriereSet = other.DescriereSet;
        SAsociereSeturi = new HashSet<AsociereSeturi>(other.SAsociereSeturi);
    }

    public object Clone()
    {
        return new Seturi(this);
    }
}