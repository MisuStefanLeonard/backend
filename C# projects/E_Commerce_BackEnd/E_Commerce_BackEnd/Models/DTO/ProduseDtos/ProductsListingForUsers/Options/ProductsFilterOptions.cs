using System.Collections.Immutable;

namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.Options;

public class ProductsFilterOptions
{
    public ImmutableHashSet<string> FilterColors { get; init; } = null!;
    public ImmutableHashSet<string> FilterProductTypes { get; init; } = null!;
    public ImmutableHashSet<decimal> PricesRange { get; init; } = null!;
}