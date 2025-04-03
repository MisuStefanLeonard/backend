namespace E_Commerce_BackEnd.Models.DTO;

public class ContactDetails
{
    public string? NrTelefon { get; init; }
    public string Email { get; init; } = null!;
    public string? NumarComanda { get; init; }
    public string MotivContact { get; init; } = null!;
    public string Descriere { get; init; } = null!;
    public string CaptchaToken { get; init; } = null!;

}