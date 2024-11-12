using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace E_Commerce_BackEnd.Models.UserRelatedModels;

public class RememberUser
{
    public int IdSesiune { get; init; }
    
    public int IdCont { get; set; }
    public Conturi CurrentUserSession { get; set; } = null!;
    public string SessionToken { get; set; } = null!;
    public DateTime IssuedAt { get; set; }
    public DateTime ExpiresAt { get; set; }

}