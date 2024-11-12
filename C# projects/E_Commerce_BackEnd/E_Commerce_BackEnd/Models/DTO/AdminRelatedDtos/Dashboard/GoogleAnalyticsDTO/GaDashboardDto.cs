namespace E_Commerce_BackEnd.Models.DTO.AdminRelatedDtos.Dashboard.GoogleAnalyticsDTO;

public class GaDashboardDto
{
    // General gathered data
    public int TotalActiveUsers { get; set; }
    public int TotalOneDayActiveUsers { get; set; }
    public int Total28DayActiveUsers { get; set; }
    public int TotalScreenPageViews { get; set; }
    public Dictionary<string, TrafficPerPage> UseriActiviPerPagina { get; set; } = new();
    
    // Real traffic data
    
    public int TotalActiveUsersReal { get; set; }
    public int TotalScreenPageViewsReal { get; set; }
    public Dictionary<string, TrafficPerPage> UseriActiviPerPaginaReal { get; set; } = new();

}