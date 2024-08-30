using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace E_Commerce_BackEnd.Models.ProductRelatedModels;

public class CodCulori 
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdCodCuloare { get; init; }

    public string? CodCuloare { get; init; }

    public ICollection<Culori>? CoduriCulori { get; }

    public CodCulori()
    {
        
    }

    public CodCulori(string? codCuloare, ICollection<Culori>? coduriCulori)
    {
        CodCuloare = codCuloare;
        CoduriCulori = coduriCulori == null ? [] : new HashSet<Culori>(coduriCulori);
    }
    
    
}