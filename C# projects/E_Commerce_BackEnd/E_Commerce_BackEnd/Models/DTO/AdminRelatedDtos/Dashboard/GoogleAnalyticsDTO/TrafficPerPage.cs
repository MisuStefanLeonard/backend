namespace E_Commerce_BackEnd.Models.DTO.AdminRelatedDtos.Dashboard.GoogleAnalyticsDTO;

public class TrafficPerPage
{
    public int TotalActiveUserPerPage { get; set; }
    public int? TotalActiveUser1DayPerPage { get; set; }
    public int? TotalActiveUser28DayPerPage { get; set; }
    public int TotalCurrentPageViews { get; set; }
    public HashSet<string> Cities { get; set; } = [];

}