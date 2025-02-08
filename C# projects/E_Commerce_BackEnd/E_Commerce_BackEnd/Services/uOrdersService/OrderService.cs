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
using E_Commerce_BackEnd.Models.ProductVouchersModels;
using E_Commerce_BackEnd.Models.UserRelatedModels;
using E_Commerce_BackEnd.Services.Helpers.AWS_Secret.AWSBucket_CRUD;
using E_Commerce_BackEnd.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.IdentityModel.Tokens;

namespace E_Commerce_BackEnd.Services.uOrdersService;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBucketAcces _bucketAcces;
    private readonly ILogger<OrderService> _logger;
    

    public OrderService(IUnitOfWork unitOfWork, IBucketAcces bucketAcces, ILogger<OrderService> logger)
    {
        _unitOfWork = unitOfWork;
        _bucketAcces = bucketAcces;
        _logger = logger;
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
                                NumeSet = product.Set!.NumeSet,
                                CodProdus = product.Produs.CodProdus,
                                NumeProdus =  product.Produs.NumeProdus!,
                                TipProdus = product.Produs.TipulProdusului,
                                CuloareSelectata = new CuloriDto
                                {
                                    IdCuloare = product.PcCuloare.IdCuloare,
                                    NumeCuloareDto = product.PcCuloare.NumeCuloare,
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
                                    LungimeDto = product.PcManopera!.NumeManopera != "STAN" ?  product.PcDimensiune.Lungime : ((int)(product.PcManopera.MaterialFolosit * 100)).ToString(),
                                    LatimeDto = product.PcDimensiune.Lungime,
                                    RecomandarePat = product.PcDimensiune.RecomandarePat,
                                    PretDto = 0,
                                    PretRedusDto = 0,
                                    JustAdded = false,
                                    PerdeaEstePerecheDto = product.PcManopera!.NumeManopera == "STAN" ? null : product.PcDimensiune.PerdeaEstePereche 
                                        
                                },
                                SelectedManopera = product.Produs.TipulProdusului == "perdea" || product.Produs.TipulProdusului == "draperie" ? new StandardManopereOnSet
                                {
                                    IdManopera = product.PcManopera!.IdManopera,
                                    NumeManopera = product.PcManopera.NumeManopera!,
                                    MetruTotalFolosit = product.PcManopera.MaterialFolosit,
                                    InaltimeMaxima = product.PcManopera.InaltimeMaxima,
                                    TipInel = product.PcManopera.InelPrindereLaManopera != null ? new TipIneleDto
                                    {
                                        IdInelPrindere = product.PcManopera.InelPrindereLaManopera.IdInel,
                                        NumeTipInel = product.PcManopera.InelPrindereLaManopera.CuloareInel,
                                        CaleRelativa = product.PcManopera.InelPrindereLaManopera.CaleRelativa,
                                        PresignedUrl = "empty"
                                    } : null,
                                    TipGalerie = new TipRejansaDto
                                    {
                                        IdRejansa = product.PcManopera.TipGalerieLaManopera.IdTipGalerie,
                                        NumeTipRejansa = product.PcManopera.TipGalerieLaManopera.NumeTipGalerie,
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
                                        NumeTipCusaturaColt = product.PcManopera.TipLinieLaManopera.NumeTipLinie,
                                        PretTipCusaturaColt = currency == "RON" ? product.PcManopera.TipLinieLaManopera.PretPeTipLinie
                                            :  product.PcManopera.TipLinieLaManopera.PretPeTipLinie / 5,
                                        CaleRelativa = product.PcManopera.TipLinieLaManopera.CaleRelativa,
                                        PresignedUrl = "empty"
                                    }
                                } : null,
                                LungimeCeruta = product.PcManopera != null ?
                                    product.PcManopera.NumeManopera == "STAN" ? product.PcManopera.MaterialFolosit.ToString() : "empty"
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
                var id = accountId;
                // retrieving his cart items
                cartItems = await cartRepository
                    .FindQueryable(item => item.IdCont == id && item.SessionId == sessionId)
                    .GroupBy(group => group.IdentificatorSet != "21" ? group.IdentificatorSet : group.IdProdusInCos.ToString())
                    .ToListAsync();
                var createNewAccount = new Conturi
                {
                    Nume = orderToBePlaced.NumePeComanda,
                    Prenume = orderToBePlaced.PrenumePeComanda,
                    NrTelefon = orderToBePlaced.NrTelefonPeComanda,
                    Username = null,
                    Email = orderToBePlaced.EmailPeComanda,
                    DataCreare = DateTime.UtcNow,
                    CodActivare = "GUEST",
                    Verificat = false,
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
                var findAccountToUpdate = await _unitOfWork.Repository<Conturi>()
                    .FindQueryable(acc => acc.IdCont == accountId)
                    .FirstAsync();
                
                findAccountToUpdate.Nume = orderToBePlaced.NumePeComanda;
                findAccountToUpdate.Prenume = orderToBePlaced.PrenumePeComanda;
                findAccountToUpdate.Email = orderToBePlaced.EmailPeComanda;
                findAccountToUpdate.NrTelefon = orderToBePlaced.NrTelefonPeComanda;

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
                
               
                
               
                voucherApplied = getVoucher ?? throw new DbUpdateException("No voucher found , maybe it was deleted");
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
                IdVoucher = voucherApplied?.IdVoucher
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
                    throw new InvalidOperationException("Payment unsuccesfull. ");
                }
            }
            
            await _unitOfWork.CommitTransactionAsync(placeOrderTransaction);
            /*
             * if payment succefull
             * 
             */
            /*
             *
             * PAYMENT API ( if succesfull ) => put products into the order with products table => confirmation page => unlock products
             * IF PAYMENT IS UNSUCCEFULL IN ANY WAY , UNLOCK PRODUCTS AND RETRY!
             * 
             */
            
            
            // opening new transaction
            var unlockProductsResponse = await UnlockProductsThatWereBought(wasAccountCreated ? null : accountId , wasAccountCreated ? Guid.Empty : sessionId);
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
                case DbUpdateException:
                    _logger.LogError("Voucher not found. Cancelling transaction and payment");
                    return new KeyValuePair<int, string>(-1, "Voucher not found");
                case InvalidOperationException:
                    _logger.LogError("Payment rejected! . Cancelling transaction and payment");
                    return new KeyValuePair<int, string>(-3, "Payment rejected");
                default:
                    return new KeyValuePair<int, string>(-2 , "General error occured");
            }
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

            // var getCartItemsThatWillBePurchased = await _unitOfWork.Repository<CosCumparaturi>()
            //     .FindQueryable(cart => cart.IdCont == accountId && cart.SessionId == sessionId)
            //     .GroupBy(group => group.IdentificatorSet != "21" ? group.IdentificatorSet : group.IdProdusInCos.ToString())
            //         .ToListAsync();

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
                        _logger.LogInformation("AICI1");
                        _logger.LogInformation($"id set : {item.IdSet}");
                        // we are in set
                        var findSet = await bundlesRepository.FindQueryable(s => s.IdSet == item.IdSet)
                            .FirstAsync();
                        _logger.LogInformation("AICI");
                        if (!findSet.IsLocked)
                        {
                            findSet.IsLocked = true;
                            lockedSets.Add(findSet);
                        }

                        var selectFromSet = items.Where(cartItem => cartItem.InaltimeAleasaPentruSet != null)
                            .Select(id => id.IdManopera)
                            .ToList();
                        if (selectFromSet.Count > 0)
                        {
                            _logger.LogInformation("ITEME GASITE IN SET CARE AU MANOPERA");
                        }
                       

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

                        if (findProduct.TipulProdusului is "perdea" or "draperie")
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

    private async Task<int> UnlockProductsThatWereBought(int? accountId, Guid sessionId)
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
                .FindQueryable(item => item.IdCont == accountId && item.SessionId == sessionId)
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

                        if (findProduct.TipulProdusului is "perdea" or "draperie")
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

            await _unitOfWork.CommitAsync();
            _logger.LogInformation($"Succefully unlocked items for client  ");
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
}