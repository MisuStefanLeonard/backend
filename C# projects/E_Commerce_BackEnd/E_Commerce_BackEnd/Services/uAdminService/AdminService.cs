using System.Diagnostics;
using System.Text.RegularExpressions;
using AutoMapper;
using E_Commerce_BackEnd.CustomExceptions;
using E_Commerce_BackEnd.Models.DTO.AdminRelatedDtos.Accounts;
using E_Commerce_BackEnd.Models.DTO.AdminRelatedDtos.Dashboard;
using E_Commerce_BackEnd.Models.DTO.AdminRelatedDtos.Dashboard.GoogleAnalyticsDTO;
using E_Commerce_BackEnd.Models.DTO.AdminRelatedDtos.OrdersDto;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductOptionsDto;
using E_Commerce_BackEnd.Models.Enums;
using E_Commerce_BackEnd.Models.OrderRelatedModels;
using E_Commerce_BackEnd.Models.ProductRelatedModels;
using E_Commerce_BackEnd.Models.UserRelatedModels;
using E_Commerce_BackEnd.Services.emailService;
using E_Commerce_BackEnd.Services.Helpers.AWS_Secret;

using E_Commerce_BackEnd.Services.uBucketService;
using E_Commerce_BackEnd.UnitOfWork;
using Google.Analytics.Data.V1Beta;
using Google.Apis.Auth.OAuth2;
using Grpc.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;


namespace E_Commerce_BackEnd.Services.uAdminService;

public partial class AdminService : IAdminService
{
    private readonly ILogger<AdminService> _logger;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBucketService _bucketService;
    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;

    public AdminService(ILogger<AdminService> logger, IUnitOfWork unitOfWork,IBucketService bucketAcces, IMapper mapper, IEmailService emailService, IMemoryCache cache)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
        _bucketService = bucketAcces;
        _mapper = mapper;
        _emailService = emailService;
        _cache = cache;
    }

    public async Task<int> AdminLogIn(string key)
    {
        var secret = await TokenService.GetSecret("prod/texx.ro/admin");
        
        if (secret == key)
        {
            _logger.LogInformation("key was equal");
            return 1;
        }
        _logger.LogInformation("key was not equal");
        return 0;
    }

    private async Task<IList<ProduseDtoForAdminListing>> TransformProductsListIntoDto(IList<Produse> products)
    {
        var productTypeRepository = _unitOfWork.Repository<TipuriProduse>();
        var dtoList = new List<ProduseDtoForAdminListing>();

        foreach (var product in products)
        {
            var listOfCurrentProductTypes = product.PTipuriPeProduse!;
            var categories = new List<string>();
            foreach (var currentTypeOnProduct in listOfCurrentProductTypes)
            {
                var currentType = await productTypeRepository
                    .GetByIdAsync(currentTypeOnProduct.IdTipProdus);
                categories.Add(currentType!.Categorie);
            }

            dtoList.Add(new ProduseDtoForAdminListing
            {
                CodProdusAdminDto = product.CodProdus,
                NumeProdusAdminDto = product.NumeProdus,
                TipProdusDto = product.TipulProdusului.ToUpper(),
                ActivInMagazinDto = product.ActivInMagazin,
                AfiseazaInNoutatiDto = product.AfiseazaInNoutati,
                ProdusLimitatDto = product.ProdusLimitat,
                CategoriiProdusDto = categories
            });
        }

        return dtoList;
    }


    public async Task<IList<ProduseDtoForAdminListing>?> GetProductsForDtoAdminListing()
    {
        var timer = new Stopwatch();
        timer.Start();
        var productsRepository = _unitOfWork.Repository<Produse>();

        var productsQueryable = await productsRepository
            .FindQueryableOfEntitiesAsync(prod => prod.IsDeleted == false,
                nav => nav.PTipuriPeProduse!);

        if (productsQueryable is null || productsQueryable.IsNullOrEmpty())
        {
            _logger.LogInformation("E NULL QEURYABLE");
            return null;
        }

        var productsList = await productsQueryable.ToListAsync();
        
        var dtoListToReturn = await TransformProductsListIntoDto(productsList);
        
        
        
        await Task.CompletedTask;
        _logger.LogInformation($"ELAPSED FOR RETRIEVING LIST OF PRODUCTS:{timer.ElapsedMilliseconds}");
        
        foreach (var product in dtoListToReturn)
        {
            _logger.LogInformation($"product code: {product.CodProdusAdminDto}");
        }
        return dtoListToReturn;
    }

    public async Task<ProduseDtoForAdminModification?> GetProductForAdminPage(string codProdus)
    {
        var productRepository = _unitOfWork.Repository<Produse>();
        var product = await productRepository
            .FindQueryable(p => p.CodProdus == codProdus)
            .Include(p => p.Producator)
            .Include(pd => pd.PProduseCuDimensiuni!)
                .ThenInclude(d => d.PdDimensiune)
            .Include(pt => pt.PTipuriPeProduse!)
                .ThenInclude(t => t.TppTipProdus)
            .Include(pc => pc.PProduseCuCulori!)
                .ThenInclude(c => c.Culoare)
                    .ThenInclude(cc => cc.CodCuloare)
            .Include(pc => pc.PProduseCuCulori!)
                .ThenInclude(i => i.ImagProduseCuCulori)
            .AsSplitQuery()
            .FirstOrDefaultAsync();

        if (product is null)
        {
            return null;
        }
        
        var productDto = new ProduseDtoForAdminModification
        {
            CodProdusDto = product.CodProdus,
            OldCodProdusDto = product.CodProdus,
            DescriereDto = product.Descriere,
            NumeProdusDto = product.NumeProdus,
            CompozitieDto = product.Compozitie,
            TvaDto = product.Tva,
            IngrijireDto = product.Ingrijire,
            FataReversibilaDto = product.FataReversibila,
            StocDto = product.Stoc,
            ActivInMagazinDto = product.ActivInMagazin,
            PretBazaDto = product.PretDeBaza,
            PretBazaRedusDto = product.PretDeBazaRedus,
            AfiseazaInNoutatiDto = product.AfiseazaInNoutati,
            ProdusLimitatDto = product.ProdusLimitat,
            NumeProducatorDto = product.Producator == null ? "" : product.Producator.NumeProducator,
            TipulProdusuluiDto = product.TipulProdusului,
            TipuriProduseDto = product.PTipuriPeProduse!.Select(tp => new TipuriProdusDto
            {
                CategorieDto = tp.TppTipProdus.Categorie,
                JustAdded = false
            }).ToList(),
            DimensiuniProduseDto = product.PProduseCuDimensiuni!.Select(pd => new DimensiuniDto
            {
                LungimeDto = pd.PdDimensiune!.Lungime,
                LatimeDto =  pd.PdDimensiune!.Latime,
                RecomandarePat =  pd.PdDimensiune!.RecomandarePat != null ? pd.PdDimensiune.RecomandarePat : "",
                PretDto = pd.Pret,
                PretRedusDto = pd.PretRedus,
                PerdeaEstePerecheDto = pd.PdDimensiune!.PerdeaEstePereche,
                JustAdded = false
            }).ToList(),
            CuloriProdusDto = product.PProduseCuCulori!.Select(pc => new CuloriDto
            {
                NumeCuloareDto = pc.Culoare.NumeCuloare,
                CodCuloareDto = pc.Culoare.CodCuloare.CodCuloare!,
                JustAdded = false,
                ImaginiProdusDto = pc.ImagProduseCuCulori!
                    .Select(imag => new ImagesDto
                    {
                        CaleImagineDto = imag.CaleImagine!,
                        FisierInBucketDto = imag.FisierInBucket,
                        PresignedUrl = GetPresignedUrlFromBucket(imag.CaleImagine!,imag.FisierInBucket).Result,
                        IdProdusCuCuloareDto = pc.IdProdusCuCuloare,
                        JustAdded = false
                    }).ToList()
            }).ToList()
        };
        
        return productDto;
    }

    public async Task<int> SaveAdminPersonalDataModification(ConturiDtoForModification updatedData)
    {
        IDbContextTransaction? dbContextTransaction = null;

        try
        {
            dbContextTransaction = await _unitOfWork.BeginTransactionAsync();
            var userRepository = _unitOfWork.Repository<Conturi>();
            
            var userToBeModified = userRepository
                .FindQueryable(u => u.IdCont == updatedData.IdContDto)
                .First();

            if (userToBeModified.Email != updatedData.EmailDto)
            {
                var newEmail = updatedData.EmailDto;
                if (newEmail is not null)
                {
                    
                    var emailRegex = MyRegex();
                    var isValid = emailRegex.IsMatch(updatedData.EmailDto!);
                    if (!isValid) 
                        throw new InvalidEmailException("Invalid email");
                    // de vazut de ce se updateaza si verificait si  ora linkului de confirmare nu se updateaza?
                    
                    var changeRequestId = Guid.NewGuid();
                    await _emailService.SendEmailAsync(
                        updatedData.EmailDto!,
                        "Schimbare email",
                        "<p style='font-size: 16px;'>Email-ul dumneavoastra a fost schimbat de catre admin.</p>" +
                        "<p style='font-size: 16px;'>Daca nu dumneavoastra ati solicitat aceasta schimbare, " +
                        "<span style='color: red; font-weight: bold;'>NU</span> intrati pe acest link si contactati-ne rapid la " +
                        "<a href='mailto:office@texx.ro'>office@texx.ro</a></p>" +
                        "<p style='font-size: 16px'>Daca dumneavoastra ati avut contact cu administratorul, intrati pe acest link pentru schimbare email-ului" +
                        $"</p><a href='http://localhost:3000/user/admin/emailChanged?changeRequestId={changeRequestId}'>LINK</a>" +
                        "<br/><br/>" +
                        "<p style='font-size: 24px'>ACEST LINK VA EXPIRA INTR-O ORA.</p>" +
                        "<p style='font-size: 16px'>texx.ro va doreste o zi buna in continuare!</p>"
                    );
                    _mapper.Map(updatedData, userToBeModified);
                    userToBeModified.Verificat = false;
                    userToBeModified.OraLinkConfirmare = DateTime.UtcNow.AddHours(1);
                    await userRepository.UpdateAsync(userToBeModified);
                    await _unitOfWork.CommitTransactionAsync(dbContextTransaction);
                    var key = $"Data_{changeRequestId}";
                    var data = $"{updatedData.EmailDto}_{userToBeModified.Email}";
                    _cache.Set(key, data, TimeSpan.FromHours(1));
                   
                    return 2;

                }
            }
            
            _mapper.Map(updatedData, userToBeModified);
          
            await userRepository.UpdateAsync(userToBeModified);
            await _unitOfWork.CommitTransactionAsync(dbContextTransaction);
            return 1;

        }
        catch (Exception ex)
        {
           
            if (dbContextTransaction != null)
            {
                await _unitOfWork.RollBackTransactionAsync(dbContextTransaction);
            }

            _logger.LogError(ex.Message);
            return ex switch
            {
                InvalidEmailException => -2,
                _ => -1
            };
        }
    }

    public async Task<int> EmailChangedByAdmin(string emailData , string currentCacheKey)
    {
        IDbContextTransaction? dbContextTransaction = null;
        try
        {
            var emailSplit = emailData.Split("_");
            var oldEmail = emailSplit[1];
            var newEmail = emailSplit[0];
            _logger.LogInformation($"{oldEmail},{newEmail}");
            
            dbContextTransaction = await _unitOfWork.BeginTransactionAsync();
            var userRepository = _unitOfWork.Repository<Conturi>();
            var timeNow = DateTime.UtcNow;
            var userToBeModified = userRepository
                .FindQueryable(u => u.Email == oldEmail)
                .First();
            _logger.LogInformation($"{(timeNow - userToBeModified.OraLinkConfirmare).Hours}");
            if ((timeNow - userToBeModified.OraLinkConfirmare).Hours > 1)
            {
                _logger.LogInformation("TIMPUL S-A SCURS");
                return -2; // expired link
            }
            
            var miniMapper = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<string, Conturi>()
                    .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src));

            });
            var localMapper = miniMapper.CreateMapper();
            userToBeModified.Verificat = true;
            
            localMapper.Map(newEmail, userToBeModified);

            await userRepository.UpdateAsync(userToBeModified);
            await _unitOfWork.CommitTransactionAsync(dbContextTransaction);
            
            _cache.Remove(currentCacheKey);
            return 1; // success!


            // de scris endpoint-urile!
        }
        catch (Exception ex)
        {
            if (dbContextTransaction != null)
            {
                await _unitOfWork.RollBackTransactionAsync(dbContextTransaction);
            }

            _logger.LogError(ex.Message);
            return -1; // error
        }
        
    }

    public async Task<int> ModifyAddressState(string alias, bool addressState,TipAdrese tipAdresa ,int accountId)
    {
        IDbContextTransaction? transaction = null;
        try
        {
            transaction = await _unitOfWork.BeginTransactionAsync();
            var addressRepository = _unitOfWork.Repository<Adrese>();
            
            var addressToModifyTheState = await addressRepository
                .FindQueryable(a => a.IdCont == accountId && a.Alias == alias && a.TipAdresa == tipAdresa)
                .FirstAsync();

            addressToModifyTheState.IsDeleted = !addressState;

            await addressRepository.UpdateAsync(addressToModifyTheState);
            await _unitOfWork.CommitTransactionAsync(transaction);
            return 1;
        }
        catch (Exception e)
        {
            if (transaction != null)
            {
                await _unitOfWork.RollBackTransactionAsync(transaction);
            }
            
            _logger.LogError(e.Message);
            return -1;
        }
    }

    public async Task<int> ModifyAddresses(ConturiDtoForModification updatedAddreses)
    {
        IDbContextTransaction? transaction = null;
        try
        {
            transaction = await _unitOfWork.BeginTransactionAsync();
            var addressRepository = _unitOfWork.Repository<Adrese>();
            var firmDetailsRepository = _unitOfWork.Repository<DetaliiFactura>();
            var locationsRepository = _unitOfWork.Repository<Locatii>();

            var listOfAddreses = updatedAddreses.AdreseClient;
            var accountId = updatedAddreses.IdContDto;

            var clientAddresses = await addressRepository
                .FindQueryable(a => a.IdCont == accountId)
                .ToListAsync();

            // same len of clientAddresses
            if (!listOfAddreses.IsNullOrEmpty())
            {
                for (var i = 0; i < listOfAddreses.Count; i++)
                {
                    var currentAddressUpdated = listOfAddreses[i];
                    var clientAddressInDb = clientAddresses[i];
                    
                    var jsonCurrentAddressUpdate = JsonConvert.SerializeObject(currentAddressUpdated);
                    var jsonClientAddressesInDb = JsonConvert.SerializeObject(clientAddressInDb);

                    if (jsonCurrentAddressUpdate == jsonClientAddressesInDb) continue;

                    var originalTypeOfAddressUpdated = currentAddressUpdated.TipAdresaDto;
                    var originalTypeOfAddressInDb = clientAddressInDb.TipAdresa;
                    
                    // case where the type of the address was not changed
                    if (originalTypeOfAddressUpdated == originalTypeOfAddressInDb)
                    {
                        _mapper.Map(currentAddressUpdated, clientAddressInDb);
                    }
                    else
                    {
                        var isUpdatedLocatieInDb = await locationsRepository
                            .FindQueryable(l => l.Judet == currentAddressUpdated.JudetDto
                                                && l.Oras == currentAddressUpdated.OrasDto
                                                && l.CodPostal == currentAddressUpdated.CodPostalDto)
                            .FirstOrDefaultAsync();

                        if (isUpdatedLocatieInDb is null)
                        {
                            var newLocation = new Locatii
                            {
                                Oras = currentAddressUpdated.OrasDto,
                                Judet = currentAddressUpdated.JudetDto,
                                CodPostal = currentAddressUpdated.CodPostalDto
                            };

                            await locationsRepository.AddAsync(newLocation);
                            await _unitOfWork.CommitAsync();
                            isUpdatedLocatieInDb = newLocation;
                            clientAddressInDb.IdLocatie = isUpdatedLocatieInDb.IdLocatie;
                        }
                        
                        if (originalTypeOfAddressInDb == TipAdrese.Livrare &&
                            originalTypeOfAddressUpdated == TipAdrese.Facturare)
                        {
                            var areFirmDetailsInDb = await firmDetailsRepository
                                .FindQueryable(df => df.Cif == currentAddressUpdated.CifDto &&
                                                     df.NumeFirma == currentAddressUpdated.NumeFirmaDto)
                                .FirstOrDefaultAsync();

                            if (areFirmDetailsInDb is null)
                            {
                                var newFirmDetails = new DetaliiFactura
                                {
                                    Cif = currentAddressUpdated.CifDto,
                                    NumeFirma = currentAddressUpdated.NumeFirmaDto
                                };

                                await firmDetailsRepository.AddAsync(newFirmDetails);
                                await _unitOfWork.CommitAsync();
                                areFirmDetailsInDb = newFirmDetails;
                                clientAddressInDb.IdDetaliuFactura = areFirmDetailsInDb.IdDetaliu;
                            }
                            _mapper.Map(currentAddressUpdated, clientAddressInDb);
                        }
                        // original was billing
                        // updated is delivery
                        else
                        {
                            var firmDetalisForCurrentAddress = await firmDetailsRepository
                                .FindQueryable(a => a.IdDetaliu == clientAddressInDb.DetaliuFactura!.IdDetaliu)
                                .FirstAsync();

                            await firmDetailsRepository.DeleteAsync(firmDetalisForCurrentAddress);
                            
                            clientAddressInDb.IdDetaliuFactura = null;
                            _mapper.Map(currentAddressUpdated, clientAddressInDb);
                        }
                    }

                }
            }
            
            await addressRepository.UpdateRangeAsync(clientAddresses);
            await _unitOfWork.CommitTransactionAsync(transaction);
            return 1;
        }
        catch (Exception e)
        {
            if (transaction != null)
            {
                await _unitOfWork.RollBackTransactionAsync(transaction);
            }
            
            _logger.LogError(e.Message);
            return -1;
        }
    }

   public async Task<IList<MainOrdersDisplayDto>> GetAllOrders()
{
    var productsOnOrdersRepository = _unitOfWork.Repository<ProduseCuComenzi>();

    var allOrders = await productsOnOrdersRepository
        .GetSimpleQueryable()
        .Select(order => new
        {
            order.IdComanda,
            IdContDto = order.Comanda.CAdresaFacturare.IdCont,
            order.Comanda.DataEmitereComanda,
            order.Comanda.StatusComanda,
            order.Comanda.TipPlata,
            order.Comanda.AwbComanda,
            order.Set,
            IsSet = order.Set != null,
            ProductPrice = order.Set == null ? 
                order.PcManopera == null 
                    ? order.PretCumparat * order.NrBucati 
                    : (order.PretCumparat * ((Convert.ToDecimal(order.PcDimensiune!.Lungime) / 100) * order.PcManopera.MaterialFolosit) 
                       + order.PcManopera.PretCurentTipGalerie * ((Convert.ToDecimal(order.PcDimensiune!.Lungime) / 100) * order.PcManopera.MaterialFolosit) 
                       + order.PcManopera.PretCurentTipLinie * ((Convert.ToDecimal(order.PcDimensiune!.Lungime) / 100) * order.PcManopera.MaterialFolosit))
                      * order.NrBucati
                
                : 0, 
            SetPrice = order.Set != null ? order.PretCumparat : 0 
        })
        .GroupBy(order => order.IdComanda)
        .Select(group => new MainOrdersDisplayDto
        {
            IdComandaDto = group.Key,
            IdContDto = group.First().IdContDto,
            DataEmitereComandaDto = group.First().DataEmitereComanda,
            StatusComandaDto = group.First().StatusComanda,
            TipPlataDto = group.First().TipPlata,
            AwbComandaDto = group.First().AwbComanda,
            // Sum the product prices excluding set products and add the distinct set price once
            PretTotalComanda = group.Where(x => !x.IsSet).Sum(x => x.ProductPrice) 
                                + group.Where(x => x.IsSet).Select(x => x.SetPrice).First()
                          
        })
        .OrderByDescending(order => order.DataEmitereComandaDto)
        .ToListAsync();

    return allOrders;
}

public async Task<DashboardGeneralData> GetMainDashboardData(DateTime? lowerInterval , DateTime? upperInterval)
{
    var productWithOrdersRepository = _unitOfWork.Repository<ProduseCuComenzi>();
    var productsRepository = _unitOfWork.Repository<Produse>();
    var ordersRepository = _unitOfWork.Repository<Comenzi>();
    var clientsRepository = _unitOfWork.Repository<Conturi>();

    var stopwatch = Stopwatch.StartNew();
    
    // total incasari
    var totalRevenue = await productWithOrdersRepository
        .GetSimpleQueryable()
        .Select(order => new
        {
            order.IdComanda,
            order.Set,
            order.Comanda.StatusComanda,
            order.PretCumparat,
            order.Comanda.DataEmitereComanda,
            IsSet = order.Set != null,
            ProductPrice = order.Set == null 
                ? order.PcManopera == null 
                    ? order.PretCumparat * order.NrBucati 
                    : (order.PretCumparat * ((Convert.ToDecimal(order.PcDimensiune!.Lungime) / 100) * order.PcManopera.MaterialFolosit) 
                       + order.PcManopera.PretCurentTipGalerie * ((Convert.ToDecimal(order.PcDimensiune!.Lungime) / 100) * order.PcManopera.MaterialFolosit)
                       + order.PcManopera.PretCurentTipLinie * ((Convert.ToDecimal(order.PcDimensiune!.Lungime) / 100) * order.PcManopera.MaterialFolosit))
                      * order.NrBucati
                : 0, 
            SetPrice = order.Set != null ? order.PretCumparat * order.NrBucati : 0 
        }).Where(order => order.StatusComanda == StatusComanda.Finalizata
                          && (lowerInterval == null || order.DataEmitereComanda >= lowerInterval)
                          && (upperInterval == null || order.DataEmitereComanda <= upperInterval))
        .GroupBy(order => order.IdComanda)
        .Select(group => new 
        {
          
            PretTotalComanda = group.Where(x => !x.IsSet).Sum(x => x.ProductPrice) 
                               + group.Where(x => x.IsSet).Select(x => x.SetPrice).FirstOrDefault()
        })
        .AsSplitQuery()
        .SumAsync(order => order.PretTotalComanda);

    var totalProduse = await productsRepository
        .GetSimpleQueryable()
        .Select(product => new
        {
            product.ActivInMagazin,
        }).GroupBy(product => product.ActivInMagazin)
        .Select(group => new
        {
            StareProdus = group.Key ? "Active" : "Inactive",
            TotalPerStare = group.Count()
        }).ToDictionaryAsync(productDict => productDict.StareProdus,
            productDict => productDict.TotalPerStare);

    // tipuri produse - pe categorii / active si inactive
    var productTypes = await productsRepository
        .GetSimpleQueryable()
        .SelectMany(product => product.PTipuriPeProduse!, (product, type) => new
        {
            product.TipulProdusului,
            Categorii = type.TppTipProdus.Categorie,
            IsActive = product.ActivInMagazin
        }).GroupBy(item => item.TipulProdusului)
        .Select(group => new
        {
            TipulProdusului = group.Key,
            Categories = group
                .GroupBy(item => item.Categorii)
                .Select(categoryGroup => new
                {
                    Categorie = categoryGroup.Key,
                    StatusCount = categoryGroup
                        .GroupBy(item => item.IsActive)
                        .Select(statusGroup => new
                        {
                            Status = statusGroup.Key ? "Activ" : "Inactiv",
                            Count = statusGroup.Count()
                        })
                })
        }).ToDictionaryAsync(tpp =>
                tpp.TipulProdusului,
            tpp => tpp.Categories.ToDictionary(
                category => category.Categorie,
                category => category.StatusCount.ToDictionary(
                    status => status.Status,
                    status => status.Count)));

    // status clienti
    var clientTypes = await clientsRepository
        .GetSimpleQueryable()
        .Select(client => new
        {
            client.IsGuest,
            client.Username
        }).GroupBy(clientGroup => clientGroup.IsGuest)
        .Select(item => new
        {
            StareCont = item.Key ? "Neinregistrat" : "Inregistrat",
            Total = item.Count()
        })
        .ToDictionaryAsync(clientDict => clientDict.StareCont, clientDict => clientDict.Total);
    
    // status comenzi
    
    var orderTypes = await ordersRepository
        .GetSimpleQueryable()
        .Select(order => new
        {
            order.StatusComanda,
            order.IdComanda,
            order.DataEmitereComanda
        }).Where(order => (lowerInterval == null || order.DataEmitereComanda >= lowerInterval)
                 && (upperInterval == null || order.DataEmitereComanda <= upperInterval))
        .GroupBy(orderGroup => orderGroup.StatusComanda)
        .Select(item => new
        {
            StareComanda = item.Key.ToString() ,
            Total = item.Count()
        })
        .ToDictionaryAsync(orderDict => orderDict.StareComanda, orderDict => orderDict.Total);
    
    // total revenue per category

    var totalRevenuePerProduct = await productWithOrdersRepository
        .GetSimpleQueryable()
        .Select(product => new
        {
            product.Produs.CodProdus,
            product.IdSet,
            product.PcManopera ,
            PretAdus = product.PcManopera == null ? product.PretCumparat * product.NrBucati
                : (product.PretCumparat * ((Convert.ToDecimal(product.PcDimensiune!.Lungime) / 100) * product.PcManopera.MaterialFolosit) 
                   + product.PcManopera.PretCurentTipGalerie * ((Convert.ToDecimal(product.PcDimensiune!.Lungime) / 100) * product.PcManopera.MaterialFolosit) 
                   + product.PcManopera.PretCurentTipLinie * ((Convert.ToDecimal(product.PcDimensiune!.Lungime) / 100) * product.PcManopera.MaterialFolosit)) * product.NrBucati
        }).Where(product => product.IdSet == null)
        .GroupBy(item => item.CodProdus)
        .Select(group => new
        {
            CodProdus = group.Key,
            TotalSumaVanzariProdus = group.Sum(product => product.PretAdus)
        }).ToDictionaryAsync(productDict => productDict.CodProdus
            , productDict => productDict.TotalSumaVanzariProdus);
            
    
    
        // totalRevenue 
        // total finished orders
        // aov = totalRevenue/total finished orders
       
        var totalFinishedOrders = orderTypes
            .Where(kv => kv.Key == "Finalizata")
            .Select(kv => kv.Value)
            .FirstOrDefault();

        // Average older value
        decimal averageOrderValue = 0;
        if (totalFinishedOrders != 0)
        {
            averageOrderValue = totalRevenue / totalFinishedOrders;
        }
        
        
        // best 5 selled products
        var bestSelled5Products = await productWithOrdersRepository
            .GetSimpleQueryable()
            .Select(order => new
            {
                order.Produs.CodProdus,
                order.IdComanda,
                order.IdSet,
                PretAdus = order.PcManopera == null 
                    ? 
                    order.PretCumparat * order.NrBucati
                    : 
                    ((order.PretCumparat * ((Convert.ToDecimal(order.PcDimensiune!.Lungime) / 100) * order.PcManopera.MaterialFolosit) ) 
                        + order.PcManopera.PretCurentTipGalerie * ((Convert.ToDecimal(order.PcDimensiune!.Lungime) / 100) * order.PcManopera.MaterialFolosit)
                        + order.PcManopera.PretCurentTipLinie * ((Convert.ToDecimal(order.PcDimensiune!.Lungime) / 100) * order.PcManopera.MaterialFolosit))
                          * order.NrBucati
            }).Where(order => order.IdSet == null)
            .GroupBy(order => order.CodProdus)
            .Select(group => new TopVandutProdusDto
            {
                CodProdus = group.Key,
                NrVanzari = group.Count(),
                VenitTotal = group.Sum(price => price.PretAdus),
            }).OrderByDescending(product => product.NrVanzari)
            .ThenByDescending(product => product.VenitTotal)
            .Take(5)
            .ToListAsync();

        var dashboardDataDto = new DashboardGeneralData
        {
            TotalIncasariGeneral = totalRevenue,
            TotalProduse = totalProduse,
            TipuriProduse = productTypes,
            PretMediuComandaGeneral = averageOrderValue,
            TipuriClientiGeneral = clientTypes,
            TipuriComenziGeneral = orderTypes,
            VenitTotalPeProdus = totalRevenuePerProduct,
            TopProduseVanduteGeneral = bestSelled5Products
        };
        
        stopwatch.Stop();
        _logger.LogInformation($"Elapsed time in miliseconds : {stopwatch.ElapsedMilliseconds}");
        
        return dashboardDataDto;
}

public async Task<GaDashboardDto> GetGoogleAnalyticsData(string? lowerInterval , string? upperInterval)
{
    const string propertyId = "462890702";

    // Get the current working directory
    var currentDirectory = Directory.GetCurrentDirectory();

    // Construct the path to the credentials file (adjust the relative path)
    var credentialsPath = Path.Combine(currentDirectory, "credentials.json");

    // Load the service account credentials from the JSON key file
    var credential = GoogleCredential.FromFile(credentialsPath)
        .CreateScoped("https://www.googleapis.com/auth/analytics.readonly");

    // Create the BetaAnalyticsDataClient with the credential
    var client = await new BetaAnalyticsDataClientBuilder
    {
        ChannelCredentials = credential.ToChannelCredentials()
    }.BuildAsync();



    if (lowerInterval == null && upperInterval == null)
    {
        lowerInterval = "2022-01-01";
        upperInterval = "today";
    }
    
    var totalUsers = new RunReportRequest
    {
        Property = "properties/" + propertyId,
        Metrics = 
        { 
            new Metric { Name = "activeUsers" }, 
            new Metric { Name = "active1DayUsers" }, 
            new Metric { Name = "active28DayUsers" }, 
            new Metric { Name = "screenPageViews"}
        },
        DateRanges = { new DateRange { StartDate = lowerInterval , EndDate = upperInterval } },
        DimensionFilter = new FilterExpression
        {
            NotExpression = new FilterExpression
            {
                Filter = new Filter
                {
                    FieldName = "pagePath",
                    StringFilter = new Filter.Types.StringFilter
                    {
                        MatchType = Filter.Types.StringFilter.Types.MatchType.Contains,
                        Value = "/admin"
                    }
                }
            }
        },
    };
    
   
    
    var totalUsersRealTime = new RunRealtimeReportRequest
    {
        Property = "properties/" + propertyId,
        Metrics = 
        { 
            new Metric { Name = "activeUsers" },
            new Metric { Name = "screenPageViews"}
        },
        DimensionFilter = new FilterExpression
        {
            NotExpression = new FilterExpression
            {
                Filter = new Filter
                {
                    FieldName = "unifiedScreenName",
                    StringFilter = new Filter.Types.StringFilter
                    {
                        MatchType = Filter.Types.StringFilter.Types.MatchType.Contains,
                        Value = "Admin"
                    }
                }
            }
        },
    };

    var usersPerPage = new RunReportRequest
    {
        Property = "properties/" + propertyId,
        Dimensions =
        {
            new Dimension { Name = "city" },
            new Dimension { Name = "pagePath" },
           
        },
        Metrics =
        {
            new Metric { Name = "activeUsers" }, 
            new Metric { Name = "active1DayUsers" }, 
            new Metric { Name = "active28DayUsers" }, 
            new Metric { Name = "screenPageViews" }
        },
        DateRanges = { new DateRange { StartDate = lowerInterval , EndDate = upperInterval  } },
        DimensionFilter = new FilterExpression
        {
            NotExpression = new FilterExpression
            {
                Filter = new Filter
                {
                    FieldName = "pagePath",
                    StringFilter = new Filter.Types.StringFilter
                    {
                        MatchType = Filter.Types.StringFilter.Types.MatchType.Contains,
                        Value = "/admin"
                    }
                }
            }
        },
    };
    
    var usersPerPageRealTime = new RunRealtimeReportRequest
    {
        Property = "properties/" + propertyId,
        Dimensions =
        {
            new Dimension { Name = "city" },
            new Dimension { Name = "unifiedScreenName" },
            
        },
        Metrics =
        {
            new Metric { Name = "activeUsers" },
            new Metric { Name = "screenPageViews"}
        },
        DimensionFilter = new FilterExpression
        {
            NotExpression = new FilterExpression
            {
                Filter = new Filter
                {
                    FieldName = "unifiedScreenName",
                    StringFilter = new Filter.Types.StringFilter
                    {
                        MatchType = Filter.Types.StringFilter.Types.MatchType.Contains,
                        Value = "Admin"
                    }
                }
            }
        },
    };
    // response from first runReport for total active users
    var responseFromTotalUsers = await client.RunReportAsync(totalUsers);
    // Real time runReportRequest for total traffic on website
    var realTimeResponseFromTotalUsers = await client.RunRealtimeReportAsync(totalUsersRealTime);
    // response from total traffic per page 
    var responseFromUsersPerPageRequest = await client.RunReportAsync(usersPerPage);
    // Real time response from total traffic on every page
    var realTimeResponseFromTotalTrafficPerPage = await client.RunRealtimeReportAsync(usersPerPageRealTime);
    
    var googleAnalyticsDto = new GaDashboardDto();
    
    // general
    foreach (var row in responseFromTotalUsers.Rows)
    {
        googleAnalyticsDto.TotalActiveUsers = int.Parse(row.MetricValues[0].Value); // total active users 
        googleAnalyticsDto.TotalOneDayActiveUsers = int.Parse(row.MetricValues[1].Value); // total active users petr 1 day 
        googleAnalyticsDto.Total28DayActiveUsers = int.Parse(row.MetricValues[2].Value); // total active users per 28 days 
        googleAnalyticsDto.TotalScreenPageViews = int.Parse(row.MetricValues[3].Value); // total page views
    }
    
    
    
    // real time
    foreach (var row in realTimeResponseFromTotalUsers.Rows)
    {
        
        googleAnalyticsDto.TotalActiveUsersReal = int.Parse(row.MetricValues[0].Value); // total active users real time
        googleAnalyticsDto.TotalScreenPageViewsReal = int.Parse(row.MetricValues[1].Value);
    }
    
    // general
    foreach (var row in responseFromUsersPerPageRequest.Rows)
    {
        var currentCity = row.DimensionValues[0].Value;
        var currentPage = row.DimensionValues[1].Value;
        if (googleAnalyticsDto.UseriActiviPerPagina.TryGetValue(currentPage, out var cityInfo))
        {
            if (cityInfo.Cities.All(city => city != currentCity))
            {
                cityInfo.Cities.Add(currentCity);
                cityInfo.TotalActiveUserPerPage += int.Parse(row.MetricValues[0].Value);
                cityInfo.TotalActiveUser1DayPerPage += int.Parse(row.MetricValues[1].Value);
                cityInfo.TotalActiveUser28DayPerPage += int.Parse(row.MetricValues[2].Value);
                cityInfo.TotalCurrentPageViews += int.Parse(row.MetricValues[3].Value);
            }
            else
            {
               _logger.LogInformation("City already in the set");
            }
        }
        else
        {
            // metrics
            var activeUsersPerPage = int.Parse(row.MetricValues[0].Value);
            var activeUsers1DayPerPage = int.Parse(row.MetricValues[1].Value);
            var activeUsers28DayPerPage = int.Parse(row.MetricValues[2].Value);
            var pageViews = int.Parse(row.MetricValues[3].Value);
            // dimensions
           
            var cities = new HashSet<string> { currentCity };

            googleAnalyticsDto.UseriActiviPerPagina[currentPage] = new TrafficPerPage
            {
                TotalActiveUserPerPage = activeUsersPerPage,
                TotalActiveUser1DayPerPage = activeUsers1DayPerPage,
                TotalActiveUser28DayPerPage = activeUsers28DayPerPage,
                TotalCurrentPageViews = pageViews,
                Cities = cities
            };
           
        }
    }
    
    // real time
    foreach (var row in realTimeResponseFromTotalTrafficPerPage.Rows)
    {
        var currentCity = row.DimensionValues[0].Value;
        var currentPage = row.DimensionValues[1].Value;
        if (googleAnalyticsDto.UseriActiviPerPaginaReal.TryGetValue(currentPage, out var cityInfo))
        {
            if (cityInfo.Cities.All(city => city != currentCity))
            {
                cityInfo.Cities.Add(currentCity);
                cityInfo.TotalActiveUserPerPage += int.Parse(row.MetricValues[0].Value);
                cityInfo.TotalCurrentPageViews += int.Parse(row.MetricValues[1].Value);
            }
            else
            {
                _logger.LogInformation("City already in the set");
            }
        }
        else
        {
            // metrics
            var activeUsersPerPage = int.Parse(row.MetricValues[0].Value);
            var pageViews = int.Parse(row.MetricValues[1].Value);
            // dimensions
           
            var cities = new HashSet<string> { currentCity };

            googleAnalyticsDto.UseriActiviPerPaginaReal[currentPage] = new TrafficPerPage
            {
                TotalActiveUserPerPage = activeUsersPerPage,
                TotalCurrentPageViews = pageViews,
                Cities = cities
            };
           
        }
    }
    
    
    return googleAnalyticsDto;
}


private async Task<string?> GetPresignedUrlFromBucket(string imagePath, string dirInBucket)
    {
        var url = await _bucketService.GeneratePresignedUrl(imagePath, dirInBucket);
        return url;
    }

    [GeneratedRegex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$")]
    private static partial Regex MyRegex();
}