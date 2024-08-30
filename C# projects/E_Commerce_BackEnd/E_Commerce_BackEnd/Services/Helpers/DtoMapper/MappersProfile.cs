using System.Security.Cryptography;
using AutoMapper;
using E_Commerce_BackEnd.Models.DTO;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos;
using E_Commerce_BackEnd.Models.ProductRelatedModels;
using E_Commerce_BackEnd.Models.UserRelatedModels;
using E_Commerce_BackEnd.Services.Helpers.Resolvers;

namespace E_Commerce_BackEnd.Services.Helpers.DtoMapper;

public class MappersProfile : Profile
{

    public MappersProfile()
    {
        CreateMap<Conturi, ConturiDto>()
            .ForMember(dest => dest.Nume, from => from.MapFrom(src => src.Nume))
            .ForMember(dest => dest.Prenume, from => from.MapFrom(src => src.Prenume))
            .ForMember(dest => dest.Email, from => from.MapFrom(src => src.Email))
            .ForMember(dest => dest.Gen, from => from.MapFrom(src => src.Gen))
            .ForMember(dest => dest.NrTelefon, from => from.MapFrom(src => src.NrTelefon))
            .ForMember(dest => dest.Username, from => from.MapFrom(src => src.Username));

        CreateMap<Conturi, Conturi>()
            .ForMember(dest => dest.IdCont, opt => opt.Ignore())
            .ForMember(dest => dest.Nume, opt => opt.MapFrom(src => src.Nume))
            .ForMember(dest => dest.Prenume, opt => opt.MapFrom(src => src.Prenume))
            .ForMember(dest => dest.Gen, opt => opt.MapFrom(src => src.Gen))
            .ForMember(dest => dest.NrTelefon, opt => opt.MapFrom(src => src.NrTelefon))
            .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Username))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.Parola, opt => opt.MapFrom(src => src.Parola))
            .ForMember(dest => dest.DataCreare, opt => opt.MapFrom(src => src.DataCreare))
            .ForMember(dest => dest.CodActivare, opt => opt.MapFrom(src => src.CodActivare))
            .ForMember(dest => dest.Verificat, opt => opt.MapFrom(src => src.Verificat))
            .ForMember(dest => dest.Rol, opt => opt.MapFrom(src => src.Rol))
            .ForMember(dest => dest.OraLinkConfirmare, opt => opt.MapFrom(src => src.OraLinkConfirmare))
            .ForMember(dest => dest.AdreseConturi, opt => opt.MapFrom(src => src.AdreseConturi))
            .BeforeMap((src, dest) =>
            {
                if (dest.AdreseConturi == null)
                {
                    dest.AdreseConturi = new HashSet<Adrese>();
                }
            });
            
           

        CreateMap<LoginDto, Conturi>()
            .ForMember(dest => dest.Nume, from => from.MapFrom(src => src.NumeProp))
            .ForMember(dest => dest.Parola, from => from.MapFrom(src => src.ParolaProp));


        CreateMap<Adrese, AdreseDto>()
            .ForMember(dest => dest.AliasDto, 
                opt => opt.MapFrom(src => src.Alias))
            .ForMember(dest => dest.TipAdresaDto, 
                opt => opt.MapFrom(src => src.TipAdresa))
            .ForMember(dest => dest.BlocDto, 
                opt => opt.MapFrom(src => src.Bloc))
            .ForMember(dest => dest.NrBlocDto, 
                opt => opt.MapFrom(src => src.NrBloc))
            .ForMember(dest => dest.StradaDto, 
                opt => opt.MapFrom(src => src.Strada))
            .ForMember(dest => dest.NrStradaDto, 
                opt => opt.MapFrom(src => src.NrStrada))
            .ForMember(dest => dest.OrasDto, 
                opt => opt.MapFrom(src => src.Locatie.Oras))
            .ForMember(dest => dest.JudetDto, 
                opt => opt.MapFrom(src => src.Locatie.Judet))
            .ForMember(dest => dest.CodPostalDto, 
                opt => opt.MapFrom(src => src.Locatie.CodPostal))
            .ForMember(dest => dest.CifDto, 
                opt => opt.MapFrom(src => src.DetaliiFacturi!.FirstOrDefault()!.Cif))
            .ForMember(dest => dest.NumeFirmaDto,
                opt => opt.MapFrom(src => src.DetaliiFacturi!.FirstOrDefault()!.NumeFirma))
            .ForMember(dest => dest.IsDeletedDto, opt => opt.MapFrom(src => src.IsDeleted));

        CreateMap<Produse, ProduseDto>()
            .ForMember(dest => dest.CodProdusDto, opt => opt.MapFrom(src => src.CodProdus))
            .ForMember(dest => dest.DescriereDto, opt => opt.MapFrom(src => src.Descriere))
            .ForMember(dest => dest.NumeProdusDto, opt => opt.MapFrom(src => src.NumeProdus))
            .ForMember(dest => dest.CompozitieDto, opt => opt.MapFrom(src => src.Compozitie))
            .ForMember(dest => dest.TvaDto, opt => opt.MapFrom(src => src.Tva))
            .ForMember(dest => dest.IngrijireDto, opt => opt.MapFrom(src => src.Ingrijire))
            .ForMember(dest => dest.GreutateDto, opt => opt.MapFrom(src => src.Greutate))
            .ForMember(dest => dest.FataReversibilaDto, opt => opt.MapFrom(src => src.FataReversibila))
            .ForMember(dest => dest.StocDto, opt => opt.MapFrom(src => src.Stoc))
            .ForMember(dest => dest.IsDeletedDto, opt => opt.MapFrom(src => src.IsDeleted))
            .ForMember(dest => dest.ActivInMagazinDto, opt => opt.MapFrom(src => src.ActivInMagazin))
            .ForMember(dest => dest.TipProdusDto, opt => opt.MapFrom(src => src.TipulProdusului));


        CreateMap<Produse, Produse>()
            .ForMember(dest => dest.CodProdus, opt => opt.MapFrom(src => src.CodProdus))
            .ForMember(dest => dest.Descriere, opt => opt.MapFrom(src => src.Descriere))
            .ForMember(dest => dest.NumeProdus, opt => opt.MapFrom(src => src.NumeProdus))
            .ForMember(dest => dest.Compozitie, opt => opt.MapFrom(src => src.Compozitie))
            .ForMember(dest => dest.Tva, opt => opt.MapFrom(src => src.Tva))
            .ForMember(dest => dest.Ingrijire, opt => opt.MapFrom(src => src.Ingrijire))
            .ForMember(dest => dest.Greutate, opt => opt.MapFrom(src => src.Greutate))
            .ForMember(dest => dest.FataReversibila, opt => opt.MapFrom(src => src.FataReversibila))
            .ForMember(dest => dest.Stoc, opt => opt.MapFrom(src => src.Stoc))
            .ForMember(dest => dest.IdProducator, opt => opt.MapFrom(src => src.IdProducator))
            .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => src.IsDeleted))
            .ForMember(dest => dest.ActivInMagazin, opt => opt.MapFrom(src => src.ActivInMagazin))
            .ForMember(dest => dest.TipulProdusului, opt => opt.MapFrom(src => src.TipulProdusului))
            .ForMember(dest => dest.IdProdus, opt => opt.Ignore());

        CreateMap<ProduseDtoForAdminModification, Produse>()
            .ForMember(dest => dest.Compozitie, opt => opt.MapFrom(src => src.CompozitieDto))
            .ForMember(dest => dest.Descriere, opt => opt.MapFrom(src => src.DescriereDto))
            .ForMember(dest => dest.IdProducator, opt => opt.MapFrom<ProducatorValueResolver>())
            .ForMember(dest => dest.NumeProdus, opt => opt.MapFrom(src => src.NumeProdusDto))
            .ForMember(dest => dest.Tva, opt => opt.MapFrom(src => src.TvaDto))
            .ForMember(dest => dest.Ingrijire, opt => opt.MapFrom(src => src.IngrijireDto))
            .ForMember(dest => dest.Greutate, opt => opt.MapFrom(src => src.GreutateDto))
            .ForMember(dest => dest.FataReversibila, opt => opt.MapFrom(src => src.FataReversibilaDto))
            .ForMember(dest => dest.Stoc, opt => opt.MapFrom(src => src.StocDto))
            .ForMember(dest => dest.ActivInMagazin, opt => opt.MapFrom(src => src.ActivInMagazinDto))
            .ForMember(dest => dest.TipulProdusului, opt => opt.MapFrom(src => src.TipulProdusuluiDto));
        









    }
}