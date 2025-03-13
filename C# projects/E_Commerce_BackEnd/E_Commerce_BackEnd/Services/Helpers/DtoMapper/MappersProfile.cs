using AutoMapper;
using E_Commerce_BackEnd.Models.DTO;
using E_Commerce_BackEnd.Models.DTO.AdminRelatedDtos.Accounts;
using E_Commerce_BackEnd.Models.DTO.AdminRelatedDtos.OrdersDto;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.InelePrindereDtos;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ManopereDto.ManoperaModification;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductOptionsDto;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.ProductPage;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.ProductPage.OptionsForCurtain;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.SeturiDtos;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.TipuriGalerieDtos;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.TipuriLinieDtos;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.VouchereDtos;
using E_Commerce_BackEnd.Models.Enums;
using E_Commerce_BackEnd.Models.OrderRelatedModels;
using E_Commerce_BackEnd.Models.ProductRelatedModels;
using E_Commerce_BackEnd.Models.ProductVouchersModels;
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
            .BeforeMap((src, dest) => { dest.AdreseConturi ??= new HashSet<Adrese>(); });
            
           

        CreateMap<LoginDto, Conturi>()
            .ForMember(dest => dest.Nume, from => from.MapFrom(src => src.NumeProp))
            .ForMember(dest => dest.Parola, from => from.MapFrom(src => src.ParolaProp));


        CreateMap<Adrese, AdreseDto>()
            .ForMember(dest => dest.IdAdresa, 
                opt => opt.MapFrom(src => src.IdAdresa))
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
                opt => opt.MapFrom(src => src.DetaliuFactura!.Cif))
            .ForMember(dest => dest.NumeFirmaDto,
                opt => opt.MapFrom(src => src.DetaliuFactura!.NumeFirma))
            .ForMember(dest => dest.IsDeletedDto, 
                opt => opt.MapFrom(src => src.IsDeleted));


        CreateMap<AdreseDto, Adrese>()
            .ForMember(dest => dest.Alias,
                opt => opt.MapFrom(src => src.AliasDto))
            .ForMember(dest => dest.TipAdresa,
                opt => opt.MapFrom(src => src.TipAdresaDto))
            .ForMember(dest => dest.Bloc,
                opt => opt.MapFrom(src => src.BlocDto))
            .ForMember(dest => dest.NrBloc,
                opt => opt.MapFrom(src => src.NrBlocDto))
            .ForMember(dest => dest.Strada,
                opt => opt.MapFrom(src => src.StradaDto))
            .ForMember(dest => dest.NrStrada,
                opt => opt.MapFrom(src => src.NrStradaDto))
            .ForMember(dest => dest.IsDeleted,
                opt => opt.MapFrom(src => src.IsDeletedDto));
        
        CreateMap<AdreseDto, AdreseDto>()
            .ForMember(dest => dest.AliasDto,
                opt => opt.MapFrom(src => src.AliasDto))
            .ForMember(dest => dest.TipAdresaDto,
                opt => opt.MapFrom(src => src.TipAdresaDto))
            .ForMember(dest => dest.BlocDto,
                opt => opt.MapFrom(src => src.BlocDto))
            .ForMember(dest => dest.NrBlocDto,
                opt => opt.MapFrom(src => src.NrBlocDto))
            .ForMember(dest => dest.StradaDto,
                opt => opt.MapFrom(src => src.StradaDto))
            .ForMember(dest => dest.NrStradaDto,
                opt => opt.MapFrom(src => src.NrStradaDto))
            .ForMember(dest => dest.IsDeletedDto,
                opt => opt.MapFrom(src => src.IsDeletedDto));
        

        CreateMap<Produse, ProduseDto>()
            .ForMember(dest => dest.CodProdusDto, opt => opt.MapFrom(src => src.CodProdus))
            // .ForMember(dest => dest.DescriereDto, opt => opt.MapFrom(src => src.Descriere))
            .ForMember(dest => dest.DescriereJsonDto, opt => opt.MapFrom(src => src.DescriereJson))
            // .ForMember(dest => dest.NumeProdusDto, opt => opt.MapFrom(src => src.NumeProdus))
            .ForMember(dest => dest.NumeProdusJsonDto, opt => opt.MapFrom(src => src.NumeProdusJson))
            // .ForMember(dest => dest.CompozitieDto, opt => opt.MapFrom(src => src.Compozitie))
            .ForMember(dest => dest.CompozitieJsonDto, opt => opt.MapFrom(src => src.CompozitieJson))
            .ForMember(dest => dest.TvaDto, opt => opt.MapFrom(src => src.Tva))
            // .ForMember(dest => dest.IngrijireDto, opt => opt.MapFrom(src => src.Ingrijire))
            .ForMember(dest => dest.IngrijireJsonDto, opt => opt.MapFrom(src => src.IngrijireJson))
            .ForMember(dest => dest.FataReversibilaDto, opt => opt.MapFrom(src => src.FataReversibila))
            .ForMember(dest => dest.StocDto, opt => opt.MapFrom(src => src.Stoc))
            .ForMember(dest => dest.InaltimeMaximaDto , opt => opt.MapFrom(src => src.InaltimeMaxima))
            .ForMember(dest => dest.IsDeletedDto, opt => opt.MapFrom(src => src.IsDeleted))
            .ForMember(dest => dest.ActivInMagazinDto, opt => opt.MapFrom(src => src.ActivInMagazin))
            // .ForMember(dest => dest.TipProdusDto, opt => opt.MapFrom(src => src.TipulProdusului))
            .ForMember(dest => dest.TipProdusJsonDto, opt => opt.MapFrom(src => src.TipulProdusuluiJson))
            .ForMember(dest => dest.ActiveazaInNoutati, opt => opt.MapFrom(src => src.AfiseazaInNoutati))
            .ForMember(dest => dest.ProdusLimitatDto, opt => opt.MapFrom(src => src.ProdusLimitat));




        CreateMap<Produse, Produse>()
            .ForMember(dest => dest.CodProdus, opt => opt.MapFrom(src => src.CodProdus))
            // .ForMember(dest => dest.Descriere, opt => opt.MapFrom(src => src.Descriere))
            .ForMember(dest => dest.DescriereJson, opt => opt.MapFrom(src => src.DescriereJson))
            // .ForMember(dest => dest.NumeProdus, opt => opt.MapFrom(src => src.NumeProdus))
            .ForMember(dest => dest.NumeProdusJson, opt => opt.MapFrom(src => src.NumeProdusJson))
            // .ForMember(dest => dest.Compozitie, opt => opt.MapFrom(src => src.Compozitie))
            .ForMember(dest => dest.CompozitieJson, opt => opt.MapFrom(src => src.CompozitieJson))
            .ForMember(dest => dest.Tva, opt => opt.MapFrom(src => src.Tva))
            // .ForMember(dest => dest.Ingrijire, opt => opt.MapFrom(src => src.Ingrijire))
            .ForMember(dest => dest.IngrijireJson, opt => opt.MapFrom(src => src.IngrijireJson))
            .ForMember(dest => dest.FataReversibila, opt => opt.MapFrom(src => src.FataReversibila))
            .ForMember(dest => dest.Stoc, opt => opt.MapFrom(src => src.Stoc))
            .ForMember(dest => dest.IdProducator, opt => opt.MapFrom(src => src.IdProducator))
            .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => src.IsDeleted))
            .ForMember(dest => dest.ActivInMagazin, opt => opt.MapFrom(src => src.ActivInMagazin))
            .ForMember(dest => dest.InaltimeMaxima , opt => opt.MapFrom(src => src.InaltimeMaxima))
            .ForMember(dest => dest.PretDeBaza, opt => opt.MapFrom(src => src.PretDeBaza))
            // .ForMember(dest => dest.TipulProdusului, opt => opt.MapFrom(src => src.TipulProdusului))
            .ForMember(dest => dest.TipulProdusuluiJson, opt => opt.MapFrom(src => src.TipulProdusuluiJson))
            .ForMember(dest => dest.AfiseazaInNoutati, opt => opt.MapFrom(src => src.AfiseazaInNoutati))
            .ForMember(dest => dest.ProdusLimitat, opt => opt.MapFrom(src => src.ProdusLimitat))
            .ForMember(dest => dest.IdProdus, opt => opt.Ignore());

        CreateMap<ProduseDtoForAdminModification, Produse>()
            // .ForMember(dest => dest.Compozitie, opt => opt.MapFrom(src => src.CompozitieDto))
            .ForMember(dest => dest.CompozitieJson, opt => opt.MapFrom(src => src.CompozitieJsonDto))
            // .ForMember(dest => dest.Descriere, opt => opt.MapFrom(src => src.DescriereDto))
            .ForMember(dest => dest.DescriereJson, opt => opt.MapFrom(src => src.DescriereJsonDto))
            .ForMember(dest => dest.IdProducator, opt => opt.MapFrom<ProducatorValueResolver>())
            // .ForMember(dest => dest.NumeProdus, opt => opt.MapFrom(src => src.NumeProdusDto))
            .ForMember(dest => dest.NumeProdusJson, opt => opt.MapFrom(src => src.NumeProdusJsonDto))
            .ForMember(dest => dest.Tva, opt => opt.MapFrom(src => src.TvaDto))
            // .ForMember(dest => dest.Ingrijire, opt => opt.MapFrom(src => src.IngrijireDto))
            .ForMember(dest => dest.IngrijireJson, opt => opt.MapFrom(src => src.IngrijireJsonDto))
            .ForMember(dest => dest.FataReversibila, opt => opt.MapFrom(src => src.FataReversibilaDto))
            .ForMember(dest => dest.Stoc, opt => opt.MapFrom(src => src.StocDto))
            .ForMember(dest => dest.ActivInMagazin, opt => opt.MapFrom(src => src.ActivInMagazinDto))
            .ForMember(dest => dest.PretDeBaza, opt => opt.MapFrom(src => src.PretBazaDto))
            .ForMember(dest => dest.InaltimeMaxima , opt => opt.MapFrom(src => src.InaltimeMaximaDto))
            .ForMember(dest => dest.PretDeBazaRedus, opt => opt.MapFrom(src => src.PretBazaRedusDto))
            // .ForMember(dest => dest.TipulProdusului, opt => opt.MapFrom(src => src.TipulProdusuluiDto))
            .ForMember(dest => dest.TipulProdusuluiJson, opt => opt.MapFrom(src => src.TipulProdusuluiJsonDto))
            .ForMember(dest => dest.AfiseazaInNoutati, opt => opt.MapFrom(src => src.AfiseazaInNoutatiDto))
            .ForMember(dest => dest.ProdusLimitat, opt => opt.MapFrom(src => src.ProdusLimitatDto));
        
        // de create mapperul pentru producrForSetDto
        // de implementat functia de getsetpage (vezi !!!!)
        CreateMap<Produse, ProductForSetDto >()
            .ForMember(dest => dest.IdProdusDto, opt => opt.MapFrom(src => src.IdProdus))
            .ForMember(dest => dest.CodProdusDto, opt => opt.MapFrom(src => src.CodProdus))
            // .ForMember(dest => dest.NumeProdusDto, opt => opt.MapFrom(src => src.NumeProdus))
            .ForMember(dest => dest.NumeProdusJsonDto, opt => opt.MapFrom(src => src.NumeProdusJson))
            .ForMember(dest => dest.ActivInMagazinDto, opt => opt.MapFrom(src => src.ActivInMagazin))
            .ForMember(dest => dest.PretBazaDto, opt => opt.MapFrom(src => src.PretDeBaza))
            // .ForMember(dest => dest.TipProdusDto, opt => opt.MapFrom(src => src.TipulProdusului))
            .ForMember(dest => dest.TipProdusJsonDto, opt => opt.MapFrom(src => src.TipulProdusuluiJson));

        
        CreateMap<Culori, CuloriDto>()
            // .ForMember(dest => dest.NumeCuloareDto, opt => opt.MapFrom(src => src.NumeCuloare))
            .ForMember(dest => dest.NumeCuloareJsonDto, opt => opt.MapFrom(src => src.NumeCuloareJson))
            .ForMember(dest => dest.CodCuloareDto, opt => opt.MapFrom<ColorCodesResolver>());
        
        // de adaugat un resolver pentru generarea imaginilor din bucket
        CreateMap<Imagini, ImagesDto>()
            .ForMember(dest => dest.CaleImagineDto, opt => opt.MapFrom(src => src.CaleImagine))
            .ForMember(dest => dest.FisierInBucketDto, opt => opt.MapFrom(src => src.FisierInBucket));


        CreateMap<Dimensiuni, DimensiuniDto>()
            .ForMember(dest => dest.RecomandarePat, opt => opt.MapFrom(src => src.RecomandarePat))
            .ForMember(dest => dest.LungimeDto, opt => opt.MapFrom(src => src.Lungime))
            .ForMember(dest => dest.LatimeDto, opt => opt.MapFrom(src => src.Latime))
            .ForMember(dest => dest.PretDto, opt =>
                opt.MapFrom<DimensionPriceResolver>())
            .ForMember(dest => dest.PretRedusDto, opt =>
                opt.MapFrom<DiscountDimensionPriceResolver>());
            

        CreateMap<IneleDto, InelePrindere>()
            // .ForMember(dest => dest.CuloareInel, opt => opt.MapFrom(src => src.CuloareInelDto))
            .ForMember(dest => dest.CuloareInelJson, opt => opt.MapFrom(src => src.CuloareInelJsonDto))
            .ForMember(dest => dest.CaleRelativa, opt => opt.MapFrom(src => src.CaleRelativa));

        CreateMap<InelePrindere, IneleDisplayDto>()
            // .ForMember(dest => dest.CuloareInelDto, opt => opt.MapFrom(src => src.CuloareInel))
            .ForMember(dest => dest.CuloareInelJsonDto, opt => opt.MapFrom(src => src.CuloareInelJson))
            .ForMember(dest => dest.IdInelDto, opt => opt.MapFrom(src => src.IdInel))
            .ForMember(dest => dest.EncodedIdInelDto, opt => opt.MapFrom(src => src.EncodedIdInel));

        CreateMap<TipuriGalerieDto, TipuriGalerie>()
            // .ForMember(dest => dest.NumeTipGalerie, opt => opt.MapFrom(src => src.NumeTipGalerieDto))
            .ForMember(dest => dest.NumeTipGalerieJson, opt => opt.MapFrom(src => src.NumeTipGalerieJsonDto))
            .ForMember(dest => dest.PretTipGalerie, opt => opt.MapFrom(src => src.PretTipGalerieDto))
            .ForMember(dest => dest.CaleRelativa, opt => opt.MapFrom(src => src.CaleRelativa))
            .ForMember(dest => dest.IncretireRejansa, opt => opt.MapFrom(src => src.IncretireDto))
            .ForMember(dest => dest.SePrindeCuInele, opt => opt.MapFrom(src => src.SePrindeCuIneleDto));


        CreateMap<TipuriGalerie, TipuriGalerieDisplayDto>()
            // .ForMember(dest => dest.NumeTipGalerieDto, opt => opt.MapFrom(src => src.NumeTipGalerie))
            .ForMember(dest => dest.NumeTipGalerieJsonDto, opt => opt.MapFrom(src => src.NumeTipGalerieJson))
            .ForMember(dest => dest.IdTipGalerieDto, opt => opt.MapFrom(src => src.IdTipGalerie))
            .ForMember(dest => dest.EncodedIdTipGalerieDto, opt => opt.MapFrom(src => src.EncodedIdTipGalerie))
            .ForMember(dest => dest.PretTipGalerieDto, opt => opt.MapFrom(src => src.PretTipGalerie))
            .ForMember(dest => dest.IncretireDto, opt => opt.MapFrom(src => src.IncretireRejansa))
            .ForMember(dest => dest.SePrindeCuIneleDto, opt => opt.MapFrom(src => src.SePrindeCuInele));

        
        CreateMap<TipuriLinieDto, TipuriLinie>()
            // .ForMember(dest => dest.NumeTipLinie, opt => opt.MapFrom(src => src.NumeTipLinieDto))
            .ForMember(dest => dest.NumeTipLinieJson, opt => opt.MapFrom(src => src.NumeTipLinieJsonDto))
            .ForMember(dest => dest.PretPeTipLinie, opt => opt.MapFrom(src => src.PretPeTipLinieDto))
            .ForMember(dest => dest.CaleRelativa, opt => opt.MapFrom(src => src.CaleRelativa));

        CreateMap<TipuriLinie, TipuriLinieDisplayDto>()
            // .ForMember(dest => dest.NumeTipLinieDto, opt => opt.MapFrom(src => src.NumeTipLinie))
            .ForMember(dest => dest.NumeTipLinieJsonDto, opt => opt.MapFrom(src => src.NumeTipLinieJson))
            .ForMember(dest => dest.IdTipLinieDto, opt => opt.MapFrom(src => src.IdTipLinie))
            .ForMember(dest => dest.EncodedIdTipLinie, opt => opt.MapFrom(src => src.EncodedIdTipLinie))
            .ForMember(dest => dest.PretPeTipLinieDto, opt => opt.MapFrom(src => src.PretPeTipLinie));
        
        CreateMap<VouchereDto, Vouchere>()
            .ForMember(dest => dest.Reducere, opt => opt.MapFrom(src => src.ReducereDto / 100))
            .ForMember(dest => dest.DataExpirare, opt => opt.MapFrom(src => src.DataExpirareDto.Date))
            .ForMember(dest => dest.CodVoucher, opt => opt.MapFrom(src => src.CodVoucherDto.ToUpper()));
        
        
        CreateMap<Vouchere, VouchereDto>()
            .ForMember(dest => dest.ReducereDto, opt => opt.MapFrom(src => src.Reducere * 100))
            .ForMember(dest => dest.DataExpirareDto, opt => opt.MapFrom(src => src.DataExpirare.Date))
            .ForMember(dest => dest.CodVoucherDto, opt => opt.MapFrom(src => src.CodVoucher.ToUpper()));

        CreateMap<Vouchere, VouchereDisplayDto>()
            .ForMember(dest => dest.ReducereDto, opt => opt.MapFrom(src => src.Reducere * 100))
            .ForMember(dest => dest.IdVoucherDto, opt => opt.MapFrom(src => src.IdVoucher))
            .ForMember(dest => dest.DataExpirareDto, opt => opt.MapFrom(src => src.DataExpirare))
            .ForMember(dest => dest.Expirat , opt => opt.MapFrom(src =>
                src.DataExpirare < DateTime.UtcNow.Date ? "expirat" : "neexpirat"
            ))
            .ForMember(dest => dest.CodVoucherDto, opt => opt.MapFrom(src => src.CodVoucher));



        CreateMap<Conturi, ConturiDisplayDto>()
            .ForMember(dest => dest.NumeDto, opt => opt.MapFrom(src => src.Nume))
            .ForMember(dest => dest.PrenumeDto, opt => opt.MapFrom(src => src.Prenume))
            .ForMember(dest => dest.ContActivDto , opt => opt.MapFrom(src => src.Verificat))
            .ForMember(dest => dest.DataCreareDto , opt => opt.MapFrom(src => src.DataCreare))
            .ForMember(dest => dest.EmailDto, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.UsernameDto, opt => opt.MapFrom(src => src.Username))
            .ForMember(dest => dest.IdContDto, opt => opt.MapFrom(src => src.IdCont))
            .ForMember(dest => dest.RolDto, opt => opt.MapFrom(src => src.Rol))
            .ForMember(dest => dest.TipContDto, opt => 
                opt.MapFrom(src => !src.IsGuest ? TipConturi.Registered : TipConturi.Guest));

            
        CreateMap<Seturi, SeturiDisplayDto>()
            .ForMember(dest => dest.IdSetDto, opt => opt.MapFrom(src => src.IdSet))
            .ForMember(dest => dest.EncodedIdSetDto, opt => opt.MapFrom(src => src.EncodedIdSet))
            .ForMember(dest => dest.NumeSetDto, opt => opt.MapFrom(src => src.NumeSetJson.NumeRomana))
            .ForMember(dest => dest.DescriereSetDto, opt => opt.MapFrom(src => src.DescriereJson.DescriereRomana))
            .ForMember(dest => dest.PretSetDto, opt => opt.MapFrom(src => src.PretSet))
            .ForMember(dest => dest.PretRedusSetDto, opt => opt.MapFrom(src => src.PretRedusSet))
            .ForMember(dest => dest.SetActivInMagazin, opt => opt.MapFrom(src => src.SetActivInMagazin));


        CreateMap<Conturi, ConturiDtoForModification>()
            .ForMember(dest => dest.IdContDto, opt => opt.MapFrom(src => src.IdCont))
            .ForMember(dest => dest.NumeDto, opt => opt.MapFrom(src => src.Nume))
            .ForMember(dest => dest.PrenumeDto, opt => opt.MapFrom(src => src.Prenume))
            .ForMember(dest => dest.UsernameDto, opt => opt.MapFrom(src => src.Username))
            .ForMember(dest => dest.EmailDto, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.DataCreareDto, opt => opt.MapFrom(src => src.DataCreare))
            .ForMember(dest => dest.ContActivDto, opt => opt.MapFrom(src => src.Verificat))
            .ForMember(dest => dest.RolDto, opt => opt.MapFrom(src => src.Rol));

        CreateMap<Comenzi, OrdersDisplayDto>()
            .ForMember(dest => dest.IdComandaDto, opt => opt.MapFrom(src => src.IdComanda))
            .ForMember(dest => dest.StatusComandaDto, opt => opt.MapFrom(src => src.StatusComanda))
            .ForMember(dest => dest.TipPlataDto, opt => opt.MapFrom(src => src.TipPlata))
            .ForMember(dest => dest.DataEmitereComandaDto, opt => opt.MapFrom(src => src.DataEmitereComanda))
            .ForMember(dest => dest.AwbComandaDto, opt => opt.MapFrom(src => src.AwbComanda))
            .ForMember(dest => dest.IsCancelableDto, opt => opt.MapFrom(src => src.IsCancelable));


        CreateMap<ConturiDtoForModification, Conturi>()
            .ForMember(dest => dest.Nume, opt => opt.MapFrom(src => src.NumeDto))
            .ForMember(dest => dest.Prenume, opt => opt.MapFrom(src => src.PrenumeDto))
            .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.UsernameDto))
            .ForMember(dest => dest.Email, opt => opt.Condition((src,dest,srcMember) => src.EmailDto == dest.Email))
            .ForMember(dest => dest.Verificat, opt => opt.MapFrom(src => src.ContActivDto));


        CreateMap<Produse, ProductsListingForUsers>()
            .ForMember(dest => dest.CodProdusDto, opt => opt.MapFrom(src => src.CodProdus))
            // .ForMember(dest => dest.NumeProdusDto, opt => opt.MapFrom(src => src.NumeProdus))
            .ForMember(dest => dest.NumeProdusJsonDto, opt => opt.MapFrom(src => src.NumeProdusJson))
            .ForMember(dest => dest.PretBazaDto, opt => opt.MapFrom(src => src.PretDeBaza))
            .ForMember(dest => dest.PretBazaRedusDto, opt => opt.MapFrom(src => src.PretDeBazaRedus))
            // .ForMember(dest => dest.TipulProdusuluiDto, opt => opt.MapFrom(src => src.TipulProdusului))
            .ForMember(dest => dest.TipulProdusuluiJsonDto, opt => opt.MapFrom(src => src.TipulProdusuluiJson));


        CreateMap<Reviews, ReviewsDto>()
            .ForMember(dest => dest.NumarSteleDto, opt => opt.MapFrom(src => src.NumarStele))
            .ForMember(dest => dest.TextRecenzie, opt => opt.MapFrom(src => src.TextRecenzie))
            .ForMember(dest => dest.NumeClient, opt => opt.MapFrom(src => src.Cont.Nume))
            .ForMember(dest => dest.PrenumeClient, opt => opt.MapFrom(src => src.Cont.Prenume))
            .ForMember(dest => dest.UsernameContClient, opt => opt.MapFrom(src => src.Cont.Username));


        CreateMap<TipuriGalerie, TipRejansaDto>()
            .ForMember(dest => dest.IdRejansa  , opt => opt.MapFrom(src => src.IdTipGalerie))
            .ForMember(dest => dest.SePrindeCuInele, opt => opt.MapFrom(src => src.SePrindeCuInele))
            .ForMember(dest => dest.PretTipRejansa, opt => opt.MapFrom(src => src.PretTipGalerie))
            // .ForMember(dest => dest.NumeTipRejansa, opt => opt.MapFrom(src => src.NumeTipGalerie))
            .ForMember(dest => dest.NumeTipRejansaDto, opt => opt.MapFrom(src => src.NumeTipGalerieJson))
            .ForMember(dest => dest.IncretireRejansa, opt => opt.MapFrom(src => src.IncretireRejansa))
            .ForMember(dest => dest.CaleRelativa, opt => opt.MapFrom(src => src.CaleRelativa));
        
        CreateMap<TipuriLinie, TipLinieDto>()
            .ForMember(dest => dest.IdTipLinie  , opt => opt.MapFrom(src => src.IdTipLinie))
            // .ForMember(dest => dest.NumeTipCusaturaColt, opt => opt.MapFrom(src => src.NumeTipLinie))
            .ForMember(dest => dest.NumeTipCusaturaColtJson, opt => opt.MapFrom(src => src.NumeTipLinieJson))
            .ForMember(dest => dest.PretTipCusaturaColt, opt => opt.MapFrom(src => src.PretPeTipLinie))
            .ForMember(dest => dest.CaleRelativa, opt => opt.MapFrom(src => src.CaleRelativa));

        
        CreateMap<InelePrindere, TipIneleDto>()
            .ForMember(dest => dest.IdInelPrindere  , opt => opt.MapFrom(src => src.IdInel))
            // .ForMember(dest => dest.NumeTipInel, opt => opt.MapFrom(src => src.CuloareInel))
            .ForMember(dest => dest.CuloareInelJsonDto, opt => opt.MapFrom(src => src.CuloareInelJson))
            .ForMember(dest => dest.CaleRelativa, opt => opt.MapFrom(src => src.CaleRelativa));


        CreateMap<ManoperaPageModification, Manopere>()
            .ForMember(dest => dest.IdManopera, opt => opt.Ignore())
            // .ForMember(dest => dest.NumeManopera, opt => opt.MapFrom(src => src.NumeManopera))
            .ForMember(dest => dest.NumeManoperaJson, opt => opt.MapFrom(src => src.NumeManoperaJson))
            .ForMember(dest => dest.InaltimeMaxima, opt => opt.MapFrom(src => src.InaltimeMaxima))
            .ForMember(dest => dest.MaterialFolosit, opt => opt.MapFrom(src => src.MetruTotalFolosit));

    }
    
}
