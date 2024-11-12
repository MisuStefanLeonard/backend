namespace E_Commerce_BackEnd.Models.DTO.AdminRelatedDtos.Dashboard.Helpers;

public class DashboardDataFromDateToDate
{
    // Data for a specific period (Between DeLa and PanaLa)
    public DateTime DeLa { get; set; } // Start date for the period-based data
    public DateTime PanaLa { get; set; } // End date for the period-based data
    public decimal TotalIncasariPePerioada { get; init; } // Total revenue in the given period
    public int TotalComenziPePerioada { get; init; } // Total orders in the given period
    public int TotalReturnariPePerioada { get; init; } // Total returns in the given period
    public decimal PretMediuComandaPePerioada { get; init; } // Average order price in the given period
    public Dictionary<string, int> TipuriClientiPePerioada { get; init; } = new(); // Customer types in the given period
    public Dictionary<string, int> TipuriComenziPePerioada { get; init; } = new(); // Order types in the given period
    public IList<TopVandutProdusDto> TopProduseVandutePePerioada { get; init; } = []; // Top products sold in the given period
    public Dictionary<string, decimal> VenitTotalPeCategoriiPePerioada { get; init; } = new(); // Revenue per category in the given period

    // Optional: You can even have more specific metrics for certain periods, such as daily, weekly, or monthly breakdowns
    public Dictionary<DateTime, decimal> VenitPeZiPePerioada { get; init; } = new(); // Revenue per day in the given period
}