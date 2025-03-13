using AutoMapper;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductOptionsDto;
using E_Commerce_BackEnd.Models.ProductRelatedModels;
using E_Commerce_BackEnd.UnitOfWork;

namespace E_Commerce_BackEnd.Services.Helpers.Resolvers;

public class ColorCodesResolver : IValueResolver<Culori,CuloriDto,string>
{
    private readonly IUnitOfWork _unitOfWork;

    public ColorCodesResolver(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public string Resolve(Culori source, CuloriDto destination, string? destMember, ResolutionContext context)
    {
        var colorCodesRepository = _unitOfWork.Repository<CodCulori>();
        

        var colorCode = colorCodesRepository
            .FindQueryable(cc => cc.IdCodCuloare == source.IdCodCuloare)
            .First();
        
        return colorCode.CodCuloare!;
    }
}