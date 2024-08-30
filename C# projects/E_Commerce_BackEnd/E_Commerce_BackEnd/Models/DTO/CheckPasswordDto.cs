namespace E_Commerce_BackEnd.Models.DTO;

public class CheckPasswordDto
{
    public string HashedPasswordDtoProp { get; set; } = null!;
    public string TokenProp { get; set; } = null!;

}