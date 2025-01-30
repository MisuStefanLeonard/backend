using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using E_Commerce_BackEnd.Models.ProductRelatedModels;

namespace E_Commerce_BackEnd.Models.UserRelatedModels;
public  class Conturi 
{
    public int IdCont { get; init; } //
    [StringLength(10)]
    public string? Nume { get; init; } // 
    [StringLength(20)]
    public string? Prenume { get; init; } // 
    public bool? Gen { get; init; } //
    [StringLength(10)]
    public string? NrTelefon { get; init; } // 
    [StringLength(15)]
    public string? Username { get; init; } //  username
    [StringLength(50)]
    public string? Email { get; init; } // email
    [StringLength(150)]
    public string? Parola { get; init; } // parola
    public  DateTime? DataCreare { get; init; }
    [StringLength(100)]
    public string CodActivare { get; set; }
    public bool Verificat { get; set; }
    public bool IsGuest { get; set; }
    [StringLength(15)]
    public string Rol { get; init; }
    public DateTime OraLinkConfirmare { get; set; }
    public ICollection<Adrese>? AdreseConturi { get; set; } = new HashSet<Adrese>();
    public ICollection<CosCumparaturi>? ProduseInCosPeCont { get; } = new HashSet<CosCumparaturi>();
    public ICollection<Reviews>? ReviewsProduse { get; } = new HashSet<Reviews>();
    public RememberUser? RememberUserSession { get; set; } 
    public Conturi()
    {
        
    }

    public Conturi(string? nume, string? prenume, bool? gen, string? nrTelefon, string username, 
                   string email, string? parola, DateTime? dataCreare, 
                   string codActivare, bool verificat, string rol ,DateTime oraLinkConfirmare,ICollection<Adrese>? adreseConturi)
    {
        Nume = nume;
        Prenume = prenume;
        Gen = gen;
        NrTelefon = nrTelefon;
        Username = username;
        Email = email;
        Parola = parola;
        DataCreare = dataCreare;
        CodActivare = codActivare;
        Verificat = verificat;
        Rol = rol;
        OraLinkConfirmare = oraLinkConfirmare;
        AdreseConturi = adreseConturi == null ? new HashSet<Adrese>() : new HashSet<Adrese>(adreseConturi);
       
    }

    
}