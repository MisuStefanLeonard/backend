using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using E_Commerce_BackEnd.Models.DTO;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ManopereDto.ManoperaForSet;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductOptionsDto;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.ProductPage.OptionsForCurtain;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ShoppingCartDtos;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.VouchereDtos;
using E_Commerce_BackEnd.Models.Enums;
using E_Commerce_BackEnd.Models.ProductRelatedModels;
using E_Commerce_BackEnd.Models.ProductVouchersModels;
using E_Commerce_BackEnd.Models.UserRelatedModels;
using E_Commerce_BackEnd.Services.Helpers.AWS_Secret.AWSBucket_CRUD;
using E_Commerce_BackEnd.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.IdentityModel.Tokens;
using Sqids;

namespace E_Commerce_BackEnd.Services.uShoppingCartService;

public class CartService : ICartService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CartService> _logger;
    private readonly SqidsEncoder<int> _sqidsEncoder;
    private readonly IBucketAcces _bucketAcces;
    private readonly IMapper _mapper;


    public CartService(IUnitOfWork unitOfWork, ILogger<CartService> logger, SqidsEncoder<int> sqidsEncoder, IBucketAcces bucketAcces, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _sqidsEncoder = sqidsEncoder;
        _bucketAcces = bucketAcces;
        _mapper = mapper;
    }


    private static string GenerateSetIdentifier(SetOnCartDto setOnCartItem)
    {
        var serializedSet = setOnCartItem.ProductsInCart
            .OrderBy(p => p.IdProdus) // Ensure consistent ordering
            .ThenBy(p => p.IdCuloare)
            .ThenBy(p => p.IdDimensiune)
            .ThenBy(p => p.IdManopera)
            .ThenBy(p => p.PrefferedHeight)
            .Select(p => $"{p.IdProdus}-{p.IdCuloare}-{p.IdDimensiune}-{p.IdManopera}-{p.PrefferedHeight}")
            .Aggregate((current, next) => $"{current}|{next}"); // Combine into a single string
 
        using var sha256 = SHA256.Create();
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(serializedSet));
        return Convert.ToBase64String(hashBytes); // Return hash as a string
    }


    public async Task<KeyValuePair<int, string>> AddOrUpdateCart(Guid sessionId,int? idCont,SetOnCartDto? setOnCartItem , ProductOnCartDto? productOnCart)
    {
        if (idCont != null && sessionId != Guid.Empty)
        {
            sessionId = Guid.Empty;
        }
       
        IDbContextTransaction? addOrUpdateTransaction = null;
        try
        {
            addOrUpdateTransaction = await _unitOfWork.BeginTransactionAsync();
            var cartRepository = _unitOfWork.Repository<CosCumparaturi>();

            if (setOnCartItem != null)
            {
                var uniqueSetIdentifier = GenerateSetIdentifier(setOnCartItem);
                if (setOnCartItem.CurrentCurrency == "EUR")
                {
                    setOnCartItem.PretCurent *= 5;
                }
                if (_sqidsEncoder.Decode(setOnCartItem.EncodedIdSet) is [var decodedId]
                    && setOnCartItem.EncodedIdSet == _sqidsEncoder.Encode(decodedId))
                  
                {
                    var existingSet = await cartRepository
                        .FindQueryable(c => c.IdCont == idCont && c.IdentificatorSet == uniqueSetIdentifier
                                                               && c.SessionId == sessionId)
                        .ToListAsync();
                    
                    if (existingSet.Count > 0)
                    {
                        foreach (var product in existingSet)
                        {
                            product.CantitateProdus++;
                        }
                        await cartRepository.UpdateRangeAsync(existingSet);
                        await _unitOfWork.CommitTransactionAsync(addOrUpdateTransaction);
                        return new KeyValuePair<int, string>(2, "Succesfully incremented (set) item quantity");
                    }
                    
                    
                    var newSetItems = setOnCartItem.ProductsInCart.Select(p => new CosCumparaturi
                    {
                        CantitateProdus = 1,
                        PretProdus = setOnCartItem.PretCurent,
                        IdCont = idCont,
                        IdProdus = p.IdProdus,
                        IdCuloare = p.IdCuloare,
                        IdDimensiune = p.IdDimensiune,
                        IdManopera = p.IdManopera,
                        InaltimeAleasaPentruSet = p.PrefferedHeight,
                        IdSet = decodedId,
                        IdentificatorSet = uniqueSetIdentifier,
                        SessionId = sessionId,
                        ExpiresAt = DateTime.UtcNow.AddDays(3)
                    }).ToList();

                    await cartRepository.AddRangeAsync(newSetItems);
                    await _unitOfWork.CommitTransactionAsync(addOrUpdateTransaction);
                    return new KeyValuePair<int, string>(1, "Succesfully added (set) item to cart");
                }
                
               
                return new KeyValuePair<int, string>(0, "Set id could not be decoded");
                
            }

            if (productOnCart != null)
            {
                if (productOnCart.CurrentCurrency == "EUR")
                {
                    productOnCart.PretCurent *= 5;
                    productOnCart.PretCurentTipGalerie *= 5;
                    productOnCart.PretCurentTipLinie *= 5;
                }
                Manopere? findManoperaInDb = null;
                Dimensiuni? findDimensionInDb = null;
                var manopereRepository = _unitOfWork.Repository<Manopere>();
                if (productOnCart.IdRejansa != -11 
                    && productOnCart.IdInelPrindere != -11 
                    && productOnCart.IdTipLinie != -11)
                {
                    findManoperaInDb = await manopereRepository
                        .FindQueryable(m => m.IdTipGalerie == productOnCart.IdRejansa
                                            && m.IdInelPrindere == productOnCart.IdInelPrindere
                                            && m.IdTipLinie == productOnCart.IdTipLinie
                                            && m.PretCurentTipGalerie == productOnCart.PretCurentTipGalerie
                                            && m.PretCurentTipLinie == productOnCart.PretCurentTipLinie
                                            && m.MaterialFolosit == productOnCart.MaterialFolosit)
                        .FirstOrDefaultAsync();
                  
                    if (findManoperaInDb == null)
                    {
                        var createNewManopera = new Manopere
                        {
                            NumeManopera = null,
                            IdInelPrindere = productOnCart.IdInelPrindere,
                            IdTipLinie = productOnCart.IdTipLinie,
                            IdTipGalerie = productOnCart.IdRejansa,
                            PretCurentTipLinie = productOnCart.PretCurentTipLinie,
                            PretCurentTipGalerie = productOnCart.PretCurentTipGalerie,
                            MaterialFolosit = productOnCart.MaterialFolosit,
                            TipManopera = TipManopere.Aleasa,
                            InaltimeMaxima = null
                        };

                        await manopereRepository.AddAsync(createNewManopera);
                        await _unitOfWork.CommitAsync();
                        findManoperaInDb = createNewManopera;
                    }


                    var galleryWidth = productOnCart.LungimeSina!;
                    var galleryToBottomHeight = productOnCart.Inaltime!;
                    var pair = productOnCart.Pereche;

                    var dimensionsRepository = _unitOfWork.Repository<Dimensiuni>();

                    var isDimensionInDb = await dimensionsRepository
                        .FindQueryable(d => d.Lungime == galleryWidth
                                            && d.Latime == galleryToBottomHeight
                                            && d.PerdeaEstePereche == pair)
                        .FirstOrDefaultAsync();

                    if (isDimensionInDb == null)
                    {
                        var newDimension = new Dimensiuni
                        {
                            Lungime = galleryWidth,
                            Latime = galleryToBottomHeight,
                            PerdeaEstePereche = pair,
                            RecomandarePat = null
                        };

                        await dimensionsRepository.AddAsync(newDimension);
                        await _unitOfWork.CommitAsync();
                        isDimensionInDb = newDimension;
                    }

                    findDimensionInDb = isDimensionInDb;
                }
                else
                {
                    if (productOnCart.LungimeSina == null && productOnCart.Inaltime == null && productOnCart.Pereche == null)
                    {
                        var totalMaterial = productOnCart.MaterialFolosit;
                        // alegem manopera standard (conventie STAN la inel (la nume) , rejansa si tip linie)
                        var ringsRepository = _unitOfWork.Repository<InelePrindere>();
                        var rejansaRepository = _unitOfWork.Repository<TipuriGalerie>();
                        var liningTypeRepository = _unitOfWork.Repository<TipuriLinie>();

                        var findStandardRing = await ringsRepository
                            .FindQueryable(ring => ring.CuloareInel == "STAN")
                            .FirstOrDefaultAsync();
                       
                        if (findStandardRing == null)
                        {
                            var standardInelPrindere = new InelePrindere
                            {
                                CuloareInel = "STAN",
                                CaleRelativa = null,
                                IsDeleted = false
                            };
                            await ringsRepository.AddAsync(standardInelPrindere);
                            await _unitOfWork.CommitAsync();

                            findStandardRing = standardInelPrindere;
                        }
                        
                        var findStandardRejansa = await rejansaRepository
                            .FindQueryable(rejansa => rejansa.NumeTipGalerie == "STAN")
                            .FirstOrDefaultAsync();

                        if (findStandardRejansa == null)
                        {
                            var standardRejansa = new TipuriGalerie
                            {
                                NumeTipGalerie = "STAN",
                                IncretireRejansa = 0,
                                PretTipGalerie = 0,
                                IsDeleted = false,
                                SePrindeCuInele = false,
                                CaleRelativa = null
                            };
                            await rejansaRepository.AddAsync(standardRejansa);
                            await _unitOfWork.CommitAsync();

                            findStandardRejansa = standardRejansa;
                        }
                        
                        var findStandardLiningType = await liningTypeRepository
                            .FindQueryable(line => line.NumeTipLinie == "STAN")
                            .FirstOrDefaultAsync();

                        if (findStandardLiningType == null)
                        {
                            var standardLiningType= new TipuriLinie
                            {
                                NumeTipLinie = "STAN",
                                PretPeTipLinie = 0,
                                CaleRelativa = null,
                                IsDeleted = false
                            };
                            await liningTypeRepository.AddAsync(standardLiningType);
                            await _unitOfWork.CommitAsync();

                            findStandardLiningType = standardLiningType;
                        }

                        var manoperaWithOnlyMaterial = new Manopere
                        {
                            NumeManopera = "STAN",
                            IdInelPrindere = findStandardRing.IdInel,
                            IdTipLinie = findStandardLiningType.IdTipLinie,
                            IdTipGalerie = findStandardRejansa.IdTipGalerie,
                            PretCurentTipLinie = 0,
                            PretCurentTipGalerie = 0,
                            MaterialFolosit = totalMaterial,
                            TipManopera = TipManopere.Aleasa,
                            InaltimeMaxima = null
                        };

                        await manopereRepository.AddAsync(manoperaWithOnlyMaterial);
                        await _unitOfWork.CommitAsync();

                        findManoperaInDb = manoperaWithOnlyMaterial;


                    }
                }
                
               
                var isProductAlreadyInCart = await cartRepository
                    .FindQueryable(c => c.IdCont == idCont
                                           && c.SessionId == sessionId
                                           && c.IdProdus == productOnCart.IdProdus
                                           && c.IdCuloare == productOnCart.IdCuloare
                                           && c.IdDimensiune == (findDimensionInDb != null ? findDimensionInDb.IdDimensiune : productOnCart.IdDimensiune) 
                                           && c.IdManopera == (findManoperaInDb != null ? findManoperaInDb.IdManopera : null)
                                           && c.IdSet == null)
                    .FirstOrDefaultAsync();

                if (isProductAlreadyInCart == null)
                {
                    var newItemInCart = new CosCumparaturi
                    {
                        CantitateProdus = 1,
                        PretProdus = productOnCart.PretCurent,
                        IdCont = idCont,
                        IdProdus = productOnCart.IdProdus,
                        IdCuloare = productOnCart.IdCuloare,
                        IdDimensiune = findDimensionInDb?.IdDimensiune ?? productOnCart.IdDimensiune ,
                        IdSet = null,
                        IdManopera = findManoperaInDb?.IdManopera,
                        IdentificatorSet = "21",
                        SessionId = sessionId,
                        ExpiresAt = DateTime.UtcNow.AddDays(3)
                    };
                    
                    await cartRepository.AddAsync(newItemInCart);
                    await _unitOfWork.CommitTransactionAsync(addOrUpdateTransaction);
                    return new KeyValuePair<int, string>(1, "Succesfully added item to cart");
                }

                isProductAlreadyInCart.CantitateProdus++;
                await cartRepository.UpdateAsync(isProductAlreadyInCart);
                await _unitOfWork.CommitTransactionAsync(addOrUpdateTransaction);
                return new KeyValuePair<int, string>(2, "Succesfully incremented item quantity");

            }

            return new KeyValuePair<int, string>(-3, "REACHED HERE , NOT GOOD");

        }
        catch (Exception e)
        {
            if (addOrUpdateTransaction != null)
            {
                await _unitOfWork.RollBackTransactionAsync(addOrUpdateTransaction);
            }
            
            _logger.LogError($"EXCEPTION: {e.GetType()}");
            _logger.LogError($"ERROR : {e.Message}" );
            return new KeyValuePair<int, string>(-1, "ERROR OCCURED. SEE LOGS FOR INFORMATIONS.");
        }
       
    }
    
    public async Task<int> DeleteFromCart(UpdateQuantityInfo productInfo)
    { 
        IDbContextTransaction? deleteTransaction = null;
        try
        {
            deleteTransaction = await _unitOfWork.BeginTransactionAsync();
            var cartRepository = _unitOfWork.Repository<CosCumparaturi>();
            if (productInfo.IdentificatorSet != "21")
            {
                
                var setToDelete = await cartRepository
                    .FindQueryable(cart => cart.IdentificatorSet == productInfo.IdentificatorSet
                                           && cart.IdCont == productInfo.IdCont &&
                                           cart.SessionId == productInfo.SessionId)
                    .ToListAsync();

                if (setToDelete.IsNullOrEmpty())
                {
                    return -2; // Bundles does not exist anymore.
                }
                
                await cartRepository.DeleteRangeAsync(setToDelete);
            }
            else
            {
                var product = await cartRepository
                    .FindQueryable(cart =>
                                              cart.IdentificatorSet == productInfo.IdentificatorSet
                                           && cart.IdCont == productInfo.IdCont
                                           && cart.SessionId == productInfo.SessionId
                                           && cart.IdCuloare == productInfo.IdCuloare
                                           && cart.IdDimensiune == productInfo.IdDimensiune 
                                              && cart.IdProdus == productInfo.IdProdus
                                           && cart.IdManopera == productInfo.IdManopera
                        && cart.IdSet == productInfo.IdSet)
                    .FirstOrDefaultAsync();

                if (product == null)
                {
                    return -2; // e.g product does not exist anymore
                }

                
                await cartRepository.DeleteAsync(product);
            }

            await _unitOfWork.CommitTransactionAsync(deleteTransaction);
            return 1; // SUccesfully deleted the product
        }
        catch (Exception e)
        {
            if (deleteTransaction != null)
            {
                await _unitOfWork.RollBackTransactionAsync(deleteTransaction);
            }
            _logger.LogError("ERROR: {0}" , e.GetType());
            _logger.LogError("MESSAGE: {0}" , e.Message);
            _logger.LogError("STACKTRACE: {0}" , e.StackTrace);
            return -1;
        }
        
        
      
    }

    private async Task<int> UpdatePrices(IList<GroupedCartItems> pricesChanged,int? idCont, Guid sessionId)
    {
        if (pricesChanged.IsNullOrEmpty())
        {
            return 0;
        }
        var cartRepository = _unitOfWork.Repository<CosCumparaturi>();
        var itemsToUpdate = new List<CosCumparaturi>();
        foreach (var item in pricesChanged)
        {
            var isSet = !int.TryParse(item.Key, out _);
           
            var controlFlag = false;
            foreach (var product in item.CartItems)
            {
                if (isSet)
                {
                  
                    var findSetToUpdate = await cartRepository
                        .FindQueryable(info => info.IdCont == idCont
                                               && info.SessionId == sessionId 
                                               && info.IdentificatorSet == item.Key)
                        .ToListAsync();

                    foreach (var itemInSet in findSetToUpdate)
                    {
                        itemInSet.PretProdus = product.PretReal;
                        
                    }
                    itemsToUpdate.AddRange(findSetToUpdate);
                    isSet = false;
                    controlFlag = true;
                    
                }
                else if(!isSet && !controlFlag)
                {
                   
                    var findProductToUpdate = await cartRepository
                        .FindQueryable(info => info.IdCont == idCont
                                               && info.SessionId == sessionId 
                                               && info.IdentificatorSet == "21"
                                               && info.IdCuloare == product.CuloareSelectata.IdCuloare
                                               && info.IdDimensiune == (product.DimensiuneSelectata == null ? null : product.DimensiuneSelectata.IdDimensiune) 
                                               && info.IdManopera == (product.SelectedManopera == null ? null : product.SelectedManopera.IdManopera) 
                                               && info.IdSet == null)
                        .FirstAsync();
                   
                    findProductToUpdate.PretProdus = product.PretReal;
                    
                    itemsToUpdate.Add(findProductToUpdate);
                }
            }
        }

        await cartRepository.UpdateRangeAsync(itemsToUpdate);
        return 1;
    }
    
    private async Task<int> UpdateExpirationDateIfCheckoutSessionStarted(IList<GroupedCartItems> itemsAboutToExpire,int? idCont, Guid sessionId , DateTime timeToSet)
    {
        if (itemsAboutToExpire.IsNullOrEmpty())
        {
            return 0;
        }
        var cartRepository = _unitOfWork.Repository<CosCumparaturi>();
        var itemsToUpdate = new List<CosCumparaturi>();
        foreach (var item in itemsAboutToExpire)
        {
            var isSet = !int.TryParse(item.Key, out _);
           
            var controlFlag = false;
            foreach (var product in item.CartItems)
            {
                if (isSet)
                {
                  
                    var findSetToUpdate = await cartRepository
                        .FindQueryable(info => info.IdCont == idCont
                                               && info.SessionId == sessionId 
                                               && info.IdentificatorSet == item.Key)
                        .ToListAsync();

                    foreach (var itemInSet in findSetToUpdate)
                    {
                        itemInSet.ExpiresAt = timeToSet;
                        
                    }
                    itemsToUpdate.AddRange(findSetToUpdate);
                    isSet = false;
                    controlFlag = true;
                    
                }
                else if(!isSet && !controlFlag)
                {
                   
                    var findProductToUpdate = await cartRepository
                        .FindQueryable(info => info.IdCont == idCont
                                               && info.SessionId == sessionId 
                                               && info.IdentificatorSet == "21"
                                               && info.IdCuloare == product.CuloareSelectata.IdCuloare
                                               && info.IdDimensiune == (product.DimensiuneSelectata == null ? null : product.DimensiuneSelectata.IdDimensiune) 
                                               && info.IdManopera == (product.SelectedManopera == null ? null : product.SelectedManopera.IdManopera) 
                                               && info.IdSet == null)
                        .FirstAsync();
                   
                    findProductToUpdate.ExpiresAt = timeToSet;
                    
                    itemsToUpdate.Add(findProductToUpdate);
                }
            }
        }

        await cartRepository.UpdateRangeAsync(itemsToUpdate);
        return 1;
    }

    private async Task<CartInfo> FetchCartProducts(int? idCont, Guid sessionId, string currency)
    {
        var cartRepository = _unitOfWork.Repository<CosCumparaturi>();
        var cartItems = await cartRepository
                .FindQueryable(item => item.IdCont == idCont && item.SessionId == sessionId)
                .GroupBy(item => item.IdentificatorSet != "21" ? item.IdentificatorSet : item.IdProdusInCos.ToString())
                .Select(info => new GroupedCartItems
                {
                    Key = info.Key,
                    CartItems = info.Select(group => new CartItems
                    {
                        IdProdus = group.Produs!.IdProdus,
                        IdSet = group.Set!.IdSet,
                        NumeSet = group.Set!.NumeSet,
                        CodProdus = group.Produs!.CodProdus,
                        NumeProdus =  group.Produs!.NumeProdus!,
                        TipProdus = group.Produs!.TipulProdusului,
                        CuloareSelectata = new CuloriDto
                        {
                            IdCuloare = group.Culoare.IdCuloare,
                            NumeCuloareDto = group.Culoare.NumeCuloare,
                            CodCuloareDto = group.Culoare.CodCuloare.CodCuloare!,
                            JustAdded = false,
                            ImaginiProdusDto = group.Produs.PProduseCuCulori!
                                .FirstOrDefault(pc => pc.ImagProduseCuCulori!.Count > 0)!
                                .ImagProduseCuCulori!.Select(imag => new ImagesDto
                                {
                                    CaleImagineDto = imag.CaleImagine!,
                                    FisierInBucketDto = imag.FisierInBucket,
                                    PresignedUrl = "empty",
                                    JustAdded = false,
                                    IdProdusCuCuloareDto = 0
                                }).Take(1)
                                .ToList()
                        },
                        DimensiuneSelectata = new DimensiuniDto
                        {
                            IdDimensiune = group.Dimensiune!.IdDimensiune,
                            LungimeDto = group.Manopera!.NumeManopera != "STAN" ?  group.Dimensiune.Lungime : ((int)(group.Manopera.MaterialFolosit * 100)).ToString(),
                            LatimeDto = group.Dimensiune.Lungime,
                            RecomandarePat = group.Dimensiune.RecomandarePat,
                            PretDto = 0,
                            PretRedusDto = 0,
                            JustAdded = false,
                            PerdeaEstePerecheDto = group.Manopera!.NumeManopera == "STAN" ? null : group.Dimensiune.PerdeaEstePereche 
                                
                        },
                        SelectedManopera = group.Produs.TipulProdusului == "perdea" || group.Produs.TipulProdusului == "draperie" ? new StandardManopereOnSet
                        {
                            IdManopera = group.Manopera!.IdManopera,
                            NumeManopera = group.Manopera.NumeManopera!,
                            MetruTotalFolosit = group.Manopera.MaterialFolosit,
                            InaltimeMaxima = group.Manopera.InaltimeMaxima,
                            TipInel = group.Manopera.InelPrindereLaManopera != null ? new TipIneleDto
                            {
                                IdInelPrindere = group.Manopera.InelPrindereLaManopera.IdInel,
                                NumeTipInel = group.Manopera.InelPrindereLaManopera.CuloareInel,
                                CaleRelativa = group.Manopera.InelPrindereLaManopera.CaleRelativa,
                                PresignedUrl = "empty"
                            } : null,
                            TipGalerie = new TipRejansaDto
                            {
                                IdRejansa = group.Manopera.TipGalerieLaManopera.IdTipGalerie,
                                NumeTipRejansa = group.Manopera.TipGalerieLaManopera.NumeTipGalerie,
                                PretTipRejansa = currency == "RON" ? group.Manopera.TipGalerieLaManopera.PretTipGalerie
                                    :  group.Manopera.TipGalerieLaManopera.PretTipGalerie / 5,
                                IncretireRejansa = group.Manopera.TipGalerieLaManopera.IncretireRejansa,
                                CaleRelativa = group.Manopera.TipGalerieLaManopera.CaleRelativa,
                                PresignedUrl = "empty",
                                SePrindeCuInele = group.Manopera.TipGalerieLaManopera.SePrindeCuInele
                            },
                            TipLinie = new TipLinieDto
                            {
                                IdTipLinie = group.Manopera.TipLinieLaManopera.IdTipLinie,
                                NumeTipCusaturaColt = group.Manopera.TipLinieLaManopera.NumeTipLinie,
                                PretTipCusaturaColt = currency == "RON" ? group.Manopera.TipLinieLaManopera.PretPeTipLinie
                                    :  group.Manopera.TipLinieLaManopera.PretPeTipLinie / 5,
                                CaleRelativa = group.Manopera.TipLinieLaManopera.CaleRelativa,
                                PresignedUrl = "empty"
                            }
                        } : null,
                        LungimeCeruta = group.Manopera != null ?
                            group.Manopera.NumeManopera == "STAN" ? group.Manopera.MaterialFolosit.ToString() : "empty"
                        : "not_perdea",
                        InaltimeCeruta = group.IdSet != null ? group.InaltimeAleasaPentruSet : "not_set",
                        PretCurent = currency == "RON" ? group.PretProdus : group.PretProdus / 5 ,
                        PretReal = group.IdSet != null
                             ? group.Set.PretRedusSet > 0 
                                 ? currency == "RON" 
                                     ? group.Set.PretRedusSet : group.Set.PretRedusSet / 5 
                                 : currency == "RON" 
                                     ? group.Set.PretSet : group.Set.PretSet / 5 
                             : group.Produs.TipulProdusului == "draperie" || group.Produs.TipulProdusului ==  "perdea" 
                                ? group.IdDimensiune == null
                                    ?  group.Produs.PretDeBazaRedus > 0 
                                        ? currency == "RON" 
                                            ?  group.Manopera!.MaterialFolosit *  group.Produs.PretDeBazaRedus
                                            :  group.Manopera!.MaterialFolosit * group.Produs.PretDeBazaRedus / 5 
                                        : currency == "RON" 
                                            ?  group.Manopera!.MaterialFolosit *  group.Produs.PretDeBaza 
                                            :  group.Manopera!.MaterialFolosit * group.Produs.PretDeBaza / 5 
                                    :  group.Manopera!.MaterialFolosit * 
                                       ((group.Produs.PretDeBazaRedus > 0
                                              ? (currency == "RON" 
                                                  ? group.Produs.PretDeBazaRedus 
                                                  : group.Produs.PretDeBazaRedus / 5)
                                              : (currency == "RON" 
                                                  ? group.Produs.PretDeBaza 
                                                  : group.Produs.PretDeBaza / 5)
                                          )
                                          + (currency == "RON" ?
                                              group.Manopera.TipGalerieLaManopera.PretTipGalerie  
                                              :  group.Manopera.TipGalerieLaManopera.PretTipGalerie / 5  )
                                          + (currency == "RON" ?
                                              group.Manopera.TipLinieLaManopera.PretPeTipLinie 
                                              :  group.Manopera.TipLinieLaManopera.PretPeTipLinie  / 5 ))
                            : group.Produs!.PProduseCuDimensiuni!.Count > 0
                                    ? group.Produs!.PProduseCuDimensiuni!.FirstOrDefault(p => group.IdDimensiune == p.IdDimensiune) != null 
                                        ? group.Produs!.PProduseCuDimensiuni!.FirstOrDefault(p => group.IdDimensiune == p.IdDimensiune)!.PretRedus > 0
                                            ? group.Produs!.PProduseCuDimensiuni!.FirstOrDefault(p => group.IdDimensiune == p.IdDimensiune)!.PretRedus
                                            : group.Produs!.PProduseCuDimensiuni!.FirstOrDefault(p => group.IdDimensiune == p.IdDimensiune)!.Pret
                                        : 0 // reaches here , means dimension not available anymore!
                                    : group.Produs!.PretDeBazaRedus > 0 ? group.Produs!.PretDeBazaRedus : group.Produs!.PretDeBaza,
                        Cantitate = group.CantitateProdus,
                        IdentificatorSet = info.Key  
                    }).ToList()
                })
                .ToListAsync();
        
        decimal cartTotal = 0;
        var totalProducts = 0;
        
        foreach (var item in cartItems)
        {
            var isSet = !int.TryParse(item.Key, out _);
            var exit = false; // true if end or false if not
            foreach (var cartItem in item.CartItems)
            {
                switch (isSet)
                {
                    // if we found the product!
                    case false:
                        cartTotal += item.CartItems[0].Cantitate * item.CartItems[0].PretCurent;
                        totalProducts += item.CartItems[0].Cantitate;
                        break;
                    // else we found a set , and we only count once!
                    case true when !exit:
                        cartTotal += item.CartItems[0].Cantitate * item.CartItems[0].PretCurent;
                        totalProducts += item.CartItems[0].Cantitate;
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

        return new CartInfo
        {
            Items = cartItems,
            PretTotal = cartTotal,
            TotalProduse = totalProducts
        };

    }

    public async Task<Tuple<int,CartInfo?>> CheckCartAtCheckout(int? idCont, Guid sessionId ,DateTime dateTimeSinceCookieWasAdded ,DateTime timeToUpdate,string currency = "RON")
    {
        
        IDbContextTransaction? updateCartTransaction = null;
        try
        {
            _logger.LogInformation($"id cont : {idCont}");
            updateCartTransaction = await _unitOfWork.BeginTransactionAsync();
            var updatedItemsForCheckout = await FetchCartProducts(idCont, sessionId, currency);
            
            if (updatedItemsForCheckout.Items.IsNullOrEmpty())
            {
                return new Tuple<int,CartInfo?>(-2,null); // EMPTY CART / GO BACK , DO NOT PROCEED // INVALID CHECKOUT
            }
            _logger.LogInformation($"Total hours passed : {(DateTime.UtcNow - dateTimeSinceCookieWasAdded).TotalHours}");
            if ((DateTime.UtcNow - dateTimeSinceCookieWasAdded).TotalHours >= 60)
            {
                var response = await UpdateExpirationDateIfCheckoutSessionStarted(updatedItemsForCheckout.Items , idCont , sessionId, timeToUpdate);
                if (response == 1)
                {
                    _logger.LogInformation("Cookie was about to expire. Renewed cookie session and modified expiry date");
                }
                updatedItemsForCheckout =  await FetchCartProducts(idCont, sessionId, currency);
            }
            var productsPriceChanged = new List<GroupedCartItems>();
            
            foreach (var item in updatedItemsForCheckout.Items)
            {
                productsPriceChanged.AddRange(from cartItem in item.CartItems 
                    where cartItem.PretCurent != cartItem.PretReal
                    select item);
            }

            var updatePrices = await UpdatePrices(productsPriceChanged, idCont, sessionId);
            switch (updatePrices)
            {
                case 1:
                    _logger.LogInformation("Succefully updated prices");
                    updatedItemsForCheckout = await FetchCartProducts(idCont, sessionId, currency);
                    break;
                case 0:
                    _logger.LogInformation("Nothing to updated");
                    break;
                default:
                    _logger.LogInformation("Error. Exception thrown in UpdatePrices function");
                    break;
            };

            updatedItemsForCheckout.ModifiedCartItems = productsPriceChanged;

            var addressesRepository = _unitOfWork.Repository<Adrese>();
            
            if (idCont != null)
            {
                var clientsAddresses = await addressesRepository
                    .FindQueryable(a => a.IdCont == idCont )
                    .GroupBy(group => group.TipAdresa)
                    .Select(address => new
                    {
                        Type = address.Key,
                        Addresses = address.Select(item => new AdreseDto
                        {
                            AliasDto = item.Alias,
                            TipAdresaDto = address.Key,
                            BlocDto = item.Bloc,
                            NrBlocDto = item.NrBloc,
                            StradaDto = item.Strada,
                            NrStradaDto = item.NrStrada,
                            OrasDto = item.Locatie.Oras!,
                            JudetDto = item.Locatie.Judet!,
                            CodPostalDto = item.Locatie.CodPostal!,
                            IsDeletedDto = false,
                            CifDto = item.DetaliuFactura != null ? item.DetaliuFactura.Cif : null,
                            NumeFirmaDto = item.DetaliuFactura != null ? item.DetaliuFactura.NumeFirma : null
                        })
                    }).ToListAsync();
                updatedItemsForCheckout.ClientsDeliveryAddresses = clientsAddresses
                    .Where(group => group.Type == TipAdrese.Livrare)
                    .SelectMany(group => group.Addresses)
                    .ToList();
                updatedItemsForCheckout.ClientsBillingAddresses = clientsAddresses
                    .Where(group => group.Type == TipAdrese.Facturare)
                    .SelectMany(group => group.Addresses)
                    .ToList();
                updatedItemsForCheckout.IsLoggedIn = true;
                var userDetails = await _unitOfWork.Repository<Conturi>()
                    .FindQueryable(acc => acc.IdCont == idCont)
                    .Select(details => new UserPersonalInfo
                    {
                        Nume = details.Nume,
                        Prenume = details.Prenume,
                        NrTelefon = details.NrTelefon,
                        Email = details.Email
                    }).FirstOrDefaultAsync();
                updatedItemsForCheckout.UserOrderDetails = userDetails;
            }
            
            
            await _unitOfWork.CommitTransactionAsync(updateCartTransaction);
            return new Tuple<int, CartInfo?>(1,updatedItemsForCheckout);

        }
        catch (Exception e)
        {
            if (updateCartTransaction != null)
            {
                await _unitOfWork.RollBackTransactionAsync(updateCartTransaction);
                _logger.LogInformation("Rolling back transaction . Error thrown");
                _logger.LogError("Error type {0}" , e.GetType());
                _logger.LogError("STACK TRACE {0}" , e.StackTrace);
              
            }
            return new Tuple<int,CartInfo?>(-1,null);
        }
    }

    

    public async Task<CartInfo> GetCartItems(int? idCont , Guid sessionId , string currency = "RON")
    {
        
        if (sessionId != Guid.Empty && idCont != null)
        {
            sessionId = Guid.Empty;
        }

        return await FetchCartProducts(idCont, sessionId, currency);
       
        
    }

    public async Task<int> ModifyQuantity(UpdateQuantityInfo productInfo)
    {
        IDbContextTransaction? updateTransaction = null;
        try
        {
            _logger.LogInformation($" id prod : {productInfo.IdProdus} id set : {productInfo.IdSet} identifactor set : {productInfo.IdentificatorSet}");
            _logger.LogInformation($"id cont : {productInfo.IdCont} , seesion id : {productInfo.SessionId}");
            var itemsToDelete = new List<CosCumparaturi>();
            updateTransaction = await _unitOfWork.BeginTransactionAsync();
            var cartRepository = _unitOfWork.Repository<CosCumparaturi>();
            if (productInfo.IdentificatorSet != "21")
            {
                
                var setToModifiy = await cartRepository
                    .FindQueryable(cart => cart.IdentificatorSet == productInfo.IdentificatorSet
                                           && cart.IdCont == productInfo.IdCont &&
                                           cart.SessionId == productInfo.SessionId)
                    .ToListAsync();

                if (setToModifiy.IsNullOrEmpty())
                {
                    return -2; // Bundles does not exist anymore.
                }
                
                foreach (var items in setToModifiy)
                {
                    if (productInfo.IsDecrementing)
                    {
                        if (items.CantitateProdus > 1)
                        {
                            items.CantitateProdus--;
                        }
                        else
                        {
                            itemsToDelete.Add(items);
                        }
                    }
                    else
                    {
                        items.CantitateProdus++;
                    }
                  
                }

                if (itemsToDelete.IsNullOrEmpty())
                {
                    await cartRepository.UpdateRangeAsync(setToModifiy);
                }
                else
                {
                    await cartRepository.DeleteRangeAsync(itemsToDelete);
                }
               
            }
            else
            {
                var product = await cartRepository
                    .FindQueryable(cart =>
                                              cart.IdentificatorSet == productInfo.IdentificatorSet
                                           && cart.IdCont == productInfo.IdCont
                                           && cart.SessionId == productInfo.SessionId
                                           && cart.IdCuloare == productInfo.IdCuloare
                                           && cart.IdDimensiune == productInfo.IdDimensiune 
                                              && cart.IdProdus == productInfo.IdProdus
                                           && cart.IdManopera == productInfo.IdManopera
                        && cart.IdSet == productInfo.IdSet)
                    .FirstOrDefaultAsync();

                if (product == null)
                {
                    return -2; // e.g product does not exist anymore
                }

                if (productInfo.IsDecrementing)
                {
                    if (product.CantitateProdus > 1)
                    {
                        product.CantitateProdus--;
                    }
                    else
                    {
                        itemsToDelete.Add(product);
                    }
                }
                else
                {
                    product.CantitateProdus++;
                }
                
                if (itemsToDelete.IsNullOrEmpty())
                {
                    await cartRepository.UpdateAsync(product);
                }
                else
                {
                    await cartRepository.DeleteAsync(product);
                }
                
            }

            await _unitOfWork.CommitTransactionAsync(updateTransaction);
            return 1; // SUccesfully quantity increment or decrement
        }
        catch (Exception e)
        {
            if (updateTransaction != null)
            {
                await _unitOfWork.RollBackTransactionAsync(updateTransaction);
            }
            _logger.LogError("ERROR: {0}" , e.GetType());
            _logger.LogError("MESSAGE: {0}" , e.Message);
            _logger.LogError("STACKTRACE: {0}" , e.StackTrace);
            return -1;
        }
    }

    public async Task<KeyValuePair<int, VouchereDto?>> GetVoucherInfo(int? idCont ,CheckVoucher voucherData)
    {
        var vouchersRepository = _unitOfWork.Repository<Vouchere>();
        var accountRepository = _unitOfWork.Repository<Conturi>();
        var getAvailableVoucher = await vouchersRepository
            .FindQueryable(v => v.CodVoucher == voucherData.VoucherCode.ToUpper())
            .FirstOrDefaultAsync();

        if (getAvailableVoucher == null || getAvailableVoucher.DataExpirare <= DateTime.UtcNow)
        {
            return new KeyValuePair<int, VouchereDto?>(0 , null);
        }

        Conturi? findAccountWithOrders;

        if (idCont != null)
        {
             findAccountWithOrders = await accountRepository
                .FindQueryable(p => p.IdCont == idCont)
                .Include(p => p.AdreseConturi)!
                .ThenInclude(order => order.AdreseLivrarePeComanda)
                .FirstOrDefaultAsync();
        }
        else
        {
             findAccountWithOrders = await accountRepository
                .FindQueryable(p => p.Email == voucherData.Email)
                .Include(p => p.AdreseConturi)!
                .ThenInclude(order => order.AdreseLivrarePeComanda)
                .FirstOrDefaultAsync();
             
        }
        
        if (findAccountWithOrders == null)
        {
            return new KeyValuePair<int, VouchereDto?>(-1, null); // No account found
        }

        var hasOrders = findAccountWithOrders.AdreseConturi!
            .Any(p => p.AdreseLivrarePeComanda != null && p.AdreseLivrarePeComanda.Any(v => v.IdVoucher != null));

        return hasOrders ? new KeyValuePair<int, VouchereDto?>(-2, null) : // Voucher already used
            new KeyValuePair<int, VouchereDto?>(1,_mapper.Map<VouchereDto>(getAvailableVoucher));
    }
    
}