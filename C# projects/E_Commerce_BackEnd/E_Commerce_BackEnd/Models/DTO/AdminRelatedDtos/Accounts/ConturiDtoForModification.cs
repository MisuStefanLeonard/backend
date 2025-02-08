using E_Commerce_BackEnd.Models.DTO.AdminRelatedDtos.OrdersDto;
using E_Commerce_BackEnd.Models.DTO.ClientOrdersDto;

namespace E_Commerce_BackEnd.Models.DTO.AdminRelatedDtos.Accounts;

public class ConturiDtoForModification
{
    public int IdContDto { get; init; }
    public string? NumeDto { get; set; }
    public string? PrenumeDto { get; set; }
    public bool ContActivDto { get; set; }
    public DateTime DataCreareDto { get; set; }
    public string? EmailDto { get; set; } 
    public string? UsernameDto { get; set; } 
    public string? RolDto { get; set; }
    public IList<AdreseDto> AdreseClient { get; init; } = new List<AdreseDto>();
    // public IList<OrdersDisplayDto> ComenziClient { get; init; } = new List<OrdersDisplayDto>();
    public IList<ClientOrder> ComenziClient { get; set; } = new List<ClientOrder>();
}