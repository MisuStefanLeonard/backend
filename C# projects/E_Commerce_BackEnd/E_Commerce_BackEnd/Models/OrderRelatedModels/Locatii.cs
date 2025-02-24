using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using E_Commerce_BackEnd.Models.UserRelatedModels;

namespace E_Commerce_BackEnd.Models.OrderRelatedModels;

public class Locatii 
{
    public int IdLocatie { get;  init; }
    public string? Oras { get;  init; }
    public string? Judet { get;  init; }
    public string? CodPostal { get;  init; }

    public ICollection<Adrese>? AdreseLocatii { get; }

    public Locatii()
    {
        
    }
    
    public Locatii(string oras, string judet, string codPostal , ICollection<Adrese>? adreseLocatii)
    {
        Oras = oras;
        Judet = judet;
        CodPostal = codPostal;
        AdreseLocatii = adreseLocatii == null ? [] : new HashSet<Adrese>(adreseLocatii);
    }
    
   
    
}