using AutoMapper;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos;
using E_Commerce_BackEnd.Models.ProductRelatedModels;
using E_Commerce_BackEnd.UnitOfWork;

namespace E_Commerce_BackEnd.Services.Helpers.Resolvers;

public class ProducatorValueResolver : IValueResolver<ProduseDtoForAdminModification, Produse,int?>
{
    private readonly IUnitOfWork _unitOfWork;

    public ProducatorValueResolver(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public int? Resolve(ProduseDtoForAdminModification source, Produse destination, int? destMember, ResolutionContext context)
    {
        if (source.NumeProducatorDto is null)
        {
            return null;
        }
        
        var producator = _unitOfWork.Repository<Producatori>()
            .GetSimpleQueryable()
            .FirstOrDefault(prod => prod.NumeProducator == source.NumeProducatorDto);

        if (producator == null)
        {
            return null;
        }

        return producator.IdProducator;
    }
}