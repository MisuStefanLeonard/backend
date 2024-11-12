namespace E_Commerce_BackEnd.Models.DTO.AdminRelatedDtos.Dashboard;

public class DashboardGeneralData
{
    public decimal TotalIncasariGeneral { get; init; } // rezolvat
    public Dictionary<string ,Dictionary<string,Dictionary<string,int>>> TipuriProduse { get; init; } = new(); // rezolvat
    public Dictionary<string, int> TotalProduse { get; init; } = new();
    public decimal PretMediuComandaGeneral { get; init; } // rezolvat
    public Dictionary<string, int> TipuriClientiGeneral { get; init; } = new(); // rezolvat
    public Dictionary<string, int> TipuriComenziGeneral { get; init; } = new(); // rezolvat
    public Dictionary<string, decimal> VenitTotalPeProdus { get; init; } = new(); // rezolvat
    public IList<TopVandutProdusDto> TopProduseVanduteGeneral { get; init; } = []; // Top products sold overall

}

// DTO for Top Sold Products
public class TopVandutProdusDto
{
    public string CodProdus { get; init; } = null!;
    public int NrVanzari { get; init; }
    public decimal? VenitTotal { get; init; }
}
