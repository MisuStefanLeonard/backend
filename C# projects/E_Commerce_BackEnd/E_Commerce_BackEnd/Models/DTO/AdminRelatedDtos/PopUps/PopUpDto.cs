using E_Commerce_BackEnd.Models.ProductRelatedModels.JSON_Models;

namespace E_Commerce_BackEnd.Models.DTO.AdminRelatedDtos.PopUps;

public class PopUpDto
{
    public int IdPopUp { get; init; }
    public Descriere DescriereJson { get; set; } = null!;
    public Nume TitluJson { get; set; } = null!;
    public int? IdVoucher { get; set; }
    public bool IsActive { get; set; }
}