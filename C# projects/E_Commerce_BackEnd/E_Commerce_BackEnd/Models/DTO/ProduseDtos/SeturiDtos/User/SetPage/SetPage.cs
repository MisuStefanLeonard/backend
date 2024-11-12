namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.SeturiDtos.User.SetPage;

public class SetPage
{
    public string NumeSetDto { get; init; } = null!;
    public decimal PretSetDto { get; init; }
    public decimal PretRedusSetDto { get; init; }
    public string DescriereSetDto { get; init; } = null!;

    public IList<ProductOnSet> ProdusePeSet { get; init; } = new List<ProductOnSet>();

}