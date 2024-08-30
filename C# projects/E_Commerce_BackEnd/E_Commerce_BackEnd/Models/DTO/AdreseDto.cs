using E_Commerce_BackEnd.Models.Enums;

namespace E_Commerce_BackEnd.Models.DTO;

public class AdreseDto
{
    public string? AliasDto { get; set; } //
    public TipAdrese TipAdresaDto { get; set; } // 
    public string? BlocDto { get; set; } //
    public string? NrBlocDto { get; set; } // 
    public string? StradaDto { get; set; } // NOT NULL
    public string? NrStradaDto { get; set; } // NOT NULL
    public string? OrasDto { get; set; } // NOT NULL
    public string? JudetDto { get; set; } // NOT NULL 
    public string? CodPostalDto { get; set; } // NOT NULL
    
    public bool IsDeletedDto { get; set; }
    
    // Facturare enum type attributes , null if not 
    public string? CifDto { get; set; } // null if adresa == "Livrare" enum type
    
    public string? NumeFirmaDto { get; set; } // null if adresa == "Livrare" enum type


}