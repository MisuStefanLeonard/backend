
using AutoMapper;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.BulkOperationsDto;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ManopereDto.ManoperaForSet;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductOptionsDto;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.Options;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.ProductPage;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.ProductPage.OptionsForCurtain;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.ReviewsDto;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.SeturiDtos;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.SeturiDtos.DtoForProductOptions;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.SeturiDtos.User;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.SeturiDtos.User.SetPage;
using E_Commerce_BackEnd.Models.Enums;
using E_Commerce_BackEnd.Models.ProductRelatedModels;
using E_Commerce_BackEnd.Models.ProductRelatedModels.JSON_Models;
using E_Commerce_BackEnd.Services.Helpers.AWS_Secret.AWSBucket_CRUD;
using E_Commerce_BackEnd.Services.Helpers.UserHelpers;
using E_Commerce_BackEnd.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.IdentityModel.Tokens;

namespace E_Commerce_BackEnd.Services.uSeturiService;

public class SeturiService : ISeturiService
{
    
    private const int PageSize = 10;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<SeturiService> _logger;
    private readonly IBucketAcces _bucketAcces;

    public SeturiService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<SeturiService> logger, IBucketAcces bucketAcces)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _bucketAcces = bucketAcces;
    }

    public async Task<IList<SeturiDisplayDto>?> GetProductSets()
    {
        var setRepository = _unitOfWork.Repository<Seturi>();

        var listOfProductsSets = await setRepository
            .FindQueryable(s => !s.IsDeleted)
            .ToListAsync();
        
        var listToDto = _mapper.Map<IList<SeturiDisplayDto>>(listOfProductsSets);
        
        return listToDto;

    }
    
    
    // Helper method to add colors to a product
    private void AddColorsToProduct(ProductForSetDto productDto, Produse product, List<AsociereSeturi> listOfProductsOnSets)
    {
        foreach (var colorLink in product.PProduseCuCulori!)
        {
            var mappedColor = _mapper.Map<CuloriDto>(colorLink.Culoare);
           
            if (productDto.ProductOptions.ColorsVariaties.All(c => c.CodCuloareDto != mappedColor.CodCuloareDto))
            {
                mappedColor!.JustAdded = listOfProductsOnSets.Any(asoc => 
                    asoc.IdProdus == product.IdProdus && asoc.IdCuloare == colorLink.IdCuloare);
                productDto.ProductOptions.ColorsVariaties.Add(mappedColor);
                
            }
        }
    }

    // Helper method to add dimensions to a product
    private  void AddDimensionsToProduct(ProductForSetDto productDto, Produse product, List<AsociereSeturi> listOfProductsOnSets)
    {
        foreach (var dimensionLink in product.PProduseCuDimensiuni!)
        {
            var mappedDimension = _mapper.Map<DimensiuniDto>(dimensionLink.PdDimensiune,opt => 
            {
                opt.Items["Product"] = product;  // Pass the product in the mapping context
            });
           

            if (!productDto.ProductOptions.DimensionVariaties.Any(d =>
                d.LungimeDto == mappedDimension.LungimeDto && d.LatimeDto == mappedDimension.LatimeDto))
            {
                mappedDimension.JustAdded = listOfProductsOnSets.Any(asoc =>
                    asoc.IdProdus == product.IdProdus && asoc.IdDimensiune == dimensionLink.IdDimensiune);
                productDto.ProductOptions.DimensionVariaties.Add(mappedDimension);
            }
        }
    }

    // Helper method to add manopere to a product
    private async Task AddManopereToProduct(ProductForSetDto productDto, Produse product, List<AsociereSeturi> listOfProductsOnSets,List<Manopere> standardManopere)
    {
        if (product.TipulProdusuluiJson.TipProdusRomana != "perdea" && product.TipulProdusuluiJson.TipProdusRomana != "draperie")
            return;
        
        foreach (var manopera in standardManopere)
        {
            if (productDto.ProductOptions.StandardManopere!.Any(m => m.NumeManoperaJson!.NumeRomana == manopera.NumeManoperaJson!.NumeRomana))
            {
                _logger.LogInformation($"Manopera '{manopera.NumeManoperaJson!.NumeRomana}' already exists in StandardManopere. Skipping...");
                continue;
            }

            var mappedManopera = new StandardManopereOnSet
            {
                // NumeManopera = manopera.NumeManoperaJson!.NumeRomana!,
                NumeManoperaJson = manopera.NumeManoperaJson!,
                MetruTotalFolosit = manopera.MaterialFolosit,
                TipInel = manopera.InelPrindereLaManopera != null ? new TipIneleDto
                {
                    NumeTipInel = manopera.InelPrindereLaManopera?.CuloareInelJson.CuloareRomana,
                    CuloareInelJsonDto = manopera.InelPrindereLaManopera?.CuloareInelJson,
                    CaleRelativa = manopera.InelPrindereLaManopera?.CaleRelativa,
                    PresignedUrl = "empty"
                } : null ,
                TipGalerie = new TipRejansaDto
                {
                    // NumeTipRejansa = manopera.TipGalerieLaManopera.NumeTipGalerieJson.NumeRomana,
                    NumeTipRejansaDto = manopera.TipGalerieLaManopera.NumeTipGalerieJson,
                    PretTipRejansa = manopera.TipGalerieLaManopera.PretTipGalerie,
                    IncretireRejansa = manopera.TipGalerieLaManopera.IncretireRejansa,
                    CaleRelativa = manopera.TipGalerieLaManopera.CaleRelativa,
                    PresignedUrl = "empty",
                    SePrindeCuInele = manopera.TipGalerieLaManopera.SePrindeCuInele
                },
                TipLinie = new TipLinieDto
                {
                    // NumeTipCusaturaColt = manopera.TipLinieLaManopera.NumeTipLinieJson.NumeRomana,
                    NumeTipCusaturaColtJson = manopera.TipLinieLaManopera.NumeTipLinieJson,
                    PretTipCusaturaColt = manopera.TipLinieLaManopera.PretPeTipLinie,
                    CaleRelativa = manopera.TipLinieLaManopera.CaleRelativa,
                    PresignedUrl = null
                }
            };

            if (mappedManopera.TipInel?.CaleRelativa != null)
            {
                mappedManopera.TipInel.PresignedUrl =
                    await _bucketAcces.GenerateUrl(mappedManopera.TipInel.CaleRelativa, "inele_prindere");
            }

            if (mappedManopera.TipGalerie.CaleRelativa != null)
            {
                mappedManopera.TipGalerie.PresignedUrl =
                    await _bucketAcces.GenerateUrl(mappedManopera.TipGalerie.CaleRelativa, "tipuri_galerie");
            }
            
            if (mappedManopera.TipLinie.CaleRelativa != null)
            {
                mappedManopera.TipLinie.PresignedUrl =
                    await _bucketAcces.GenerateUrl(mappedManopera.TipLinie.CaleRelativa, "tipuri_linie");
            }

            productDto.ProductOptions.StandardManopere!.Add(mappedManopera);
        }

        foreach (var asoc in listOfProductsOnSets.Where(a => a.IdProdus == product.IdProdus && a.Manopera != null))
        {
            // if (productDto.SelectedManopere.Contains(asoc.Manopera!.NumeManopera!))
            //     continue;
            // productDto.SelectedManopere.Add(asoc.Manopera!.NumeManopera!);
            if (productDto.SelectedManopere.Contains(asoc.Manopera!.NumeManoperaJson!.NumeRomana))
                continue;
            productDto.SelectedManopere.Add(asoc.Manopera!.NumeManoperaJson.NumeRomana);
        }
    }
    
    public async Task<SetModificationDto?> GetSetPage(int idSet)
    {
        var productWithSetsRepository = _unitOfWork.Repository<AsociereSeturi>();
        var seturiRepository = _unitOfWork.Repository<Seturi>();
        var productsRepository = _unitOfWork.Repository<Produse>();

        // Retrieve the set
        var currentSet = await seturiRepository
            .FindQueryable(s => s.IdSet == idSet)
            .FirstOrDefaultAsync();

        if (currentSet is null)
        {
            return null;
        }
        

        // Retrieve all products associated with the set
        var listOfProductsOnSets = await productWithSetsRepository
            .FindQueryable(asoc => asoc.IdSet == idSet)
            .Include(c => c.AsCuloare)
                .ThenInclude(cc => cc!.CodCuloare)
            .Include(d => d.AsDimensiune)
            .Include(m => m.Manopera)
                .ThenInclude(tg => tg!.TipGalerieLaManopera)
            .Include(m => m.Manopera)
                .ThenInclude(ip => ip!.InelPrindereLaManopera)
            .Include(m => m.Manopera)
                .ThenInclude(tl => tl!.TipLinieLaManopera)
            .AsSplitQuery()
            .ToListAsync();

        var standardManopere = await _unitOfWork.Repository<Manopere>()
            .GetSimpleQueryable()
            .Where(m => m.TipManopera == TipManopere.Standard)
            .Include(tg => tg.TipGalerieLaManopera)
            .Include(ip => ip.InelPrindereLaManopera)
            .Include(tl => tl.TipLinieLaManopera)
            .ToListAsync();
        

        var listOfProductVariaties = new List<ProductForSetDto>();

        foreach (var setIterator in listOfProductsOnSets)
        {
            var currentProductOnSet = await productsRepository
                .FindQueryable(p => p.IdProdus == setIterator.IdProdus)
                .Include(c => c.PProduseCuCulori!)
                    .ThenInclude(c => c.Culoare)
                .ThenInclude(cc => cc.CodCuloare)
                .Include(d => d.PProduseCuDimensiuni!)
                    .ThenInclude(d => d.PdDimensiune)
                .FirstAsync();

            // Check if the product is already in the list
            var existingProduct = listOfProductVariaties.FirstOrDefault(p => p.CodProdusDto == currentProductOnSet.CodProdus);

            if (existingProduct == null)
            {
               
                var mappedProduct = _mapper.Map<ProductForSetDto>(currentProductOnSet);
                AddColorsToProduct(mappedProduct, currentProductOnSet, listOfProductsOnSets);
                AddDimensionsToProduct(mappedProduct, currentProductOnSet, listOfProductsOnSets);
                await AddManopereToProduct(mappedProduct, currentProductOnSet, listOfProductsOnSets,standardManopere);

                listOfProductVariaties.Add(mappedProduct);
            }
            else
            {
                // Merge variations into the existing product
                AddColorsToProduct(existingProduct, currentProductOnSet, listOfProductsOnSets);
                AddDimensionsToProduct(existingProduct, currentProductOnSet, listOfProductsOnSets);
                await AddManopereToProduct(existingProduct, currentProductOnSet, listOfProductsOnSets,standardManopere);
            }
        }

        // Create the SetModificationDto to return
        var setModificationDto = new SetModificationDto
        {
            ProductsOnSet = listOfProductVariaties,
            // NumeSetDto = currentSet.NumeSet,
            NumeSetJsonDto = currentSet.NumeSetJson,
            // DescriereSetDto = currentSet.DescriereSet,
            DescriereSetJsonDto = currentSet.DescriereJson,
            PretSetDto = currentSet.PretSet,
            PretRedusSetDto = currentSet.PretRedusSet
        };

        return setModificationDto;
    }
    

    public async Task<IList<SelectProducts>> GetProductCodes()
    {
        var productsRepository = _unitOfWork.Repository<Produse>();
        
        var listOfProductCodes = await productsRepository
            .GetSimpleQueryable()
            .GroupBy(p => EF.Functions.JsonUnquote(EF.Functions.JsonExtract<string>(p.TipulProdusuluiJson , "$.tip_ro")))
            .Select(group => new SelectProducts
            {
                Title = group.Key.ToUpper(),  // Mapping to TipulProdusuluiDto
                Children = group.Select(p => new ProdusDto
                {
                    Id = p.CodProdus,
                    Title = p.NumeProdusJson.NumeRomana.ToUpper()// Mapping to CodProdus
                }).ToList()  // Projecting to the ProduseDto list
            })
            .ToListAsync();

        return listOfProductCodes;
    }

    public async Task<IList<Nume>> GetSetNames()
    {
        var seturiRepository = _unitOfWork.Repository<Seturi>();

        var listOfSeturiNames = await seturiRepository
            .GetSimpleQueryable()
            .Select(s => s.NumeSetJson)
            .ToListAsync();

        return listOfSeturiNames;
    }

    public async Task<int> DeleteSet(int idSet)
    {
        IDbContextTransaction? deleteTransaction = null;
        try
        {
          deleteTransaction = await _unitOfWork.BeginTransactionAsync();

            var setsRepository = _unitOfWork.Repository<Seturi>();

            var setToDelete = await setsRepository
                .FindQueryable(set => set.IdSet == idSet)
                .FirstAsync();

            if (setToDelete.IsLocked)
            {
                return -3;
            }
            
            

            if (setToDelete.CombinatieSetPeComanda.IsNullOrEmpty())
            {
                await setsRepository.DeleteAsync(setToDelete);
                _logger.LogInformation("Set had no orders. Deleted forever");
            }
            else
            {
                setToDelete.IsDeleted = true;
                await setsRepository.UpdateAsync(setToDelete);
                _logger.LogInformation("Set had orders. Transparent delete performed");
            }

            await _unitOfWork.CommitTransactionAsync(deleteTransaction);
            return 1;

        }
        catch (Exception e)
        {
            if (deleteTransaction != null)
            {
                await _unitOfWork.RollBackTransactionAsync(deleteTransaction);
            }
            if (e is ArgumentNullException or InvalidOperationException)
            {
                _logger.LogError("No sets present into database / Set not found");
                return -1;
            }
            _logger.LogError("General error occured at set deletion/transaparent deletion");
            return 0;
        }
    }

    public async Task<int> DeleteBulkSets(BulkOperationsDto sets)
    {
        var setsRepository = _unitOfWork.Repository<Seturi>();

        IDbContextTransaction? deleteBulkTransaction = null;

        try
        {
            deleteBulkTransaction = await _unitOfWork.BeginTransactionAsync();
            List<Seturi> setsToBeDeleted = [];
            List<Seturi> setsToBeUpdatedToDeleted = [];

            foreach (var item in sets.SelectedItemsToDoBulkOperations!)
            {
                var setToBeDeleted = await setsRepository
                    .FindQueryable(set => set.IdSet == int.Parse(item.ToString()!))
                    .FirstOrDefaultAsync();
                

                if (setToBeDeleted is not null)
                {
                    if (setToBeDeleted.IsLocked)
                    {
                        return -3;
                    }
                    var hasOrders = setToBeDeleted.CombinatieSetPeComanda.IsNullOrEmpty();
                    if (hasOrders)
                    {
                        setsToBeDeleted.Add(setToBeDeleted);
                    }
                    else
                    {
                        setsToBeUpdatedToDeleted.Add(setToBeDeleted);
                    }
                    
                }
                else
                {
                    _logger.LogInformation("Set already has been deleted (bulk operation delete)");
                }

            }
            

            if (setsToBeDeleted.Count != 0)
            {
                await setsRepository.DeleteRangeAsync(setsToBeDeleted);
            }

            if (setsToBeUpdatedToDeleted.Count != 0)
            {
                await setsRepository.UpdateRangeAsync(setsToBeUpdatedToDeleted);

            }
          
            await _unitOfWork.CommitTransactionAsync(deleteBulkTransaction);
            return 1;
        }
        catch (Exception e)
        {
            if (deleteBulkTransaction is not null)
            {
                await _unitOfWork.RollBackTransactionAsync(deleteBulkTransaction);
            }
            _logger.LogError(e.Message);
            return -1;
        }
    }

    public async Task<int> ActivateSet(int idSet , bool activationState)
    {
        IDbContextTransaction? updateTransaction = null;
        try
        {
            updateTransaction = await _unitOfWork.BeginTransactionAsync();

            var setsRepository = _unitOfWork.Repository<Seturi>();

            var setToUpdate = await setsRepository
                .FindQueryable(set => set.IdSet == idSet)
                .FirstAsync();
            
            // set locked
            if (setToUpdate.IsLocked)
            {
                return -3;
            }


            setToUpdate.SetActivInMagazin = activationState;
            await setsRepository.UpdateAsync(setToUpdate);

            _logger.LogInformation(activationState == false
                ? "Successfully deactivated set"
                : "Successfully activated set into the shop");


            await _unitOfWork.CommitTransactionAsync(updateTransaction);
            return 1;

        }
        catch (Exception e)
        {
            if (updateTransaction != null)
            {
                await _unitOfWork.RollBackTransactionAsync(updateTransaction);
            }
            if (e is ArgumentNullException or InvalidOperationException)
            {
                _logger.LogError("No sets present into database / Set not found");
                return -1;
            }
            _logger.LogError("General error occured at set update activation");
            return 0;
        }
    }

    public async Task<int> ActivateBulkSets(BulkOperationsDto sets)
    {
        var setsRepository = _unitOfWork.Repository<Seturi>();

        IDbContextTransaction? updateBulkTransaction = null;

        try
        {
            updateBulkTransaction = await _unitOfWork.BeginTransactionAsync();
            var setsToActivate = new List<Seturi>();
            foreach (var item in sets.SelectedItemsToDoBulkOperations!)
            {
                var setToUpdate = await setsRepository
                    .FindQueryable(set => set.IdSet == int.Parse(item.ToString()!))
                    .FirstOrDefaultAsync();
                
                if (setToUpdate is not null)
                {
                    if (setToUpdate.IsLocked)
                    {
                        return -3;
                    }
                    setToUpdate.SetActivInMagazin = true;
                    setsToActivate.Add(setToUpdate);
                }
                else
                {
                    _logger.LogError($"Set with id {item} has not been found");
                    _logger.LogError($"Skipping set with id {item}");
                }
                
               
            }

            await setsRepository.UpdateRangeAsync(setsToActivate);
          
            await _unitOfWork.CommitTransactionAsync(updateBulkTransaction);
            return 1;
        }
        catch (Exception e)
        {
            if (updateBulkTransaction is not null)
            {
                await _unitOfWork.RollBackTransactionAsync(updateBulkTransaction);
            }
            _logger.LogError(e.Message);
            return -1;
        }
    }

    public async Task<int> DeleteProductFromSet(int idSet,int idProdus)
    {
        IDbContextTransaction? deleteTransaction = null;
        try
        {
            deleteTransaction = await _unitOfWork.BeginTransactionAsync();
            
            var productWithSetsRepository = _unitOfWork.Repository<AsociereSeturi>();

            var findSet = await _unitOfWork.Repository<Seturi>()
                .FindQueryable(set => set.IdSet == idSet)
                .FirstOrDefaultAsync();

            if (findSet is { IsLocked: true })
            {
                return -3;
            }
           
            var findAllProductVariationsToDelete = await productWithSetsRepository
                .FindQueryable(asoc => asoc.IdProdus == idProdus && asoc.IdSet == idSet)
                .ToListAsync();
            
            

            if (findAllProductVariationsToDelete.Count == 0)
            {
                await _unitOfWork.CommitTransactionAsync(deleteTransaction);
                return 1;
            }

            await productWithSetsRepository.DeleteRangeAsync(findAllProductVariationsToDelete);
            await _unitOfWork.CommitTransactionAsync(deleteTransaction);
            return 1;

        }
        catch (Exception e)
        {
            if (deleteTransaction != null)
            {
                await _unitOfWork.RollBackTransactionAsync(deleteTransaction);
            }

            _logger.LogError(e.Message);

            return e switch
            {
                InvalidOperationException => -2, // sequence empty
               _ => -1
            };
            
        }
    }

    public async Task<int> AddOrUpdateSet(SetModificationDto modifiedSet, int idSet,bool isAdding)
    {
        IDbContextTransaction? updateOrAddTransaction = null;
        try
        {
            updateOrAddTransaction = await _unitOfWork.BeginTransactionAsync();
            var seturiRepository = _unitOfWork.Repository<Seturi>();
            var colorsRepository = _unitOfWork.Repository<Culori>();
            var dimensionsRepository = _unitOfWork.Repository<Dimensiuni>();
            var manopereRepository = _unitOfWork.Repository<Manopere>();
            var asociereSeturiRepository = _unitOfWork.Repository<AsociereSeturi>();
            
            if (!isAdding)
            {
                var findSet = await _unitOfWork.Repository<Seturi>()
                    .FindQueryable(set => set.IdSet == idSet).FirstOrDefaultAsync();
                // set locked
                if (findSet is { IsLocked: true })
                {
                    return -3;
                }
                
                var listOfNewOptions = new List<AsociereSeturi>();

                var oldOptions = await asociereSeturiRepository
                    .FindQueryable(asoc => asoc.IdSet == idSet)
                    .ToListAsync();

                foreach (var productOnSet in modifiedSet.ProductsOnSet)
                {
                    // blue-08 , grey-03
                    var selectedColors = productOnSet.SelectedColors;
                    // 200x190 , 200x140
                    var selectedDimensions = productOnSet.SelectedDimensions;
                    // nume manopera (test)
                    var selectedManopere = productOnSet.SelectedManopere;

                    if (productOnSet.TipProdusJsonDto!.TipProdusRomana is "perdea" or "draperie")
                    {
                        foreach (var color in selectedColors)
                        {
                            var splitColorByCode = color.Split('-');
                            var colorName = splitColorByCode[0];
                            var colorCode = splitColorByCode[1];
                            

                            var colorInDb = await colorsRepository
                                .GetSimpleQueryable()
                                .Where(c => EF.Functions.JsonUnquote(EF.Functions.JsonExtract<string>(c.NumeCuloareJson , "$.culoare_ro")) == colorName
                                            && c.CodCuloare.CodCuloare == colorCode)
                                .FirstAsync();

                            foreach (var manopera in selectedManopere)
                            {
                                var findManoperaInDb = await manopereRepository
                                    .GetSimpleQueryable()
                                    .Where(m => EF.Functions.JsonUnquote(EF.Functions.JsonExtract<string>(m.NumeManoperaJson! , "$.nume_ro")) == manopera)
                                    .FirstAsync();

                                var isManoperaVariantionInDb = oldOptions
                                    .FirstOrDefault(asoc => asoc.IdProdus == productOnSet.IdProdusDto
                                                            && asoc.IdManopera == findManoperaInDb.IdManopera &&
                                                            asoc.IdCuloare == colorInDb.IdCuloare);

                                if (isManoperaVariantionInDb is null)
                                {
                                    var newProductOptionOnSet = new AsociereSeturi
                                    {
                                        IdProdus = productOnSet.IdProdusDto,
                                        IdSet = idSet,
                                        IdCuloare = colorInDb.IdCuloare,
                                        IdDimensiune = null,
                                        IdManopera = findManoperaInDb.IdManopera
                                    };
                                    listOfNewOptions.Add(newProductOptionOnSet);
                                }
                                else
                                {
                                    oldOptions.Remove(isManoperaVariantionInDb);
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (var color in selectedColors)
                        {
                            var splitColorByCode = color.Split('-');
                            var colorName = splitColorByCode[0];
                            var colorCode = splitColorByCode[1];

                            var colorInDb = await colorsRepository
                                .GetSimpleQueryable()
                                .Where(c =>EF.Functions.JsonUnquote(EF.Functions.JsonExtract<string>(c.NumeCuloareJson , "$.culoare_ro")) == colorName
                                           && c.CodCuloare.CodCuloare == colorCode)
                                .FirstAsync();

                            if (selectedDimensions.Count != 0)
                            {
                                foreach (var selectedDimension in selectedDimensions)
                                {
                                    var splitDimensionFromRecomandarePat = selectedDimension.Split('-');
                                    var dimensions = splitDimensionFromRecomandarePat[0].Split('x');


                                    var lungime = dimensions[0];
                                    var latime = dimensions[1];
                                    var recomandarePat = splitDimensionFromRecomandarePat[1];
                                    var dimensionInDb = await dimensionsRepository
                                        .FindQueryable(d => d.Lungime == lungime &&
                                                            d.Latime == latime &&
                                                            d.RecomandarePat == recomandarePat)
                                        .FirstAsync();


                                    var isVariationInDb = oldOptions
                                        .FirstOrDefault(asoc => asoc.IdProdus == productOnSet.IdProdusDto &&
                                                                asoc.IdSet == idSet &&
                                                                asoc.IdCuloare == colorInDb.IdCuloare &&
                                                                asoc.IdDimensiune == dimensionInDb.IdDimensiune);

                                    if (isVariationInDb is null)
                                    {

                                        var newProductOptionOnSet = new AsociereSeturi
                                        {
                                            IdProdus = productOnSet.IdProdusDto,
                                            IdSet = idSet,
                                            IdCuloare = colorInDb.IdCuloare,
                                            IdDimensiune = dimensionInDb.IdDimensiune,
                                        };
                                        listOfNewOptions.Add(newProductOptionOnSet);

                                    }
                                    else
                                    {
                                        oldOptions.Remove(isVariationInDb);
                                    }
                                }
                            }
                            else
                            {
                                var isVariationInDb = oldOptions
                                    .FirstOrDefault(asoc => asoc.IdProdus == productOnSet.IdProdusDto &&
                                                            asoc.IdSet == idSet &&
                                                            asoc.IdCuloare == colorInDb.IdCuloare &&
                                                            asoc.IdDimensiune == null);

                                if (isVariationInDb is null)
                                {
                                    var newProductOptionOnSet = new AsociereSeturi
                                    {
                                        IdProdus = productOnSet.IdProdusDto,
                                        IdSet = idSet,
                                        IdCuloare = colorInDb.IdCuloare,
                                        IdDimensiune = null,
                                    };
                                    listOfNewOptions.Add(newProductOptionOnSet);
                                }
                                else
                                {
                                    oldOptions.Remove(isVariationInDb);
                                }
                            }


                        }
                    }

                  
                }

                // Perform the database operations
                if (listOfNewOptions.Count > 0)
                {
                    await asociereSeturiRepository.AddRangeAsync(listOfNewOptions);
                }

                if (oldOptions.Count > 0)
                {
                    await asociereSeturiRepository.DeleteRangeAsync(oldOptions);
                }

                await _unitOfWork.CommitTransactionAsync(updateOrAddTransaction);
                _logger.LogInformation("Set updated succesfully");

                return 1;
            }
            // else we create a new set.


            var optionsToBeAdded = new List<AsociereSeturi>();
            var newSet = new Seturi
            {
                // NumeSet = modifiedSet.NumeSetDto,
                NumeSetJson = modifiedSet.NumeSetJsonDto,
                // DescriereSet = modifiedSet.DescriereSetDto,
                DescriereJson = modifiedSet.DescriereSetJsonDto,
                PretSet = modifiedSet.PretSetDto,
                PretRedusSet = modifiedSet.PretRedusSetDto,
                SetActivInMagazin = false,
                IsDeleted = false
            };

            await seturiRepository.AddAsync(newSet);
            await _unitOfWork.CommitAsync();
            
            _logger.LogInformation($"Set id after insert in db : {newSet.IdSet}");


            foreach (var productOnSet in modifiedSet.ProductsOnSet)
            {
                _logger.LogError($"Nume produs curent de procesat ---- {productOnSet.CodProdusDto}");
                // blue-08 , grey-03
                var selectedColors = productOnSet.SelectedColors;
                // 200x190 , 200x140
                var selectedDimensions = productOnSet.SelectedDimensions;
                // nume manopera (test)
                var selectedManopere = productOnSet.SelectedManopere;

                if (string.Equals(productOnSet.TipProdusJsonDto!.TipProdusRomana, "draperie") ||
                    string.Equals(productOnSet.TipProdusJsonDto.TipProdusRomana, "perdea"))
                {
                    _logger.LogInformation("Adaugam perdea/ draperie");
                    foreach (var color in selectedColors)
                    {
                        var splitColorByCode = color.Split('-');
                        var colorName = splitColorByCode[0];
                        var colorCode = splitColorByCode[1];

                        var colorInDb = await colorsRepository
                            .GetSimpleQueryable()
                            .Where(c => EF.Functions.JsonUnquote(EF.Functions.JsonExtract<string>(c.NumeCuloareJson , "$.culoare_ro")) == colorName
                                        && c.CodCuloare.CodCuloare == colorCode)
                            .FirstAsync();

                        _logger.LogError($"Culoare curenta : {colorInDb.NumeCuloareJson.CuloareRomana}");

                        foreach (var manopera in selectedManopere)
                        {
                            var findManoperaInDb = await manopereRepository
                                .GetSimpleQueryable()
                                .Where(m => EF.Functions.JsonUnquote(EF.Functions.JsonExtract<string>(m.NumeManoperaJson! , "$.nume_ro")) == manopera)
                                .FirstAsync();
                            _logger.LogError($"Nume manopera curenta : {findManoperaInDb.NumeManoperaJson!.NumeRomana}");


                            var newProductOptionOnSet = new AsociereSeturi
                            {
                                IdProdus = productOnSet.IdProdusDto,
                                IdSet = newSet.IdSet,
                                IdCuloare = colorInDb.IdCuloare,
                                IdDimensiune = null,
                                IdManopera = findManoperaInDb.IdManopera
                            };

                            optionsToBeAdded.Add(newProductOptionOnSet);

                        }
                    }
                }
                else
                {
                    foreach (var color in selectedColors)
                    {
                        var splitColorByCode = color.Split('-');
                        var colorName = splitColorByCode[0];
                        var colorCode = splitColorByCode[1];

                        var colorInDb = await colorsRepository
                            .GetSimpleQueryable()
                            .Where(c => EF.Functions.JsonUnquote(EF.Functions.JsonExtract<string>(c.NumeCuloareJson , "$.culoare_ro")) == colorName
                                        && c.CodCuloare.CodCuloare == colorCode)
                            .FirstAsync();

                        if (selectedDimensions.Count != 0)
                        {
                            foreach (var selectedDimension in selectedDimensions)
                            {
                                var splitDimensionFromRecomandarePat = selectedDimension.Split('-');
                                var dimensions = splitDimensionFromRecomandarePat[0].Split('x');


                                var lungime = dimensions[0];
                                var latime = dimensions[1];
                                var recomandarePat = splitDimensionFromRecomandarePat[1];
                                var dimensionInDb = await dimensionsRepository
                                    .FindQueryable(d => d.Lungime == lungime &&
                                                        d.Latime == latime &&
                                                        d.RecomandarePat == recomandarePat)
                                    .FirstAsync();

                                _logger.LogInformation($"Set id after insert in db : {newSet.IdSet}");

                                var newProductOptionOnSet = new AsociereSeturi
                                {
                                    IdProdus = productOnSet.IdProdusDto,
                                    IdSet = newSet.IdSet,
                                    IdCuloare = colorInDb.IdCuloare,
                                    IdDimensiune = dimensionInDb.IdDimensiune,
                                    IdManopera = null
                                };
                                optionsToBeAdded.Add(newProductOptionOnSet);

                            }
                        }
                        else
                        {

                            var newProductOptionOnSet = new AsociereSeturi
                            {
                                IdProdus = productOnSet.IdProdusDto,
                                IdSet = newSet.IdSet,
                                IdCuloare = colorInDb.IdCuloare,
                                IdDimensiune = null,
                                IdManopera = null
                            };
                            optionsToBeAdded.Add(newProductOptionOnSet);
                        }
                    }


                }
            }

            foreach (var options in optionsToBeAdded)
            {
                _logger.LogInformation($"Optiuni : ID_SET {options.IdSet} .ID_PRODUS: {options.IdProdus} , ID_CULOARE: {options.IdCuloare}" +
                                       $", ID_DIMENSIUNE : {options.IdDimensiune} , ID_MANOPERA : {options.IdManopera}");
            }
                
            await asociereSeturiRepository.AddRangeAsync(optionsToBeAdded);
            await _unitOfWork.CommitTransactionAsync(updateOrAddTransaction);
            _logger.LogInformation("Set created succesfully");
            return 1;
        }
        
        catch (Exception e)
        {
            if (updateOrAddTransaction is not null)
            {
                await _unitOfWork.RollBackTransactionAsync(updateOrAddTransaction);
            }

            _logger.LogError(e.Message);
            _logger.LogError($"Error: {e.GetType()}");

            return -1;
        }
    }

    public async Task<ProductForSetDto?> GetProductDataForSetAdd(string codProdus)
    {
        var productsRepository = _unitOfWork.Repository<Produse>();
        var manopereRepository = _unitOfWork.Repository<Manopere>();
        
        var productToGetData = await productsRepository
            .GetSimpleQueryable()
            .Where(p => p.CodProdus == codProdus.ToUpper())
            .Include(pd => pd.PProduseCuDimensiuni!)
                .ThenInclude(d => d.PdDimensiune)
            .Include(pc => pc.PProduseCuCulori!)
                .ThenInclude(c => c.Culoare)
                .ThenInclude(cc => cc.CodCuloare)
            .FirstAsync();

        var productMapped = _mapper.Map<ProductForSetDto>(productToGetData);
        
        if (!productToGetData.PProduseCuDimensiuni.IsNullOrEmpty())
        {
            foreach (var dimensionLink in productToGetData.PProduseCuDimensiuni!)
            {
                var mappedDimensionToDto = _mapper.Map<DimensiuniDto>(dimensionLink.PdDimensiune, opt => 
                {
                    opt.Items["Product"] = productToGetData;  
                });
                
                productMapped.ProductOptions.DimensionVariaties.Add(mappedDimensionToDto);
            }
        }

        if (!productToGetData.PProduseCuCulori.IsNullOrEmpty())
        {
            foreach (var colorLink in productToGetData.PProduseCuCulori!)
            {
                var mappedColorToDto = _mapper.Map<CuloriDto>(colorLink.Culoare, opt => 
                {
                    opt.Items["Product"] = productToGetData;  
                });
                
                productMapped.ProductOptions.ColorsVariaties.Add(mappedColorToDto);
            }
        }


        if (productToGetData.TipulProdusuluiJson.TipProdusRomana is "perdea" or "draperie")
        {
            var standardManopere = await manopereRepository
                .GetSimpleQueryable()
                .Where(m => m.TipManopera == TipManopere.Standard)
                .Select(m => new StandardManopereOnSet
                {
                    // NumeManopera = m.NumeManoperaJson.NumeRomana!,
                    NumeManoperaJson = m.NumeManoperaJson,
                    MetruTotalFolosit = m.MaterialFolosit,
                    TipInel = new TipIneleDto
                    {
                        // NumeTipInel = m.InelPrindereLaManopera == null ? null : m.InelPrindereLaManopera.CuloareInel,
                        CuloareInelJsonDto = m.InelPrindereLaManopera == null ? null : m.InelPrindereLaManopera.CuloareInelJson,
                        CaleRelativa = m.InelPrindereLaManopera!.CaleRelativa,
                        PresignedUrl = "empty"
                    },
                    TipGalerie = new TipRejansaDto
                    {
                        // NumeTipRejansa = m.TipGalerieLaManopera.NumeTipGalerie,
                        NumeTipRejansaDto = m.TipGalerieLaManopera.NumeTipGalerieJson,
                        PretTipRejansa = m.TipGalerieLaManopera.PretTipGalerie,
                        IncretireRejansa = m.TipGalerieLaManopera.IncretireRejansa,
                        CaleRelativa = m.TipGalerieLaManopera.CaleRelativa,
                        PresignedUrl = "empty",
                        SePrindeCuInele = m.TipGalerieLaManopera.SePrindeCuInele
                    },
                    TipLinie = new TipLinieDto
                    {
                        // NumeTipCusaturaColt = m.TipLinieLaManopera.NumeTipLinie,
                        NumeTipCusaturaColtJson = m.TipLinieLaManopera.NumeTipLinieJson,
                        PretTipCusaturaColt = m.TipLinieLaManopera.PretPeTipLinie,
                        CaleRelativa = m.TipLinieLaManopera.CaleRelativa,
                        PresignedUrl = "empty"
                    },
                    InaltimeMaxima = m.InaltimeMaxima,
                })
                .ToListAsync();

            foreach (var manopera in standardManopere)
            {
                if (manopera.TipInel?.CaleRelativa != null)
                {
                    manopera.TipInel.PresignedUrl = await _bucketAcces
                        .GenerateUrl(manopera.TipInel.CaleRelativa, "inele_prindere");
                }

                if (manopera.TipGalerie.CaleRelativa != null)
                {
                    manopera.TipGalerie.PresignedUrl = await _bucketAcces
                        .GenerateUrl(manopera.TipGalerie.CaleRelativa, "tipuri_galerie");
                }

                if (manopera.TipLinie.CaleRelativa != null)
                {
                    manopera.TipLinie.PresignedUrl = await _bucketAcces
                        .GenerateUrl(manopera.TipLinie.CaleRelativa, "tipuri_linie");
                }
            }

            productMapped.ProductOptions.StandardManopere = standardManopere;
        }
       

        return productMapped;

    }

    public async Task<FinalSetDisplayForUsers> GetSetsForUsers
    (
        int? pageNumber, 
        List<string>? productTypes, 
        List<decimal>? productPrices,
        string? productName,
        string currency = "RON")
    {
        var seturiRepository = _unitOfWork.Repository<Seturi>();

        var mainQuery = seturiRepository
            .GetSimpleQueryable()
            .Include(s => s.SAsociereSeturi!)
                .ThenInclude(p => p.Produs)
                    .ThenInclude(p => p.PProduseCuCulori!)
                        .ThenInclude(p => p.Culoare)
            .Include(set => set.SAsociereSeturi!)
                .ThenInclude(asoc => asoc.Produs)
                    .ThenInclude(prod => prod.PProduseCuCulori!)
                        .ThenInclude(color => color.ImagProduseCuCulori)
            .Include(set => set.ReviewPeSet)
            .Where(set => productName.IsNullOrEmpty() ||
                          set.SAsociereSeturi!
                              .Any(product =>
                                  EF.Functions.JsonUnquote(EF.Functions.JsonExtract<string>(product.Produs.NumeProdusJson, "$.nume_ro")).ToLower().Contains(productName!.ToLower()) ||
                                  EF.Functions.JsonUnquote(EF.Functions.JsonExtract<string>(product.Produs.NumeProdusJson, "$.nume_en")).ToLower().Contains(productName.ToLower())
                              )
            )
            .Where(set => !set.IsDeleted && set.SetActivInMagazin)
            .Where(set => productTypes.IsNullOrEmpty() || set.SAsociereSeturi!
                .Any(product => productTypes!.Contains(EF.Functions.JsonUnquote(EF.Functions.JsonExtract<string>(product.Produs.TipulProdusuluiJson , "$.tip_ro").ToLower()))))
            .Where(set => productPrices.IsNullOrEmpty() ||
                          currency == "EUR"
                ? set.PretRedusSet > 0
                    ? set.PretRedusSet * (decimal)0.2 >= productPrices![0] &&
                      set.PretRedusSet * (decimal)0.2 <= productPrices[1]
                    : set.PretSet * (decimal)0.2 >= productPrices![0] &&
                      set.PretSet * (decimal)0.2 <= productPrices[1]
                : set.PretRedusSet > 0
                    ? set.PretRedusSet >= productPrices![0] && set.PretRedusSet  <= productPrices[1]
                    : set.PretSet >= productPrices![0] && set.PretSet  <= productPrices[1]);
        
        
        var totalSetsFiltered = await mainQuery.CountAsync();

        var takePaginatedSets = await mainQuery
            .OrderBy(set =>  EF.Functions.JsonUnquote(EF.Functions.JsonExtract<string>(set.NumeSetJson, "$.nume_ro")))
            .Skip((pageNumber ?? 0) * PageSize)
            .Take(PageSize)
            .Select(set => new SetsListingForUsers
            {
                EncodedIdSet  = set.EncodedIdSet,
                NumeSetJsonDto =  set.NumeSetJson,
                PretSetDto =
                    currency == "EUR" ? UserHelpers.ConvertCurrency("RON", "EUR", set.PretSet, 0) : set.PretSet,
                PretRedusSetDto = currency == "EUR"
                    ? UserHelpers.ConvertCurrency("RON", "EUR", set.PretRedusSet, 0)
                    : set.PretRedusSet,
                SetProductsDto = set.SAsociereSeturi!
                    .GroupBy(asoc => EF.Functions.JsonUnquote(EF.Functions.JsonExtract<string>(asoc.Produs.NumeProdusJson, "$.nume_ro")))
                    .Select(asoc => new ProductsInSet
                    {
                        NumeProdusDto = currency == "RON" ?  asoc.First().Produs.NumeProdusJson.NumeRomana : asoc.First().Produs.NumeProdusJson.NumeEngleza,
                        CuloriProdusDto = asoc.SelectMany(a => a.Produs.PProduseCuCulori!)
                            .Where(color => asoc.Any(a => a.IdCuloare == color.IdCuloare))
                            .Distinct()
                            .Select(color => new ColorsWithImages
                            {
                                NumeCuloareDto = currency == "RON" ?  color.Culoare.NumeCuloareJson.CuloareRomana :  color.Culoare.NumeCuloareJson.CuloareEngleza,
                                ImaginiProdusDto = color.ImagProduseCuCulori!.Select(imag => new ImagesDtoForUsers
                                {
                                    CaleImagineDto = imag.CaleImagine!,
                                    FisierInBucketDto = imag.FisierInBucket,
                                    PresignedUrl = null
                                }).ToList()
                            }).ToList()
                    }).ToList(),
                ReviewsInfoGeneral = new ReviewsInfoForQuickDisplay
                {
                    TotalReviews = set.ReviewPeSet!.Count,
                    AverageRating = set.ReviewPeSet.Count > 0
                        ? Math.Round(set.ReviewPeSet.Average(avg => avg.NumarStele), 1)
                        : 0.0,
                }
            }).ToListAsync();


        foreach (var imag in from set
                     in takePaginatedSets from products
                     in set.SetProductsDto from color 
                     in products.CuloriProdusDto 
                 where !color.ImaginiProdusDto.IsNullOrEmpty() from imag 
                     in color.ImaginiProdusDto! select imag)
        {
            imag.PresignedUrl = await _bucketAcces.GenerateUrl(imag.CaleImagineDto, imag.FisierInBucketDto);
        }

        return new FinalSetDisplayForUsers
        {
            ShopSets = takePaginatedSets,
            TotalSetsListed = totalSetsFiltered
        };
    }

    public async Task<KeyValuePair<int,SetPage?>> GetSetForUser(int setId,string setName , string currency = "RON")
    {
        try
        {
            _logger.LogInformation($"set id : {setId} , set name : {setName}");
            var setPageInfo = await _unitOfWork.Repository<Seturi>()
                .GetSimpleQueryable()
                .Include(set => set.SAsociereSeturi!)
                    .ThenInclude(asoc => asoc.Produs)
                        .ThenInclude(prod => prod.PProduseCuCulori!)
                            .ThenInclude(color => color.Culoare)
                .Include(set => set.SAsociereSeturi!)
                    .ThenInclude(asoc => asoc.Produs)
                        .ThenInclude(prod => prod.PProduseCuCulori!)
                            .ThenInclude(color => color.ImagProduseCuCulori)
                .Include(set => set.SAsociereSeturi!)
                    .ThenInclude(asoc => asoc.Produs)
                        .ThenInclude(prod => prod.PProduseCuDimensiuni!)
                            .ThenInclude(dim => dim.PdDimensiune)
                .Include(set => set.SAsociereSeturi!)
                    .ThenInclude(asoc => asoc.Produs)
                        .ThenInclude(prod => prod.Producator)
                .Include(set => set.SAsociereSeturi!)
                    .ThenInclude(asoc => asoc.Manopera)
                        .ThenInclude(man => man!.InelPrindereLaManopera)
                .Include(set => set.SAsociereSeturi!)
                    .ThenInclude(asoc => asoc.Manopera)
                        .ThenInclude(man => man!.TipGalerieLaManopera)
                .Include(set => set.SAsociereSeturi!)
                    .ThenInclude(asoc => asoc.Manopera)
                        .ThenInclude(man => man!.TipLinieLaManopera)
                .Include(set => set.ReviewPeSet) 
                .Where(set => EF.Functions.JsonUnquote(EF.Functions.JsonExtract<string>(set.NumeSetJson , "$.nume_ro")).ToLower() == setName.ToLower() 
                              && set.IdSet == setId
                              && set.SetActivInMagazin
                              && !set.IsDeleted)
                .Select(set => new SetPage
                {
                    NumeSetDto = currency == "RON" ? set.NumeSetJson.NumeRomana : set.NumeSetJson.NumeEngleza,
                    // NumeSetJsonDto = set.NumeSetJson,
                    PretSetDto = currency == "EUR" ? set.PretSet * (decimal)0.2 : set.PretSet,
                    PretRedusSetDto = currency == "EUR" ? set.PretRedusSet * (decimal)0.2 : set.PretRedusSet,
                    DescriereSetDto = currency == "RON" ? set.DescriereJson.DescriereRomana! : set.DescriereJson.DescriereEngleza!,
                    // DescriereJsonDto = set.DescriereJson,
                    ProdusePeSet = set.SAsociereSeturi!
                        .GroupBy(product => product.Produs.CodProdus)
                        .Select(asoc => new ProductOnSet
                        {
                            IdProdus = asoc.First().IdProdus,
                            CodProdusDto = asoc.Key,
                            DescriereDto = currency == "RON" ? asoc.First().Produs.DescriereJson!.DescriereRomana : asoc.First().Produs.DescriereJson!.DescriereEngleza,
                            NumeProdusDto = currency == "RON" ? asoc.First().Produs.NumeProdusJson.NumeRomana : asoc.First().Produs.NumeProdusJson.NumeEngleza,
                            CompozitieDto = currency == "RON" ? asoc.First().Produs.CompozitieJson!.CompozitieRomana :  asoc.First().Produs.CompozitieJson!.CompozitieEngleza,
                            TvaDto = asoc.First().Produs.Tva,
                            IngrijireDto = currency == "RON" ? asoc.First().Produs.IngrijireJson!.IngrijireRomana : asoc.First().Produs.IngrijireJson!.IngrijireEngleza,
                            FataReversibilaDto = asoc.First().Produs.FataReversibila,
                            TipulProdusuluiDto = currency == "RON" ? asoc.First().Produs.TipulProdusuluiJson.TipProdusRomana : asoc.First().Produs.TipulProdusuluiJson.TipProdusEngleza,
                            NumeProducatorDto = asoc.First().Produs.Producator != null 
                                ? asoc.First().Produs.Producator!.NumeProducator 
                                : null,
                            SelectedColors = asoc
                                .SelectMany(a => a.Produs.PProduseCuCulori!
                                    .Where(color =>  a.IdCuloare == color.IdCuloare))
                                .Distinct()
                                .Select(colorDto => new CuloriDto
                                {
                                    IdCuloare = colorDto.IdCuloare,
                                    NumeCuloareDto = currency == "RON" ? colorDto.Culoare.NumeCuloareJson.CuloareRomana : colorDto.Culoare.NumeCuloareJson.CuloareEngleza,
                                    CodCuloareDto = colorDto.Culoare.CodCuloare.CodCuloare!,
                                    JustAdded = false,
                                    ImaginiProdusDto = colorDto.ImagProduseCuCulori != null 
                                        ? colorDto.ImagProduseCuCulori
                                            .Select(imag => new ImagesDto
                                            {
                                                CaleImagineDto = imag.CaleImagine!,
                                                FisierInBucketDto = imag.FisierInBucket,
                                                PresignedUrl = null,
                                                JustAdded = false,
                                            }).ToList()
                                        : new List<ImagesDto>()
                                }).ToList(),
                            
                            SelectedDimensions = asoc
                                .SelectMany(a => a.Produs.PProduseCuDimensiuni!
                                        .Where(dimension =>  a.IdDimensiune == dimension.IdDimensiune)
                                        .Select(dimensionDto => new DimensiuniDto
                                        {
                                            IdDimensiune = dimensionDto.IdDimensiune,
                                            LungimeDto = dimensionDto.PdDimensiune!.Lungime,
                                            LatimeDto = dimensionDto.PdDimensiune!.Latime,
                                            RecomandarePat = dimensionDto.PdDimensiune!.RecomandarePat,
                                            JustAdded = false,
                                        })).Distinct()
                                .ToList(),
                            
                            SelectedManopere = asoc
                                .Where(manopera => manopera.IdManopera != null)
                                .Select(man => new 
                                {
                                    man.Manopera!.IdManopera,
                                    NumeManopera = currency == "RON" ? man.Manopera.NumeManoperaJson!.NumeRomana : man.Manopera.NumeManoperaJson!.NumeEngleza,
                                    man.Manopera.MaterialFolosit,
                                    man.Manopera.InaltimeMaxima,
                                    TipInel = man.Manopera.InelPrindereLaManopera,
                                    TipGalerie = man.Manopera.TipGalerieLaManopera,
                                    TipLinie = man.Manopera.TipLinieLaManopera
                                })
                                .Distinct()
                                .Select(man => new StandardManopereOnSet
                                {
                                    IdManopera = man.IdManopera,
                                    NumeManopera = man.NumeManopera,
                                    MetruTotalFolosit = man.MaterialFolosit,
                                    InaltimeMaxima = man.InaltimeMaxima,
                                    TipInel = new TipIneleDto
                                    {
                                        NumeTipInel = currency == "RON" ? man.TipInel!.CuloareInelJson.CuloareRomana :man.TipInel!.CuloareInelJson.CuloareEngleza ,
                                        CaleRelativa = man.TipInel.CaleRelativa,
                                        PresignedUrl = "empty"
                                    },
                                    TipGalerie = new TipRejansaDto
                                    {
                                        NumeTipRejansa =  currency == "RON" ? man.TipGalerie.NumeTipGalerieJson.NumeRomana : man.TipGalerie.NumeTipGalerieJson.NumeEngleza,
                                        PretTipRejansa = 0,
                                        IncretireRejansa =  man.TipGalerie.IncretireRejansa,
                                        CaleRelativa =  man.TipGalerie.CaleRelativa,
                                        PresignedUrl = "empty",
                                        SePrindeCuInele =  man.TipGalerie.SePrindeCuInele
                                    },
                                    TipLinie = new TipLinieDto
                                    {
                                        NumeTipCusaturaColt = currency == "RON" ?  man.TipLinie.NumeTipLinieJson.NumeRomana : man.TipLinie.NumeTipLinieJson.NumeEngleza,
                                        PretTipCusaturaColt = 0,
                                        CaleRelativa = man.TipLinie.CaleRelativa,
                                        PresignedUrl = "empty"
                                    }
                                }).ToList(),
                            
                        }).ToList(),
                    ReviewsSet = set.ReviewPeSet!
                        .Select(reviews => new ReviewsDto
                        {
                            NumarSteleDto = reviews.NumarStele,
                            TextRecenzie = reviews.TextRecenzie,
                            NumeClient = reviews.Cont.Nume,
                            PrenumeClient = reviews.Cont.Prenume,
                            UsernameContClient = reviews.Cont.Username!
                        }).ToList(),
                    ReviewsGeneral = new ReviewsInfo
                    {
                        TotalReviews = set.ReviewPeSet!.Count,
                        AverageRating = set.ReviewPeSet.Count > 0
                            ? Math.Round(set.ReviewPeSet.Average(avg => avg.NumarStele), 1)
                            : 0.0,
                        FiveStarsReviews = set.ReviewPeSet.Count(fiveStars => fiveStars.NumarStele == 5),
                        FourStarsReviews = set.ReviewPeSet.Count(fiveStars => fiveStars.NumarStele == 4),
                        ThreeStarsReviews = set.ReviewPeSet.Count(fiveStars => fiveStars.NumarStele == 3),
                        TwoStarsReviews = set.ReviewPeSet.Count(fiveStars => fiveStars.NumarStele == 2),
                        OneStarReviews = set.ReviewPeSet.Count(fiveStars => fiveStars.NumarStele == 1)
                    }
                }).AsSplitQuery()
                .FirstAsync();

            foreach (var produs in setPageInfo.ProdusePeSet)
            {
                foreach (var culoare in produs.SelectedColors)
                {
                    if (culoare.ImaginiProdusDto.IsNullOrEmpty()) continue;
                    foreach (var imagine in culoare.ImaginiProdusDto!)
                    {
                        imagine.PresignedUrl =
                            await _bucketAcces.GenerateUrl(imagine.CaleImagineDto, imagine.FisierInBucketDto);
                    }
                }

                if (produs.TipulProdusuluiDto is not ("perdea" or "draperie"))
                {
                    continue;
                }
                
                foreach (var manopera in produs.SelectedManopere)
                {
                    if (manopera.TipInel?.CaleRelativa != null)
                    {
                        manopera.TipInel.PresignedUrl = await _bucketAcces
                            .GenerateUrl(manopera.TipInel.CaleRelativa, "inele_prindere");
                    }

                    if (manopera.TipGalerie.CaleRelativa != null)
                    {
                        manopera.TipGalerie.PresignedUrl = await _bucketAcces
                            .GenerateUrl(manopera.TipGalerie.CaleRelativa, "tipuri_galerie");
                    }
                    
                    if (manopera.TipLinie.CaleRelativa != null)
                    {
                        manopera.TipLinie.PresignedUrl = await _bucketAcces
                            .GenerateUrl(manopera.TipLinie.CaleRelativa, "tipuri_galerie");
                    }
                }
            }
           

            return new KeyValuePair<int, SetPage?>(1, setPageInfo);
        }
        catch (Exception e)
        {
            if (e.InnerException is ArgumentNullException)
            {
                _logger.LogError("Set does not exist anymore");
                return new KeyValuePair<int, SetPage?>(1,null);
            }
            _logger.LogError(e.Message);
            _logger.LogError(e.StackTrace);
            _logger.LogError($"Error name : {e.GetType().Name}");
            _logger.LogError("General error occured");
            return new KeyValuePair<int, SetPage?>(0,null);
                
        }
    }
}