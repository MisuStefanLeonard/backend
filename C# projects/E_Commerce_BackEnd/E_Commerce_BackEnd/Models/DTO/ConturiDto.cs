using System.ComponentModel.DataAnnotations;

namespace E_Commerce_BackEnd.Models.DTO;

public class ConturiDto
{
    
    
    [StringLength(10)]
    public string? Nume { get; init; }
    
    [StringLength(20)]
    public string? Prenume { get; init; }
    
    public bool? Gen { get; init; }
    
    [StringLength(10)]
    public string? NrTelefon { get; init; }
    
    [StringLength(50)]
    public string? Email { get; init; }
    
    [StringLength(20)]
    public string? Username { get; init; }
    
}