using AutoMapper;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductOptionsDto;
using E_Commerce_BackEnd.Models.ProductRelatedModels;
using E_Commerce_BackEnd.UnitOfWork;

namespace E_Commerce_BackEnd.Services.Helpers.Resolvers;

public class DimensionPriceResolver : IValueResolver<Dimensiuni, DimensiuniDto, decimal>
{
    private readonly IUnitOfWork _unitOfWork;

    public DimensionPriceResolver(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public decimal Resolve(Dimensiuni source, DimensiuniDto destination, decimal destMember, ResolutionContext context)
    {
        var dimensionOnProductRepository = _unitOfWork.Repository<ProduseCuDimensiuni>();
        

        if (context.Items["Product"] is not Produse product)
        {
            // Handle case when the product is not available
            return 0;
        }

        // Find the price for the dimension and product combination
        var findPriceForTheDimension = dimensionOnProductRepository
            .FindQueryable(pd => pd.IdDimensiune == source.IdDimensiune && pd.IdProdus == product.IdProdus)
            .FirstOrDefault();
        

        // Return the price if found, or default to 0
        return findPriceForTheDimension?.Pret ?? 0;
    }
}
