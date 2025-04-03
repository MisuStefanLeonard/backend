namespace E_Commerce_BackEnd.Models.DTO.AdminRelatedDtos.Dashboard;

public class DashboardGeneralData
{
    public decimal TotalIncasariGeneral { get; init; } // rezolvat
    public Dictionary<string ,Dictionary<string,Dictionary<string,int>>> TipuriProduse { get; init; } = new(); // rezolvat
    public Dictionary<string, int> TotalProduse { get; init; } = new();
    public decimal PretMediuComandaGeneral { get; init; } // rezolvat
    public Dictionary<string, int> TipuriComenziGeneral { get; init; } = new(); // rezolvat
    public Dictionary<string, decimal> VenitTotalPeProdus { get; init; } = new(); // rezolvat
    public IList<TopVandutProdusDto> TopProduseVanduteGeneral { get; init; } = []; // Top products sold overall
    public IList<OneDayRevenueDto>? OneDayRevenue { get; init; } = null!;
    public IList<OneMonthRevenueDto> OneMonthRevenue { get; init; } = [];
    public IList<OneYearRevenueDto> OneYearRevenue { get; init; } = [];

}
// DTO for Top Sold Products
public class TopVandutProdusDto
{
    public string CodProdus { get; init; } = null!;
    public int NrVanzari { get; init; }
    public decimal? VenitTotal { get; init; }
}
public class OneDayRevenueDto
{
    public decimal Revenue { get; init; }
    public int OrdersCount { get; init; }
    public int OrderHourTime { get; init; }
}
public class OneMonthRevenueDto
{
    public decimal Revenue { get; init; }
    public int OrdersCount { get; init; }
    public DateTime Day { get; init; }
}

public class OneYearRevenueDto
{
    public decimal Revenue { get; init; }
    public int OrdersCount { get; init; }
    public string Month { get; init; } = null!;
}
