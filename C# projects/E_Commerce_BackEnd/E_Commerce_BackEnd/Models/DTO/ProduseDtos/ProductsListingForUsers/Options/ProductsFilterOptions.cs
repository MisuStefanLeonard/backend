using System.Collections.Immutable;
using E_Commerce_BackEnd.Models.ProductRelatedModels.JSON_Models;

namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.Options;

public class ProductsFilterOptions
{
    public ImmutableHashSet<string> FilterColors { get; init; } = null!;
    public ImmutableHashSet<string> FilterProductTypes { get; init; } = null!;
    public ImmutableHashSet<Culoare> FilterColorsJson { get; init; } = null!;
    public ImmutableHashSet<TipProdus> FilterProductTypesJson { get; init; } = null!;
    public ImmutableHashSet<Categorie> FilterProductCategoriesJson { get; init; } = null!;
    public ImmutableHashSet<decimal> PricesRange { get; init; } = null!;
}