namespace E_Commerce_BackEnd.Models.DTO.AdminRelatedDtos.ProductBillingDto;

public class ProductBilling
{
    public string name { get; init; } = null!;
    public string code { get; init; } = null!;
    public string productDescription { get; set; } = null!;
    public string translatedName { get; init; } = null!;
    public string translatedMeasuringUnit { get; init; } = null!;
    public bool isDiscount { get; init; }= false;
    public string measuringUnitName { get; init; }= null!;
    public string currency { get; init; } = null!;
    public double quantity { get; init; }
    public double price { get; init; }
    public bool isTaxIncluded { get; init; } = true;
    public double exchangeRate { get; init; } = 5;
    public string taxName { get; init; } = "Normala";
    public double taxPercentage { get; init; } = 19;
    public bool isService { get; init; } = false;
    
}