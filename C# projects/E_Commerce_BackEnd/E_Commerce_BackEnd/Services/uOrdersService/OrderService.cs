using System.Text;
using E_Commerce_BackEnd.CustomExceptions;
using E_Commerce_BackEnd.Models.ConfigurationModels;
using E_Commerce_BackEnd.Models.DTO;
using E_Commerce_BackEnd.Models.DTO.AdminRelatedDtos.OrdersDto;
using E_Commerce_BackEnd.Models.DTO.ClientOrdersDto;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ManopereDto.ManoperaForSet;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductOptionsDto;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.ProductPage.OptionsForCurtain;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ShoppingCartDtos;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.VouchereDtos;
using E_Commerce_BackEnd.Models.Enums;
using E_Commerce_BackEnd.Models.OrderRelatedModels;
using E_Commerce_BackEnd.Models.ProductRelatedModels;
using E_Commerce_BackEnd.Models.ProductRelatedModels.JSON_Models;
using E_Commerce_BackEnd.Models.ProductVouchersModels;
using E_Commerce_BackEnd.Models.UserRelatedModels;
using E_Commerce_BackEnd.Services.emailService;
using E_Commerce_BackEnd.Services.Helpers.adminHelpers;
using E_Commerce_BackEnd.Services.Helpers.AWS_Secret.AWSBucket_CRUD;
using E_Commerce_BackEnd.Services.Helpers.UserHelpers;
using E_Commerce_BackEnd.Services.uMJMLService;
using E_Commerce_BackEnd.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;

namespace E_Commerce_BackEnd.Services.uOrdersService;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBucketAcces _bucketAcces;
    private readonly ILogger<OrderService> _logger;
    private readonly IEmailService _emailService;
    private readonly IMjmlService _mjmlService;
    private static readonly List<string> InvoiceCredentials = ["smart_bill_username", "smart_bill_password" , "cif"];
    private const string SeriesNumber = "THD2015";
    private readonly DocumentProcessing _documentProcessing;
    

    public OrderService(IUnitOfWork unitOfWork, IBucketAcces bucketAcces, ILogger<OrderService> logger, IEmailService emailService, IMjmlService mjmlService, DocumentProcessing documentProcessing)
    {
        _unitOfWork = unitOfWork;
        _bucketAcces = bucketAcces;
        _logger = logger;
        _emailService = emailService;
        _mjmlService = mjmlService;
        _documentProcessing = documentProcessing;
    }

    public async Task<IList<ClientOrder>> GetClientOrders(int accountId , string currency = "RON")
    {
        try
        {
            var clientOrders = await _unitOfWork.Repository<Conturi>()
                .FindQueryable(account => account.IdCont == accountId)
                .SelectMany(address => address.AdreseConturi!)
                .SelectMany(order => order.AdreseLivrarePeComanda!)
                .AsSplitQuery()
                .Select(order => new ClientOrder
                {
                    Items = order.PcComenzi!
                        .GroupBy(group => group.IdentificatorSet != "21" ? group.IdentificatorSet : group.IdProduseCuComenzi.ToString())
                        .Select(item => new GroupedCartItems
                        {
                            Key = item.Key,
                            CartItems = item.Select(product => new CartItems
                            {
                                IdProdus = product.Produs.IdProdus,
                                IdSet = product.Set!.IdSet,
                                NumeSet =  currency == "RON" ? product.Set!.NumeSetJson.NumeRomana :  product.Set!.NumeSetJson.NumeEngleza,
                                CodProdus = product.Produs.CodProdus,
                                NumeProdus = currency == "RON" ?  product.Produs.NumeProdusJson.NumeRomana : product.Produs.NumeProdusJson.NumeEngleza ,
                                TipProdus = currency == "RON" ?  product.Produs.TipulProdusuluiJson.TipProdusRomana :  product.Produs.TipulProdusuluiJson.TipProdusEngleza,
                                CuloareSelectata = new CuloriDto
                                {
                                    IdCuloare = product.PcCuloare.IdCuloare,
                                    NumeCuloareDto = currency == "RON" ?  product.PcCuloare.NumeCuloareJson.CuloareRomana : product.PcCuloare.NumeCuloareJson.CuloareEngleza,
                                    CodCuloareDto = product.PcCuloare.CodCuloare.CodCuloare!,
                                    JustAdded = false,
                                    ImaginiProdusDto = product.Produs.PProduseCuCulori!
                                        .FirstOrDefault(pc => pc.ImagProduseCuCulori!.Count > 0)!
                                        .ImagProduseCuCulori!.Select(imag => new ImagesDto
                                        {
                                            CaleImagineDto = imag.CaleImagine!,
                                            FisierInBucketDto = imag.FisierInBucket,
                                            PresignedUrl = "empty",
                                            JustAdded = false,
                                            IdProdusCuCuloareDto = 0
                                        }).Take(1).OrderBy(c => c.CaleImagineDto)
                                        .ToList()
                                },
                                DimensiuneSelectata = new DimensiuniDto
                                {
                                    IdDimensiune = product.PcDimensiune!.IdDimensiune,
                                    LungimeDto = product.PcManopera!.NumeManoperaJson!.NumeRomana != "STAN" ?  product.PcDimensiune.Lungime : ((int)(product.PcManopera.MaterialFolosit * 100)).ToString(),
                                    LatimeDto = product.PcDimensiune.Lungime,
                                    RecomandarePat = product.PcDimensiune.RecomandarePat,
                                    PretDto = 0,
                                    PretRedusDto = 0,
                                    JustAdded = false,
                                    PerdeaEstePerecheDto = product.PcManopera!.NumeManoperaJson.NumeRomana == "STAN" ? null : product.PcDimensiune.PerdeaEstePereche 
                                        
                                },
                                SelectedManopera = product.Produs.TipulProdusuluiJson.TipProdusRomana == "perdea" || product.Produs.TipulProdusuluiJson.TipProdusRomana == "draperie" ? new StandardManopereOnSet
                                {
                                    IdManopera = product.PcManopera!.IdManopera,
                                    NumeManopera = currency == "RON" ?  product.PcManopera.NumeManoperaJson!.NumeRomana : product.PcManopera.NumeManoperaJson!.NumeEngleza ,
                                    MetruTotalFolosit = product.PcManopera.MaterialFolosit,
                                    InaltimeMaxima = product.PcManopera.InaltimeMaxima,
                                    TipInel = product.PcManopera.InelPrindereLaManopera != null ? new TipIneleDto
                                    {
                                        IdInelPrindere = product.PcManopera.InelPrindereLaManopera.IdInel,
                                        NumeTipInel =  currency == "RON" ?  product.PcManopera.InelPrindereLaManopera.CuloareInelJson.CuloareRomana
                                            : product.PcManopera.InelPrindereLaManopera.CuloareInelJson.CuloareEngleza,
                                        CaleRelativa = product.PcManopera.InelPrindereLaManopera.CaleRelativa,
                                        PresignedUrl = "empty"
                                    } : null,
                                    TipGalerie = new TipRejansaDto
                                    {
                                        IdRejansa = product.PcManopera.TipGalerieLaManopera.IdTipGalerie,
                                        NumeTipRejansa =  currency == "RON" ?  product.PcManopera.TipGalerieLaManopera.NumeTipGalerieJson.NumeRomana
                                            :  product.PcManopera.TipGalerieLaManopera.NumeTipGalerieJson.NumeEngleza,
                                        PretTipRejansa = currency == "RON" ? product.PcManopera.TipGalerieLaManopera.PretTipGalerie
                                            :  product.PcManopera.TipGalerieLaManopera.PretTipGalerie / 5,
                                        IncretireRejansa = product.PcManopera.TipGalerieLaManopera.IncretireRejansa,
                                        CaleRelativa = product.PcManopera.TipGalerieLaManopera.CaleRelativa,
                                        PresignedUrl = "empty",
                                        SePrindeCuInele = product.PcManopera.TipGalerieLaManopera.SePrindeCuInele
                                    },
                                    TipLinie = new TipLinieDto
                                    {
                                        IdTipLinie = product.PcManopera.TipLinieLaManopera.IdTipLinie,
                                        NumeTipCusaturaColt =  currency == "RON" ?  product.PcManopera.TipLinieLaManopera.NumeTipLinieJson.NumeRomana
                                            : product.PcManopera.TipLinieLaManopera.NumeTipLinieJson.NumeEngleza,
                                        PretTipCusaturaColt = currency == "RON" ? product.PcManopera.TipLinieLaManopera.PretPeTipLinie
                                            :  product.PcManopera.TipLinieLaManopera.PretPeTipLinie / 5,
                                        CaleRelativa = product.PcManopera.TipLinieLaManopera.CaleRelativa,
                                        PresignedUrl = "empty"
                                    }
                                } : null,
                                LungimeCeruta = product.PcManopera != null ?
                                    product.PcManopera.NumeManoperaJson.NumeRomana == "STAN" ? product.PcManopera.MaterialFolosit.ToString() : "empty"
                                : "not_perdea",
                                InaltimeCeruta = product.IdSet != null ? product.InaltimeAleasaPentruSet : "not_set",
                                PretCurent = currency == "RON" ? product.PretCumparat : product.PretCumparat / 5 ,
                                Cantitate = product.NrBucati,
                                IdentificatorSet = item.Key  
                            }).ToList()
                        }).ToList(),
                    ClientDeliveryAddress = new AdreseDto
                    {
                        AliasDto = order.CAdresaLivrare.Alias,
                        TipAdresaDto = TipAdrese.Livrare,
                        BlocDto = order.CAdresaLivrare.Bloc,
                        NrBlocDto = order.CAdresaLivrare.NrBloc,
                        StradaDto = order.CAdresaLivrare.Strada,
                        NrStradaDto = order.CAdresaLivrare.NrStrada,
                        OrasDto = order.CAdresaLivrare.Locatie.Oras!,
                        JudetDto = order.CAdresaLivrare.Locatie.Judet!,
                        CodPostalDto = order.CAdresaLivrare.Locatie.CodPostal!,
                        IsDeletedDto = order.CAdresaLivrare.IsDeleted,
                        CifDto = null,
                        NumeFirmaDto = null
                    },
                    ClientBillingAddress = new AdreseDto
                    {
                        AliasDto = order.CAdresaFacturare.Alias,
                        TipAdresaDto = TipAdrese.Facturare,
                        BlocDto = order.CAdresaFacturare.Bloc,
                        NrBlocDto = order.CAdresaFacturare.NrBloc,
                        StradaDto = order.CAdresaFacturare.Strada,
                        NrStradaDto = order.CAdresaFacturare.NrStrada,
                        OrasDto = order.CAdresaFacturare.Locatie.Oras!,
                        JudetDto = order.CAdresaFacturare.Locatie.Judet!,
                        CodPostalDto = order.CAdresaFacturare.Locatie.CodPostal!,
                        IsDeletedDto = order.CAdresaFacturare.IsDeleted,
                        CifDto = order.CAdresaFacturare.DetaliuFactura!.Cif,
                        NumeFirmaDto = order.CAdresaFacturare.DetaliuFactura!.Cif
                    },
                    UserOrderDetails = new UserPersonalInfo
                    {
                        Nume = order.NumePeComanda,
                        Prenume = order.PrenumePeComanda,
                        NrTelefon = order.NrTelefonPeComanda,
                        Email = order.EmailPeComanda
                    },
                    OrderDate = order.DataEmitereComanda,
                    OrderId = order.IdComanda,
                    OrderBillNumber = order.BillNumberJson!,
                    OrderStatus = order.StatusComanda,
                    OrderPayment = order.TipPlata,
                    OrderTrackingString = order.AwbComanda,
                    OrderVoucher = order.IdVoucher != null ? new VouchereDto
                    {
                        CodVoucherDto = order.VoucherPeComanda!.CodVoucher,
                        ReducereDto = order.VoucherPeComanda.Reducere,
                        DataExpirareDto = default
                    } : null,
                    PretTotal = 0,
                    TotalProduse = 0,
                }).ToListAsync();

            if (clientOrders.Count > 0)
            {
                _logger.LogInformation("A LUAT COMENZILE");
            }
            else
            {
                _logger.LogError("NU A LUAT COMENZILE");

            }
          
            
            foreach (var order in clientOrders)
            {
                _logger.LogInformation($"orderId => {order.OrderId}");
                foreach (var item in order.Items)
                {
                    var isSet = !int.TryParse(item.Key, out _);
                    var exit = false; // true if end or false if not
                    foreach (var cartItem in item.CartItems)
                    {
                        switch (isSet)
                        {
                            // if we found the product!
                            case false:
                                order.PretTotal += item.CartItems[0].Cantitate * item.CartItems[0].PretCurent;
                                order.TotalProduse += item.CartItems[0].Cantitate;
                                break;
                            // else we found a set , and we only count once!
                            case true when !exit:
                                order.PretTotal += item.CartItems[0].Cantitate * item.CartItems[0].PretCurent;
                                order.TotalProduse += item.CartItems[0].Cantitate;
                                exit = true;
                                break;
                        }
                        if (cartItem.CuloareSelectata.ImaginiProdusDto!.Count <= 0) continue;
                        foreach (var image in cartItem.CuloareSelectata.ImaginiProdusDto)
                        {
                            image.PresignedUrl = await _bucketAcces.GenerateUrl(image.CaleImagineDto, image.FisierInBucketDto);
                        }
                        
                        if (cartItem.SelectedManopera == null) continue;
                        
                        if (cartItem.SelectedManopera.TipInel != null)
                        {
                            if(cartItem.SelectedManopera.TipInel.CaleRelativa == null) continue;
                            cartItem.SelectedManopera.TipInel.PresignedUrl = await
                                _bucketAcces.GenerateUrl(cartItem.SelectedManopera.TipInel.CaleRelativa!, "inele_prindere");
                        }
                        
                        if(cartItem.SelectedManopera.TipGalerie.CaleRelativa == null) continue;
                        cartItem.SelectedManopera.TipGalerie.PresignedUrl = await
                            _bucketAcces.GenerateUrl(cartItem.SelectedManopera.TipGalerie.CaleRelativa!, "tipuri_galerie");
                        
                        if(cartItem.SelectedManopera.TipLinie.CaleRelativa == null) continue;
                        cartItem.SelectedManopera.TipLinie.PresignedUrl = await
                            _bucketAcces.GenerateUrl(cartItem.SelectedManopera.TipLinie.CaleRelativa!, "tipuri_linie");
                    }
                }
            }
            return clientOrders;
        }
        catch (Exception e)
        {
            _logger.LogError("Error thrown on fetching client orders");
            _logger.LogError(e.Message);
            _logger.LogError(e.StackTrace);
            throw;
        }
        
    }

    public async Task<KeyValuePair<int , string>> PlaceOrder(int? accountId, Guid sessionId,  PlaceOrderDto orderToBePlaced,string currency = "RON" )
    {
        IDbContextTransaction? placeOrderTransaction = null;
        try
        {
            placeOrderTransaction =  await _unitOfWork.BeginTransactionAsync();
            var cartRepository = _unitOfWork.Repository<CosCumparaturi>();
            var orderRepository = _unitOfWork.Repository<Comenzi>();
            var cartItems = new List<IGrouping<string, CosCumparaturi>>();
            var wasAccountCreated = false;
            // confirmation order key
            var generateConfirmationOrderKey = Guid.NewGuid();
            // guest user
            if (accountId == null)
            {
                // var id = accountId;
                // retrieving his cart items
                cartItems = await cartRepository
                    .FindQueryable(item => item.IdCont == null && item.SessionId == sessionId)
                    .GroupBy(group => group.IdentificatorSet != "21" ? group.IdentificatorSet : group.IdProdusInCos.ToString())
                    .ToListAsync();

                var findAccount = await _unitOfWork.Repository<Conturi>()
                    .FindQueryable(acc => acc.Email == orderToBePlaced.EmailPeComanda)
                    .FirstOrDefaultAsync();

                if (findAccount == null)
                {
                    var createNewAccount = new Conturi
                    {
                        Nume = orderToBePlaced.NumePeComanda,
                        Prenume = orderToBePlaced.PrenumePeComanda,
                        NrTelefon = orderToBePlaced.NrTelefonPeComanda,
                        Username = null,
                        Email = orderToBePlaced.EmailPeComanda,
                        Parola = UserHelpers.CryptPassword(Guid.NewGuid().ToString()),
                        DataCreare = DateTime.UtcNow,
                        CodActivare = "GUEST",
                        Verificat = true,
                        IsGuest = true,
                        Rol = "Client",
                        OraLinkConfirmare = DateTime.UtcNow,
                    };

                    await _unitOfWork.Repository<Conturi>().AddAsync(createNewAccount);
                    await _unitOfWork.CommitAsync();
                    // generating id
                    accountId = createNewAccount.IdCont;
                    wasAccountCreated = true;
                }
                else
                {
                    wasAccountCreated = false;
                    accountId = findAccount.IdCont;
                }
                
            }
            else
            {
                var findAccountToUpdate = await _unitOfWork.Repository<Conturi>()
                    .FindQueryable(acc => acc.IdCont == accountId)
                    .FirstAsync();

                findAccountToUpdate.Nume ??= orderToBePlaced.NumePeComanda;
                findAccountToUpdate.Prenume ??= orderToBePlaced.PrenumePeComanda;
                findAccountToUpdate.NrTelefon ??= orderToBePlaced.NrTelefonPeComanda;
                
                await _unitOfWork.Repository<Conturi>().UpdateAsync(findAccountToUpdate);
            }
            
            
            // if user is logged in , then it did not enter  if(accountId == null) , hence no items were retrieved
            if (cartItems.IsNullOrEmpty())
            {
                // here accountId is not NULL and sessionId = Guid.Empty
                cartItems = await cartRepository
                    .FindQueryable(item => item.IdCont == accountId && item.SessionId == sessionId)
                    .GroupBy(group => group.IdentificatorSet != "21" ? group.IdentificatorSet : group.IdProdusInCos.ToString())
                    .ToListAsync();
            }
            
            
            var getLocation = await _unitOfWork.Repository<Locatii>()
                .FindQueryable(location => location.CodPostal == orderToBePlaced.AdresaLivrare.CodPostalDto
                                           && location.Judet == orderToBePlaced.AdresaLivrare.JudetDto
                                           && location.Oras == orderToBePlaced.AdresaLivrare.OrasDto)
                .FirstOrDefaultAsync();

            if (getLocation == null)
            {
                var newLocation = new Locatii
                {
                    Oras = orderToBePlaced.AdresaLivrare.OrasDto,
                    Judet = orderToBePlaced.AdresaLivrare.JudetDto,
                    CodPostal = orderToBePlaced.AdresaLivrare.CodPostalDto
                };
                await _unitOfWork.Repository<Locatii>().AddAsync(newLocation);
                await _unitOfWork.CommitAsync();
                getLocation = newLocation;
            }
                
                
            var getDeliveryAddress = await _unitOfWork.Repository<Adrese>()
                .FindQueryable(address =>
                    address.Alias == orderToBePlaced.AdresaLivrare.AliasDto && address.IdCont == accountId)
                .FirstOrDefaultAsync();

            if (getDeliveryAddress == null)
            {
                var newAddress = new Adrese
                {
                    Alias = orderToBePlaced.AdresaLivrare.AliasDto,
                    TipAdresa = TipAdrese.Livrare,
                    Bloc = orderToBePlaced.AdresaLivrare.BlocDto,
                    NrBloc = orderToBePlaced.AdresaLivrare.NrBlocDto,
                    Strada = orderToBePlaced.AdresaLivrare.StradaDto,
                    NrStrada = orderToBePlaced.AdresaLivrare.NrStradaDto,
                    IsDeleted = true,
                    IdLocatie = getLocation.IdLocatie,
                    IdCont = (int)accountId,
                };
                await _unitOfWork.Repository<Adrese>().AddAsync(newAddress);
                await _unitOfWork.CommitAsync();
                getDeliveryAddress = newAddress;
            }
            else
            {
                getDeliveryAddress.IsDeleted = true;
                await _unitOfWork.Repository<Adrese>().UpdateAsync(getDeliveryAddress);
            }


            Adrese? billingAddress = null;
          
            if (!orderToBePlaced.AdresaFacturare.CifDto.IsNullOrEmpty())
            {
                var getBillingLocation = await _unitOfWork.Repository<Locatii>()
                    .FindQueryable(location => location.CodPostal == orderToBePlaced.AdresaFacturare.CodPostalDto
                                               && location.Judet == orderToBePlaced.AdresaFacturare.JudetDto
                                               && location.Oras == orderToBePlaced.AdresaFacturare.OrasDto)
                    .FirstOrDefaultAsync();

                if (getBillingLocation == null)
                {
                    var newLocation = new Locatii
                    {
                        Oras = orderToBePlaced.AdresaFacturare.OrasDto,
                        Judet = orderToBePlaced.AdresaFacturare.JudetDto,
                        CodPostal = orderToBePlaced.AdresaFacturare.CodPostalDto
                    };
                    await _unitOfWork.Repository<Locatii>().AddAsync(newLocation);
                    await _unitOfWork.CommitAsync();
                    getBillingLocation = newLocation;
                }

                var getBillingDetails = await _unitOfWork.Repository<DetaliiFactura>()
                    .FindQueryable(details => details.Cif == orderToBePlaced.AdresaFacturare.CifDto
                                              && details.NumeFirma ==
                                              orderToBePlaced.AdresaFacturare.NumeFirmaDto!.ToUpper())
                    .FirstOrDefaultAsync();

                if (getBillingDetails == null)
                {
                    var newBillingDetails = new DetaliiFactura
                    {
                        Cif = orderToBePlaced.AdresaFacturare.CifDto,
                        NumeFirma = orderToBePlaced.AdresaFacturare.NumeFirmaDto!.ToUpper()
                    };
                    await _unitOfWork.Repository<DetaliiFactura>().AddAsync(newBillingDetails);
                    await _unitOfWork.CommitAsync();
                    getBillingDetails = newBillingDetails;
                }

                var getBillingAddress = await _unitOfWork.Repository<Adrese>()
                    .FindQueryable(address => address.Alias == orderToBePlaced.AdresaFacturare.AliasDto
                                              && address.IdCont == accountId &&
                                              address.IdDetaliuFactura == getBillingDetails.IdDetaliu)
                    .FirstOrDefaultAsync();


                if (getBillingAddress == null)
                {
                    var newBillingAddress = new Adrese
                    {
                        Alias = orderToBePlaced.AdresaFacturare.AliasDto,
                        TipAdresa = TipAdrese.Facturare,
                        Bloc = orderToBePlaced.AdresaFacturare.BlocDto,
                        NrBloc = orderToBePlaced.AdresaFacturare.NrBlocDto,
                        Strada = orderToBePlaced.AdresaFacturare.StradaDto,
                        NrStrada = orderToBePlaced.AdresaFacturare.NrStradaDto,
                        IsDeleted = true,
                        IdDetaliuFactura = getBillingDetails.IdDetaliu,
                        IdLocatie = getBillingLocation.IdLocatie,
                        IdCont = (int)accountId,
                    };
                    await _unitOfWork.Repository<Adrese>().AddAsync(newBillingAddress);
                    await _unitOfWork.CommitAsync();
                    getBillingAddress = newBillingAddress;
                }else
                {
                    getBillingAddress.IsDeleted = true;
                    await _unitOfWork.Repository<Adrese>().UpdateAsync(getBillingAddress);
                }

                billingAddress = getBillingAddress;
            }
            
            

            Vouchere? voucherApplied = null;
            
            if (orderToBePlaced.VoucherAplicat != null)
            {
               
                var getVoucher = await _unitOfWork.Repository<Vouchere>()
                    .FindQueryable(v => !v.IsDeleted && v.CodVoucher == orderToBePlaced.VoucherAplicat.CodVoucherDto.ToUpper()
                                                         .ToUpper())
                    .FirstOrDefaultAsync();


                if (getVoucher == null || getVoucher.DataExpirare >= DateTime.UtcNow)
                {
                    _logger.LogError("Voucher not found. Cancelling transaction and payment");
                    return new KeyValuePair<int, string>(-1, "Voucher not found / Expired voucher");
                }

                voucherApplied = getVoucher;
            }
            
            if (voucherApplied != null)
            {
                // 0.99 , 0.53
                // comanda : 900 , reducere de 5% => 0.05 
                orderToBePlaced.PretTotal -=  orderToBePlaced.PretTotal * voucherApplied.Reducere;
            }
            
            // poate aici genereaza awb pt curier , daca se intampla ceva , anuleaza !;
            // 75 + 100 + 180 + 75 = 
            var newOrder = new Comenzi
            {
                DataEmitereComanda = DateTime.UtcNow,
                StatusComanda = StatusComanda.InProcesare,
                TipPlata = orderToBePlaced.TipPlata,
                AwbComanda = "temporary",
                PretTransport = orderToBePlaced.PretTransport,
                NumePeComanda = orderToBePlaced.NumePeComanda,
                PrenumePeComanda = orderToBePlaced.PrenumePeComanda,
                NrTelefonPeComanda = orderToBePlaced.NrTelefonPeComanda,
                EmailPeComanda = orderToBePlaced.EmailPeComanda,
                UniqueConfirmationToken = generateConfirmationOrderKey.ToString(),
                UniqueConfirmationTokenUsed = false,
                IsCancelable = true,
                IdAdresaLivrare = getDeliveryAddress.IdAdresa,
                IdAdresaFacturare = billingAddress?.IdAdresa ?? getDeliveryAddress.IdAdresa,
                IdVoucher = null,
                BillNumberJson = new NumarFactura
                {
                    NumarEngleza = null,
                    NumarRomana = null
                }
            };

            await orderRepository.AddAsync(newOrder);
            await _unitOfWork.CommitAsync();

            var lockProductsResponse = await LockProductsThatAreBeingBought(cartItems);

            if (lockProductsResponse == -1)
            {
                throw new InvalidDataException("Something happened in the LockProductsThatAreBeingBought function. Cancel transactions.");
            }
            
            var itemsOnOrder = cartItems.SelectMany(item => item).Select(itemInCart => new ProduseCuComenzi
            {
                NrBucati = itemInCart.CantitateProdus,
                PretCumparat = voucherApplied != null ? itemInCart.PretProdus - itemInCart.PretProdus * voucherApplied.Reducere : itemInCart.PretProdus,
                InaltimeAleasaPentruSet = itemInCart.InaltimeAleasaPentruSet,
                IdentificatorSet = itemInCart.IdentificatorSet,
                IdSet = itemInCart.IdSet,
                IdProdus = itemInCart.IdProdus,
                IdComanda = newOrder.IdComanda,
                IdCuloare = itemInCart.IdCuloare,
                IdDimensiune = itemInCart.IdDimensiune,
                IdManopera = itemInCart.IdManopera,
            }).ToList();
            // add products to orders table
            var orderConfirmationString = newOrder.UniqueConfirmationToken;
            var orderId = newOrder.IdComanda;
            await _unitOfWork.Repository<ProduseCuComenzi>().AddRangeAsync(itemsOnOrder);
            
            if (orderToBePlaced.TipPlata == TipPlata.Card)
            {
                var successfull = true;
                if (successfull)
                {
                    newOrder.IsOrderPayed = true;
                    await orderRepository.UpdateAsync(newOrder);
                }
                else
                {
                    throw new PaymentRejectedException("Payment unsuccesfull. ");
                }
            }
            
            await _unitOfWork.CommitTransactionAsync(placeOrderTransaction);
            
            
            /*
             * if payment succefull or it was cash payment.
             *  contact courier and place order there!
             */
            
            // courier here
            //-----
            // courier here
            
            // bill generated!
            var generateBillResponse = await _documentProcessing.GenerateBill(orderId, currency);
            if (generateBillResponse.Key == 1)
            {
                _logger.LogInformation($"Bill generated succefully for currency {currency}, order id {orderId}. Invoice number {generateBillResponse.Value}");
            }else if (generateBillResponse.Key == -4)
            {
                _logger.LogInformation($"{generateBillResponse.Value}");
            }else if (generateBillResponse.Key == -3)
            {
                _logger.LogInformation($"{generateBillResponse.Value}");
            }
            /*
             *
             * PAYMENT API ( if succesfull ) => put products into the order with products table => confirmation page
             * IF PAYMENT IS UNSUCCEFULL IN ANY WAY , UNLOCK PRODUCTS AND RETRY!
             * 
             */
            
            
            // opening new transaction
            var unlockProductsResponse = await UnlockProductsThatWereBought( (int)accountId , sessionId , wasAccountCreated , orderId );
            if (unlockProductsResponse == -1)
            {
                throw new InvalidDataException(
                    "Something happened in the UnlockProductsThatWereBought . Cancel transactions");
            }

           
            
            return new KeyValuePair<int, string>(1 , $"{orderConfirmationString}|{orderId}");
        }
        catch (Exception e)
        {
            if (placeOrderTransaction != null)
            {
                _logger.LogError("Rolling back placing order transaction. Error occured");
                await _unitOfWork.RollBackTransactionAsync(placeOrderTransaction);
            }

            switch (e)
            {
                case InvalidOperationException:
                    _logger.LogError("An error occured! . Cancelling transaction and payment");
                    return new KeyValuePair<int, string>(-3, "An error occured");
                case PaymentRejectedException:
                    _logger.LogError("PAYMENT REJECTED . Cancelling transaction and payment");
                    return new KeyValuePair<int, string>(-5, "Payment rejected");
                default:
                    return new KeyValuePair<int, string>(-2 , "General error occured");
            }
        }
    }

    public async Task<int> ConfirmPage(string confirmationId, int orderId)
    {
        IDbContextTransaction? updateTransaction = null;
        try
        {
            updateTransaction = await _unitOfWork.BeginTransactionAsync();
            var findOrder = await _unitOfWork.Repository<Comenzi>()
                .FindQueryable(o => o.IdComanda == orderId)
                .FirstAsync();

            if (findOrder.UniqueConfirmationTokenUsed)
            {
                return -2;
            }
            
            findOrder.UniqueConfirmationTokenUsed = true;
            await _unitOfWork.Repository<Comenzi>().UpdateAsync(findOrder);
            await _unitOfWork.CommitTransactionAsync(updateTransaction);
            return 1;
        }
        catch (Exception e)
        {
            if (updateTransaction != null)
            {
                _logger.LogError("Rolling back confirmation page transaction");
                await _unitOfWork.RollBackTransactionAsync(updateTransaction);
            }
            _logger.LogError("Error thrown in confirm page method");
            _logger.LogError(e.Message);
            _logger.LogError(e.StackTrace);
            return -1;
        }
    }

    public async Task<KeyValuePair<int , Stream?>> ReturnPdfBill(int orderId, string currency = "RON")
    {
        const string invoiceApiEndpoint = "https://ws.smartbill.ro/SBORO/api/invoice/pdf";

        var getOrderWithId = await _unitOfWork.Repository<Comenzi>()
            .FindQueryable(order => order.IdComanda == orderId)
            .FirstOrDefaultAsync();

        if (getOrderWithId is null)
        {
            return new KeyValuePair<int, Stream?>(-1,null);
        }

        if (currency == "RON")
        {
            if (getOrderWithId.BillNumberJson!.NumarRomana == null)
            {
                return new KeyValuePair<int, Stream?>(-4, null);
            }
        }
        else
        {
            if (getOrderWithId.BillNumberJson!.NumarEngleza == null)
            {
                return new KeyValuePair<int, Stream?>(-4, null);
            }
        }

       
        
        var getInvoiceCredentials = await _unitOfWork.Repository<GlobalConfigs>()
            .FindQueryable(setting => InvoiceCredentials.Contains(setting.NumeAtributGlobal))
            .ToListAsync();

        var username = getInvoiceCredentials.Find(p => p.NumeAtributGlobal == "smart_bill_username");
        var password = getInvoiceCredentials.Find(p => p.NumeAtributGlobal == "smart_bill_password");
        var cif = getInvoiceCredentials.Find(p => p.NumeAtributGlobal == "cif");
        
        if (username == null || password == null || cif == null ||
            username.NumeAtributGlobal.IsNullOrEmpty() ||
            password.NumeAtributGlobal.IsNullOrEmpty() ||
            cif.NumeAtributGlobal.IsNullOrEmpty())
        {
            return new KeyValuePair<int, Stream?>(-2,null); // credentials missing

        }
        
        var invoiceApiEndpointOptions = new RestClientOptions(invoiceApiEndpoint)
        {
            Authenticator = new HttpBasicAuthenticator(username.ValoareAtributGlobal , password.ValoareAtributGlobal),
        };
        var client = new RestClient(invoiceApiEndpointOptions);
        var request = new RestRequest
        {
            Method = Method.Get,
        };
        _logger.LogInformation($"{(currency == "RON" ? $"Downloadare factura in moneda RON , limba RO cu nr {getOrderWithId.BillNumberJson.NumarRomana}" :
            $"Downloadare factura in moneda EUR , limba EN cu nr {getOrderWithId.BillNumberJson.NumarEngleza}")}");
        request.AddHeader("Accept", "application/octet-stream");
        
        request.AddQueryParameter("cif", $"{cif.ValoareAtributGlobal}");
        request.AddQueryParameter("seriesname", SeriesNumber);
        request.AddQueryParameter("number", $"{(currency == "RON" ? getOrderWithId.BillNumberJson.NumarRomana : getOrderWithId.BillNumberJson.NumarEngleza)}");
        
        var response = await client.ExecuteAsync(request);

        if (response.IsSuccessful)
        {
            var rawPdfBytes = response.RawBytes;
            var memoryStream = new MemoryStream(rawPdfBytes!);
            _logger.LogInformation($"Succefully visualized bill for order {orderId}");
            return new KeyValuePair<int, Stream?>(1 , memoryStream);
        }
        else
        {
            _logger.LogError(response.ErrorMessage);
            _logger.LogInformation($"Error occured when trying to visualize bill with id {orderId}");
            return new KeyValuePair<int, Stream?>(-3 , null);
        }

    }

    public async Task<int> SendBillOnMail(int orderId, string email, string currency = "RON")
    {
        _logger.LogInformation($"order id {orderId}"); const string sendBillOnEmailEndpoint = "https://ws.smartbill.ro/SBORO/api/document/send";
        var getOrderWithId = await _unitOfWork.Repository<Comenzi>()
            .FindQueryable(order => order.IdComanda == orderId)
            .FirstOrDefaultAsync();
        if (getOrderWithId is null)
        {
            return -1;
        }

        var getInvoiceCredentials = await _unitOfWork.Repository<GlobalConfigs>()
            .FindQueryable(setting => InvoiceCredentials.Contains(setting.NumeAtributGlobal))
            .ToListAsync();

        var username = getInvoiceCredentials.Find(p => p.NumeAtributGlobal == "smart_bill_username");
        var password = getInvoiceCredentials.Find(p => p.NumeAtributGlobal == "smart_bill_password");
        var cif = getInvoiceCredentials.Find(p => p.NumeAtributGlobal == "cif");

        if (username == null || password == null || cif == null ||
            username.NumeAtributGlobal.IsNullOrEmpty() ||
            password.NumeAtributGlobal.IsNullOrEmpty() ||
            cif.NumeAtributGlobal.IsNullOrEmpty())
        {
            return -2; // credentials not yet configured by admin.
        }
        
        
        var toBase64Subject =
            Convert.ToBase64String(
                Encoding.UTF8.GetBytes(
                    $"{(currency == "RON" ? "Buna ziua. Factura dvs" : "Good afternoon. Your bill")}"));

        var toBase64BodyText = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{(currency == "RON" ? $"Aveti atasata factura comenzii cu numarul {orderId}\nVa dorim o zi buna in continuare\n\n Cu stima,\n Echipa takDecor" 
            : $"In the attachement section, you can find the bill with number {orderId}\nWe wish you a good day\n\n With pleasure,\ntakDecor team")}"));

        var jsonBody = new
        {
            type = "factura",
            companyVatCode = cif.ValoareAtributGlobal,
            seriesName = SeriesNumber,
            number = currency == "RON" ? getOrderWithId.BillNumberJson!.NumarRomana : getOrderWithId.BillNumberJson!.NumarEngleza,
            to = email,
            subject = toBase64Subject,
            bodyText = toBase64BodyText
        };

        var invoiceApiEndpointOptions = new RestClientOptions(sendBillOnEmailEndpoint)
        {
            Authenticator = new HttpBasicAuthenticator(username.ValoareAtributGlobal, password.ValoareAtributGlobal),
        };
        var client = new RestClient(invoiceApiEndpointOptions);
        var request = new RestRequest
        {
            Method = Method.Post,
        };
        request.AddJsonBody(JsonConvert.SerializeObject(jsonBody, Formatting.Indented));
        request.AddHeader("Content-Type", "application/json");
        // request.AddHeader("Accept", "application/xml");
        request.AddHeader("Accept", "application/json");

        var response = await client.ExecuteAsync(request);
        _logger.LogInformation(response.Content);
        if (response.IsSuccessful)
        {
            _logger.LogInformation($"Succesfully sent bill on email for order with number {orderId}");
            return 1;
        }
        else
        {
            _logger.LogError($"An error occured when sending bill on email for order with number {orderId}");
            _logger.LogError(response.ErrorMessage);
            return -1;
        }
        
    }

    private async Task<int> LockProductsThatAreBeingBought(IList<IGrouping<string, CosCumparaturi>> getCartItemsThatWillBePurchased)
    {
        try
        {
            
            var productsRepository = _unitOfWork.Repository<Produse>();
            var bundlesRepository = _unitOfWork.Repository<Seturi>();
            var manopereRepository = _unitOfWork.Repository<Manopere>();
            var ringTypesRepository = _unitOfWork.Repository<InelePrindere>();
            var galleryTypesRepository = _unitOfWork.Repository<TipuriGalerie>();
            var lineTypesRepository = _unitOfWork.Repository<TipuriLinie>();

            var lockedProducts = new List<Produse>();
            var lockedSets = new List<Seturi>();
            var lockedRingTypes = new List<InelePrindere>();
            var lockedGalleryTypes = new List<TipuriGalerie>();
            var lockedLineTypess = new List<TipuriLinie>();
            var lockedManoperas = new List<Manopere>();

            
            foreach (var items in getCartItemsThatWillBePurchased)
            {
                var cartItems = items.Select(g => g).ToList();
                if (cartItems.Count > 0)
                {
                    _logger.LogInformation("CART ITEMS NOT EMPTY IN LOCK FUNCTION");
                }
                foreach (var item in items)
                {
                    if (!int.TryParse(items.Key, out _))
                    {
                       
                        // we are in set
                        var findSet = await bundlesRepository.FindQueryable(s => s.IdSet == item.IdSet)
                            .FirstAsync();
                       
                        if (!findSet.IsLocked)
                        {
                            findSet.IsLocked = true;
                            lockedSets.Add(findSet);
                        }

                        var selectFromSet = items.Where(cartItem => cartItem.InaltimeAleasaPentruSet != null)
                            .Select(id => id.IdManopera)
                            .ToList();

                        var findManopereInDbThatNeedToBeLocked = await manopereRepository
                            .FindQueryable(manopera => selectFromSet.Contains(manopera.IdManopera))
                            .ToListAsync();

                        if (findManopereInDbThatNeedToBeLocked.Count > 0)
                        {
                            foreach (var manopera in findManopereInDbThatNeedToBeLocked.Where(manopera => !manopera.IsLocked))
                            {
                                manopera.IsLocked = true;
                            }
                            lockedManoperas.AddRange(findManopereInDbThatNeedToBeLocked);
                        }
                        

                    }
                    else
                    {
                        var findProduct = await productsRepository.FindQueryable(p => p.IdProdus == item.IdProdus)
                            .FirstAsync();

                        if (!findProduct.IsLocked)
                        {
                            findProduct.IsLocked = true;
                            lockedProducts.Add(findProduct);
                        }

                        if (findProduct.TipulProdusuluiJson.TipProdusRomana is "perdea" or "draperie")
                        {
                            var findManoperaOfTheCurrentProduct = await manopereRepository
                                .FindQueryable(man => man.IdManopera == item.IdManopera)
                                .FirstAsync();

                            if (findManoperaOfTheCurrentProduct.IdInelPrindere != null)
                            {
                                var findRingToLockOfTheCurrentManopera = await ringTypesRepository
                                    .FindQueryable(
                                        ring => ring.IdInel == findManoperaOfTheCurrentProduct.IdInelPrindere)
                                    .FirstAsync();

                                if (!findRingToLockOfTheCurrentManopera.IsLocked)
                                {
                                    findRingToLockOfTheCurrentManopera.IsLocked = true;
                                }
                                
                                lockedRingTypes.Add(findRingToLockOfTheCurrentManopera);
                            }
                            
                            var findGalleryTypeToLockOfTheCurrentManopera = await galleryTypesRepository
                                .FindQueryable(
                                    tg => tg.IdTipGalerie == findManoperaOfTheCurrentProduct.IdTipGalerie)
                                .FirstAsync();
                            
                            if (!findGalleryTypeToLockOfTheCurrentManopera.IsLocked)
                            {
                                findGalleryTypeToLockOfTheCurrentManopera.IsLocked = true;
                            }
                            
                            lockedGalleryTypes.Add(findGalleryTypeToLockOfTheCurrentManopera);
                            
                            var findLiningTypeToLockOfTheCurrentManopera = await lineTypesRepository
                                .FindQueryable(
                                    tl => tl.IdTipLinie == findManoperaOfTheCurrentProduct.IdTipLinie)
                                .FirstAsync();
                            
                            if (!findLiningTypeToLockOfTheCurrentManopera.IsLocked)
                            {
                                findLiningTypeToLockOfTheCurrentManopera.IsLocked = true;
                            }
                            
                            lockedLineTypess.Add(findLiningTypeToLockOfTheCurrentManopera);
                            
                        }
                    }
                }
            }

            
            // 🔹 Update the locked items in the database
            if (lockedProducts.Count > 0) await productsRepository.UpdateRangeAsync(lockedProducts);
            if (lockedSets.Count > 0) await bundlesRepository.UpdateRangeAsync(lockedSets);
            if (lockedManoperas.Count > 0) await manopereRepository.UpdateRangeAsync(lockedManoperas);
            if (lockedRingTypes.Count > 0) await ringTypesRepository.UpdateRangeAsync(lockedRingTypes);
            if (lockedLineTypess.Count > 0) await lineTypesRepository.UpdateRangeAsync(lockedLineTypess);
            if (lockedGalleryTypes.Count > 0) await galleryTypesRepository.UpdateRangeAsync(lockedGalleryTypes);

            await _unitOfWork.CommitAsync();
            _logger.LogInformation($"Succefully locked items for client  ");
            return 1;
        }
        catch (Exception e)
        {
            _logger.LogError(e.InnerException?.ToString());
            _logger.LogError(e.Message);
            _logger.LogError(e.StackTrace);
            return -1;
        }
    }

    private async Task<int> UnlockProductsThatWereBought(int accountId, Guid sessionId , bool wasAccountCreated, int orderId)
    {
        IDbContextTransaction? unlockTransaction = null;
        try
        {
            unlockTransaction = await _unitOfWork.BeginTransactionAsync();
            var productsRepository = _unitOfWork.Repository<Produse>();
            var bundlesRepository = _unitOfWork.Repository<Seturi>();
            var manopereRepository = _unitOfWork.Repository<Manopere>();
            var ringTypesRepository = _unitOfWork.Repository<InelePrindere>();
            var galleryTypesRepository = _unitOfWork.Repository<TipuriGalerie>();
            var lineTypesRepository = _unitOfWork.Repository<TipuriLinie>();
            var cartRepository = _unitOfWork.Repository<CosCumparaturi>();
            
            var currentItemsThatWereOrdered = await cartRepository
                .FindQueryable(item => item.IdCont == ( wasAccountCreated ? null : accountId) 
                                       && item.SessionId == (wasAccountCreated ? sessionId : Guid.Empty))
                .GroupBy(group => group.IdentificatorSet != "21" ? group.IdentificatorSet : group.IdProdusInCos.ToString())
                .ToListAsync();
            
            var productsToUnlock = new List<Produse>();
            var setsToUnlock = new List<Seturi>();
            var ringsToUnlock = new List<InelePrindere>();
            var galleryTypesToUnlock = new List<TipuriGalerie>();
            var lineTypesToUnlock = new List<TipuriLinie>();
            var manoperasToUnlock = new List<Manopere>();

            
            foreach (var items in currentItemsThatWereOrdered)
            {
                foreach (var item in items)
                {
                    if (!int.TryParse(items.Key, out _))
                    {
                        // we are in set
                        var findSet = await bundlesRepository.FindQueryable(s => s.IdSet == item.IdSet)
                            .FirstAsync();

                        if (findSet.IsLocked)
                        {
                            findSet.IsLocked = false;
                            setsToUnlock.Add(findSet);
                        }

                        var selectFromSet = items.Where(cartItem => cartItem.InaltimeAleasaPentruSet != null)
                            .Select(id => id.IdManopera)
                            .ToList();

                        var findManopereInDbThatNeedToBeLocked = await manopereRepository
                            .FindQueryable(manopera => selectFromSet.Contains(manopera.IdManopera))
                            .ToListAsync();

                        if (findManopereInDbThatNeedToBeLocked.Count > 0)
                        {
                            foreach (var manopera in findManopereInDbThatNeedToBeLocked.Where(manopera => manopera.IsLocked))
                            {
                                manopera.IsLocked = false;
                            }

                            manoperasToUnlock.AddRange(findManopereInDbThatNeedToBeLocked);
                        }
                        

                    }
                    else
                    {
                        var findProduct = await productsRepository.FindQueryable(p => p.IdProdus == item.IdProdus)
                            .FirstAsync();

                        if (findProduct.IsLocked)
                        {
                            findProduct.IsLocked = false;
                            productsToUnlock.Add(findProduct);
                        }

                        if (findProduct.TipulProdusuluiJson.TipProdusRomana is "perdea" or "draperie")
                        {
                            var findManoperaOfTheCurrentProduct = await manopereRepository
                                .FindQueryable(man => man.IdManopera == item.IdManopera)
                                .FirstAsync();

                            if (findManoperaOfTheCurrentProduct.IdInelPrindere != null)
                            {
                                var findRingToLockOfTheCurrentManopera = await ringTypesRepository
                                    .FindQueryable(
                                        ring => ring.IdInel == findManoperaOfTheCurrentProduct.IdInelPrindere)
                                    .FirstAsync();

                                if (findRingToLockOfTheCurrentManopera.IsLocked)
                                {
                                    findRingToLockOfTheCurrentManopera.IsLocked = false;
                                }
                                
                                ringsToUnlock.Add(findRingToLockOfTheCurrentManopera);
                            }
                            
                            var findGalleryTypeToLockOfTheCurrentManopera = await galleryTypesRepository
                                .FindQueryable(
                                    tg => tg.IdTipGalerie == findManoperaOfTheCurrentProduct.IdTipGalerie)
                                .FirstAsync();
                            
                            if (findGalleryTypeToLockOfTheCurrentManopera.IsLocked)
                            {
                                findGalleryTypeToLockOfTheCurrentManopera.IsLocked = false;
                            }
                            
                            galleryTypesToUnlock.Add(findGalleryTypeToLockOfTheCurrentManopera);
                            
                            var findLiningTypeToLockOfTheCurrentManopera = await lineTypesRepository
                                .FindQueryable(
                                    tl => tl.IdTipLinie == findManoperaOfTheCurrentProduct.IdTipLinie)
                                .FirstAsync();
                            
                            if (findLiningTypeToLockOfTheCurrentManopera.IsLocked)
                            {
                                findLiningTypeToLockOfTheCurrentManopera.IsLocked = false;
                            }
                            
                            lineTypesToUnlock.Add(findLiningTypeToLockOfTheCurrentManopera);
                            
                        }
                    }
                }
            }

            
            // 🔹 Update the locked items from LOCKED to UNLOCKED
            if (productsToUnlock.Count > 0) await productsRepository.UpdateRangeAsync(productsToUnlock);
            if (setsToUnlock.Count > 0) await bundlesRepository.UpdateRangeAsync(setsToUnlock);
            if (manoperasToUnlock.Count > 0) await manopereRepository.UpdateRangeAsync(manoperasToUnlock);
            if (ringsToUnlock.Count > 0) await ringTypesRepository.UpdateRangeAsync(ringsToUnlock);
            if (lineTypesToUnlock.Count > 0) await lineTypesRepository.UpdateRangeAsync(lineTypesToUnlock);
            if (galleryTypesToUnlock.Count > 0) await galleryTypesRepository.UpdateRangeAsync(galleryTypesToUnlock);

            // await _unitOfWork.CommitAsync();
            await _unitOfWork.CommitTransactionAsync(unlockTransaction);
            _logger.LogInformation($"Succefully unlocked items for client  ");

             await SendOrderConfirmationMail(accountId, wasAccountCreated, orderId);
             // trimite mail si la admin aici !
             var response = await RemoveFromCartAfterSuccessfullOrder(accountId, sessionId, wasAccountCreated);
             if (response == 1)
             {
                 _logger.LogInformation("Removed items from cart");
             }
             else
             {
                 _logger.LogError("Critical error on removing products from cart");
             }
            
            return 1;
            
        }
        catch (Exception e)
        {
            if (unlockTransaction != null)
            {
                await _unitOfWork.RollBackTransactionAsync(unlockTransaction);
                _logger.LogError("Rolling back transaction of unlocking products . Check LOGS");
            }
            _logger.LogError(e.Message);
            _logger.LogError(e.StackTrace);
            return -1;
        }
    }

    private async Task<int> RemoveFromCartAfterSuccessfullOrder(int accountId, Guid sessionId , bool wasAccountCreated)
    {
        IDbContextTransaction? deleteTransaction = null;
        try
        {
            _logger.LogInformation($"{wasAccountCreated} / {accountId} / {sessionId}");
            deleteTransaction = await _unitOfWork.BeginTransactionAsync();
            var cartRepository = _unitOfWork.Repository<CosCumparaturi>();
            var currentItemsThatWereOrdered = await cartRepository
                .FindQueryable(item => item.IdCont == ( wasAccountCreated ? null : accountId) 
                                       && item.SessionId == (wasAccountCreated ? sessionId : Guid.Empty))
                .ToListAsync();


            await cartRepository.DeleteRangeAsync(currentItemsThatWereOrdered);
            await _unitOfWork.CommitTransactionAsync(deleteTransaction);

            return 1;
        }
        catch (Exception e)
        {
            if (deleteTransaction != null)
            {
                await _unitOfWork.RollBackTransactionAsync(deleteTransaction);
                _logger.LogError("Rolling back transaction of deleting products from cart . Check LOGS");
            }
            _logger.LogError(e.Message);
            _logger.LogError(e.StackTrace);
            return -1;
        }
        
    }

    private async Task SendOrderConfirmationMail(int accountId , bool wasAccountCreated , int orderId)
    {
        _logger.LogInformation("Sending order confirmation mail...");
        try
        {
            var findAccountEmail = await _unitOfWork.Repository<Conturi>()
                .FindQueryable(acc => acc.IdCont == accountId)
                .FirstOrDefaultAsync();

            if (findAccountEmail == null)
            {
                _logger.LogError($"No account found in the database with the id {accountId}");
                return;
            }

            var getDockerEnv = Environment.GetEnvironmentVariable("DOCKER");
            var siteUrl = getDockerEnv != "true" ? "http://localhost:3000" : "https://www.texxshop.ro";
            
            var accountCreationMessageEn = wasAccountCreated
                ? $"<mj-text font-size=\"18px\" color=\"black\">\n   " +
                  $"       We have automatically created you an account on our website so that you can see your order on the e-mail address that you've ordered : <strong>{findAccountEmail.Email}</strong> and password : <strong>{findAccountEmail.Parola}</strong>. We recommend you that you reset the password so that you can log in normally. Thank you for the order !\n     " +
                  $"   </mj-text>\n  "
                : ""; 
            
            var accountCreationMessageRo = wasAccountCreated
                ? $"<mj-text font-size=\"18px\" color=\"black\">\n   " +
                  $"      V-am creat automat un cont pentru a va putea comenzile la adresa de mail pe care ati facut comanda : <strong>{findAccountEmail.Email}</strong> si  parola : <strong>{findAccountEmail.Parola}</strong>. Va recomandam sa va resetati parola pentru a va putea loga normal. Multumim de cumparaturi !\n " +
                  $"   </mj-text>\n  "
                : ""; 
            
            var url = await _bucketAcces.GenerateUrl("LogoTexx.png" , null);
            var insertLogo = url != null
                ? $"<mj-section>\n" +
                  $" <mj-column>\n" +
                  $"  <mj-image width=\"100px\" src=\"{url}\" alt=\"Company Logo\"/>\n" +
                  $" </mj-column>\n" +
                  $"</mj-section>"
                : "";
            
            var mjmlTemplateAdmin = $"<mjml>\n" +
                               $"  <mj-body>\n" +
                               $"   {insertLogo}" +
                               $"    <mj-section>\n" +
                               $"      <mj-column>\n" +
                               $"         <mj-text font-size=\"18px\" color=\"#F45E43\" font-family=\"helvetica\" align=\"center\">O noua comanda plasata</mj-text>\n" +
                               $"         <mj-spacer></mj-spacer>\n" +
                               $"      </mj-column>\n" +
                               $"      <mj-column background-color=\"#a8a8a8\" border-radius=\"20px\" padding=\"20px\" width=\"100%\">\n" +
                               $"         <mj-text font-size=\"18px\" color=\"#333333\">\n" +
                               $"            O noua comanda a fost plasata. O puteti vedea aici :\n" +
                               $"         </mj-text>\n" +
                               $"         <mj-text font-size=\"18px\" color=\"#333333\">\n" +
                               $"            Intrati pe site la sectiunea de comenzi si vedeti ultima comanda in functie de data\n" +
                               $"         </mj-text>\n" +
                               $"      </mj-column>\n" +
                               $"    </mj-section>\n" +
                               $"  </mj-body>\n" +
                               $"</mjml>";


            var loginOrProfileRo = wasAccountCreated
                ? $"{siteUrl}/user/login"
                : $"{siteUrl}/user/profile/orders";
            
            var loginOrProfileEn = wasAccountCreated
                ? $"{siteUrl}/en/user/login"
                : $"{siteUrl}/en/user/profile/orders";

            var mjmlTemplate = $"<mjml>\n" +
                               $"  <mj-body>\n " +
                               $"{insertLogo}" +
                               $"   <mj-section>\n " +
                               $"     <mj-column>\n" +
                               $"        <mj-text font-size=\"18px\" color=\"#F45E43\" font-family=\"helvetica\" align=\"center\">Confirmare comenzii  / Order confirmation </mj-text>\n " +
                               $"       <mj-spacer></mj-spacer>\n    " +
                               $"    </mj-column>\n    " +
                               $"  <mj-column background-color=\"#a8a8a8\" border-radius=\"20px\" padding=\"20px\" width=\"100%\">\n   " +
                               $"     <mj-text font-size=\"18px\" color=\"#333333\">\n      " +
                               $"    <strong>Confirmare comenzii #{orderId} / Order confirmation #{orderId}</strong>\n " +
                               $"       </mj-text>\n    " +
                               $"    <mj-text font-size=\"22px\" color=\"#F45E43\">\n      " +
                               $"    RO\n      " +
                               $"  </mj-text>\n  " +
                               $"      <mj-text font-size=\"18px\" color=\"black\">\n " +
                               $"         Comanda dumneavoastra este pe drum ! Va ajunge la dvs. in cel mai scurt timp.\n  " +
                               $"      </mj-text>\n     " +
                               accountCreationMessageRo +
                               $"     <mj-text font-size=\"18px\">Puteti vedea detaliile comenzii dvs. in sectiunea profilului / comenzi. Click pentru a va loga. </mj-text>\n   " +
                               $"     <mj-button color=\"white\" background-color=\"black\">\n    " +
                               $"      <a href=\"{loginOrProfileRo}\">CLICK AICI</a>\n   " +
                               $"     </mj-button>\n " +
                               $"       <mj-text font-size=\"22px\" color=\"#F45E43\">\n  " +
                               $"        EN\n " +
                               $"       </mj-text>\n " +
                               $"       <mj-text font-size=\"18px\" color=\"black\">\n  " +
                               $"        The order is on your way ! It will get to you in the shortest time.\n    " +
                               $"    </mj-text>\n   " +
                               accountCreationMessageEn +
                               $"      <mj-text font-size=\"18px\">You can see the details of your order in the profile section / orders . Click for log-in</mj-text>\n    " +
                               $"    <mj-button color=\"white\" background-color=\"black\">\n         " +
                               $" <a href=\"{loginOrProfileEn}\">CLICK HERE</a>\n     " +
                               $"   </mj-button>\n\n   " +
                               $"   </mj-column>\n  " +
                               $"  </mj-section>\n" +
                               $"  </mj-body>\n" +
                               $"</mjml>";
           
            var convertToHtml = await _mjmlService.ConvertMjmlToHtml(mjmlTemplate);
            var convertToHtmlAdmin = await _mjmlService.ConvertMjmlToHtml(mjmlTemplateAdmin);
            if (convertToHtml != null)
            {
                await _emailService.SendEmailAsync(findAccountEmail.Email!, $"Confirmare comanda {orderId} / Order confirmation {orderId}" , convertToHtml);
            }

            if (convertToHtmlAdmin != null)
            {
                await _emailService.SendEmailAsync("test.webb1932@gmail.com", $"O noua comanda plasata" , convertToHtmlAdmin);
            }
            
            _logger.LogInformation("Sent order confirmation email succefully!");
            
        }
        catch (Exception e)
        {
            _logger.LogError("Error when trying to send order confirmation email");
            _logger.LogError(e.Message);
            _logger.LogError(e.StackTrace);
        }
    }
}