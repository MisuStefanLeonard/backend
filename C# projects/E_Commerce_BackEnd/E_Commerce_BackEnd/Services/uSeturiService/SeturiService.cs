using System.Text.RegularExpressions;
using AutoMapper;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.BulkOperationsDto;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductOptionsDto;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.Options;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.ProductPage.OptionsForCurtain;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.SeturiDtos;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.SeturiDtos.DtoForProductOptions;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.SeturiDtos.User;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.SeturiDtos.User.SetPage;
using E_Commerce_BackEnd.Models.ProductRelatedModels;
using E_Commerce_BackEnd.Services.Helpers.AWS_Secret.AWSBucket_CRUD;
using E_Commerce_BackEnd.Services.Helpers.UserHelpers;
using E_Commerce_BackEnd.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;

namespace E_Commerce_BackEnd.Services.uSeturiService;

public partial class SeturiService : ISeturiService
{
    [GeneratedRegex("^[a-z-0-9A-Z]+$")]
    private static partial Regex ValidateQueryParams();
    [GeneratedRegex("^[0-9]+$")]
    private static partial Regex ValidateNumberesOnly();
    private const int PageSize = 10;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<SeturiService> _logger;
    private readonly IBucketAcces _bucketAcces;
    private readonly IMemoryCache _cache;

    public SeturiService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<SeturiService> logger, IBucketAcces bucketAcces, IMemoryCache cache)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _bucketAcces = bucketAcces;
        _cache = cache;
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

    public async Task<SetModificationDto?> GetSetPage(int idSet)
    {
        var productWithSetsRepository = _unitOfWork.Repository<AsociereSeturi>();
        var seturiRepository = _unitOfWork.Repository<Seturi>();
        var productsRepository = _unitOfWork.Repository<Produse>();
        var colorsWithProductRepository = _unitOfWork.Repository<ProduseCuCulori>();
        var dimensionsWithProductRepository = _unitOfWork.Repository<ProduseCuDimensiuni>();
        
        // Retrieve the set
        var currentSet = await seturiRepository
            .FindQueryable(s => s.IdSet == idSet)
            .FirstOrDefaultAsync();

        if (currentSet is null)
        {
            return null;
        }
        
        _logger.LogInformation($"SET ID {idSet}");
        
        // Retrieve all products associated with the set
        var listOfProductsOnSets = await productWithSetsRepository
            .FindQueryable(asoc => asoc.IdSet == idSet)
            .Include(c => c.AsCuloare)
                .ThenInclude(cc => cc!.CodCuloare)
            .Include(d => d.AsDimensiune)
            .AsSplitQuery()
            .ToListAsync();
        

        var listOfProductVariaties = new List<ProductForSetDto>();
        if (listOfProductsOnSets.IsNullOrEmpty())
        {
            _logger.LogError("EMPTY LIST OF PRODUCTS");
        }
        foreach (var it in listOfProductsOnSets)
        {
            _logger.LogError($"id produs {it.IdProdus} : id culoare {it.IdCuloare}");
        }
        foreach (var setIterator in listOfProductsOnSets)
        {
            var currentProductOnSet = await productsRepository
                .FindQueryable(p => p.IdProdus == setIterator.IdProdus)
                .FirstOrDefaultAsync();

            if (currentProductOnSet is null)
            {
                return null;
            }

            // Check if the product is already in the list
            var existingProduct = listOfProductVariaties.FirstOrDefault(p => p.CodProdusDto == currentProductOnSet.CodProdus);

            // If the product does not exist, create a new entry for it
            if (existingProduct == null)
            {
                var mappedProduct = _mapper.Map<ProductForSetDto>(currentProductOnSet);
                _logger.LogInformation($"Mapped product -> {mappedProduct.CodProdusDto}");
                // Retrieve all colors linked with the product
                var allColorsForProduct = await colorsWithProductRepository
                    .FindQueryable(pc => pc.IdProdus == currentProductOnSet.IdProdus)
                    .Include(c => c.Culoare)
                    .ThenInclude(cc => cc.CodCuloare)
                    .ToListAsync();

                // Retrieve all dimensions linked with the product
                var allDimensionsForProduct = await dimensionsWithProductRepository
                    .FindQueryable(pd => pd.IdProdus == currentProductOnSet.IdProdus)
                    .Include(d => d.PdDimensiune)
                    .ToListAsync();

                // Prepare to track selected items (already linked to the set)
                foreach (var colorProductLink in allColorsForProduct)
                {
                    var mappedColorToDto = _mapper.Map<CuloriDto>(colorProductLink.Culoare);
                    _logger.LogInformation($"mapped color -> {mappedColorToDto.NumeCuloareDto}");
                    // Check if this color is already linked in the set
                    var isColorSelectedInSet = listOfProductsOnSets.Any(asoc => 
                        asoc.IdProdus == currentProductOnSet.IdProdus &&
                        asoc.IdCuloare == colorProductLink.IdCuloare);
                    
                    mappedColorToDto.JustAdded = isColorSelectedInSet;

                    // Add color to product options
                    mappedProduct.ProductOptions.ColorsVariaties.Add(mappedColorToDto);
                }

                foreach (var dimensionProductLink in allDimensionsForProduct)
                {
                    var mappedDimensionToDto = _mapper.Map<DimensiuniDto>(dimensionProductLink.PdDimensiune, opt => 
                    {
                        opt.Items["Product"] = currentProductOnSet;  // Pass the product in the mapping context
                    });

                    // Check if this dimension is already linked in the set
                    var isDimensionSelectedInSet = listOfProductsOnSets.Any(asoc => 
                        asoc.IdProdus == currentProductOnSet.IdProdus &&
                        asoc.IdDimensiune == dimensionProductLink.IdDimensiune);
                    
                    mappedDimensionToDto.JustAdded = isDimensionSelectedInSet;

                    // Add dimension to product options
                    mappedProduct.ProductOptions.DimensionVariaties.Add(mappedDimensionToDto);
                }

                // Add the product to the list of variations
                listOfProductVariaties.Add(mappedProduct);
            }
            else
            {
                // If the product already exists, merge the color and dimension variations
                var allColorsForProduct = await colorsWithProductRepository
                    .FindQueryable(pc => pc.IdProdus == currentProductOnSet.IdProdus)
                    .Include(c => c.Culoare)
                    .ThenInclude(cc => cc.CodCuloare)
                    .ToListAsync();

                foreach (var colorProductLink in allColorsForProduct)
                {
                    var mappedColorToDto = _mapper.Map<CuloriDto>(colorProductLink.Culoare);

                    var isColorSelectedInSet = listOfProductsOnSets.Any(asoc => 
                        asoc.IdProdus == currentProductOnSet.IdProdus &&
                        asoc.IdCuloare == colorProductLink.IdCuloare);

                    mappedColorToDto.JustAdded = isColorSelectedInSet;

                    // Avoid duplicate color entries
                    if (existingProduct.ProductOptions.ColorsVariaties.All(c => c.CodCuloareDto != mappedColorToDto.CodCuloareDto))
                    {
                        existingProduct.ProductOptions.ColorsVariaties.Add(mappedColorToDto);
                    }
                }

                var allDimensionsForProduct = await dimensionsWithProductRepository
                    .FindQueryable(pd => pd.IdProdus == currentProductOnSet.IdProdus)
                    .Include(d => d.PdDimensiune)
                    .ToListAsync();

                foreach (var dimensionProductLink in allDimensionsForProduct)
                {
                    var mappedDimensionToDto = _mapper.Map<DimensiuniDto>(dimensionProductLink.PdDimensiune, opt => 
                    {
                        opt.Items["Product"] = currentProductOnSet;
                    });

                    var isDimensionSelectedInSet = listOfProductsOnSets.Any(asoc => 
                        asoc.IdProdus == currentProductOnSet.IdProdus &&
                        asoc.IdDimensiune == dimensionProductLink.IdDimensiune);

                    mappedDimensionToDto.JustAdded = isDimensionSelectedInSet;

                    // Avoid duplicate dimension entries
                    if (!existingProduct.ProductOptions.DimensionVariaties.Any(d => 
                        d.LungimeDto == mappedDimensionToDto.LungimeDto && 
                        d.LatimeDto == mappedDimensionToDto.LatimeDto))
                    {
                        existingProduct.ProductOptions.DimensionVariaties.Add(mappedDimensionToDto);
                    }
                }
            }
        }

        // Create the SetModificationDto to return
        var setModificationDto = new SetModificationDto
        {
            ProductsOnSet = listOfProductVariaties,
            NumeSetDto = currentSet.NumeSet,
            DescriereSetDto = currentSet.DescriereSet,
            PretSetDto = currentSet.PretSet,
            PretRedusSetDto = currentSet.PretRedusSet
        };

        foreach (var productOnSet in setModificationDto.ProductsOnSet)
        {
            _logger.LogInformation($"{productOnSet.CodProdusDto} ");
            foreach (var selColor in productOnSet.SelectedColors)
            {
                _logger.LogInformation($"Selected color {selColor}");
            }
            foreach (var seldim in productOnSet.SelectedDimensions)
            {
                _logger.LogInformation($"Selected dimension {seldim}");
            }
        }

        return setModificationDto;
    }

    public async Task<IList<SelectProducts>> GetProductCodes()
    {
        var productsRepository = _unitOfWork.Repository<Produse>();
        
        IList<SelectProducts> listOfProductCodes = await productsRepository
            .GetSimpleQueryable()
            .GroupBy(p => p.TipulProdusului)
            .Select(group => new SelectProducts
            {
                Title = group.Key.ToUpper(),  // Mapping to TipulProdusuluiDto
                Children = group.Select(p => new ProdusDto
                {
                    Id = p.CodProdus,
                    Title = p.NumeProdus!.ToUpper()// Mapping to CodProdus
                }).ToList()  // Projecting to the ProduseDto list
            })
            .ToListAsync();

        return listOfProductCodes;
    }

    public async Task<IList<string>> GetSetNames()
    {
        var seturiRepository = _unitOfWork.Repository<Seturi>();

        var listOfSeturiNames = await seturiRepository
            .GetSimpleQueryable()
            .Select(s => s.NumeSet)
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
                    _logger.LogInformation("Inel already has been deleted (bulk operation delete)");
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

    public async Task<int> ModifyOrUpdateSet(SetModificationDto modifiedSet, int idSet,bool isAdding)
    {
        IDbContextTransaction? updateOrAddTransaction = null;
        try
        {
            updateOrAddTransaction = await _unitOfWork.BeginTransactionAsync();
            var seturiRepository = _unitOfWork.Repository<Seturi>();
            var colorsRepository = _unitOfWork.Repository<Culori>();
            var colorCodesRepository = _unitOfWork.Repository<CodCulori>();
            var dimensionsRepository = _unitOfWork.Repository<Dimensiuni>();
            var asociereSeturiRepository = _unitOfWork.Repository<AsociereSeturi>();

            if (!isAdding)
            {
                var listOfNewOptions = new List<AsociereSeturi>();
                var listOfOptionsToRemove = new List<AsociereSeturi>();
                
                var oldOptions = await asociereSeturiRepository
                    .FindQueryable(asoc => asoc.IdSet == idSet)
                    .ToListAsync();
                
                foreach (var productOnSet in modifiedSet.ProductsOnSet)
                {
                    // blue-08 , grey-03
                    var selectedColors = productOnSet.SelectedColors;
                    // 200x190 , 200x140
                    var selectedDimensions = productOnSet.SelectedDimensions;
                    if (selectedColors.Count != 0 && selectedDimensions.Count != 0)
                    {
                        foreach (var color in selectedColors)
                        {
                            var splitColorByCode = color.Split('-');
                            var colorName = splitColorByCode[0];
                            var colorCode = splitColorByCode[1];
                      

                            var colorCodeInDb = await colorCodesRepository
                                .FindQueryable(cc => cc.CodCuloare == colorCode)
                                .FirstAsync();
                        
                            var colorInDb = await colorsRepository
                                .FindQueryable(c => c.NumeCuloare == colorName &&
                                                    c.IdCodCuloare == colorCodeInDb.IdCodCuloare)
                                .FirstAsync();
                       
                            foreach (var selectedDimension in selectedDimensions)
                            {
                                // lungime190xlatime200-recomandarePat140x200
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
                    }else if (selectedColors.Count != 0 && selectedDimensions.Count == 0)
                    {
                        foreach (var color in selectedColors)
                        {
                            var splitColorByCode = color.Split('-');
                            var colorName = splitColorByCode[0];
                            var colorCode = splitColorByCode[1];

                            var colorCodeInDb = await colorCodesRepository
                                .FindQueryable(cc => cc.CodCuloare == colorCode)
                                .FirstAsync();

                            var colorInDb = await colorsRepository
                                .FindQueryable(c => c.NumeCuloare == colorName &&
                                                    c.IdCodCuloare == colorCodeInDb.IdCodCuloare)
                                .FirstAsync();

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
                    else if (selectedColors.Count == 0 && selectedDimensions.Count != 0)
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
                                                        asoc.IdCuloare == null &&
                                                        asoc.IdDimensiune == dimensionInDb.IdDimensiune);

                            if (isVariationInDb is null)
                            {
                                var newProductOptionOnSet = new AsociereSeturi
                                {
                                    IdProdus = productOnSet.IdProdusDto,
                                    IdSet = idSet,
                                    IdCuloare = null,
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
                        _logger.LogInformation($"No colors or dimensions were chosen for the product {productOnSet.CodProdusDto}");
                    }
                    
                }
                    
                listOfOptionsToRemove = oldOptions;

                // Perform the database operations
                if (listOfNewOptions.Count > 0)
                {
                    await asociereSeturiRepository.AddRangeAsync(listOfNewOptions);
                }

                if (listOfOptionsToRemove.Count > 0)
                {
                    await asociereSeturiRepository.DeleteRangeAsync(listOfOptionsToRemove);
                }
               
                await _unitOfWork.CommitTransactionAsync(updateOrAddTransaction);
                _logger.LogInformation("Set updated succesfully");

                return 1;
            }
            // else we create a new set.
            
            
            var optionsToBeAdded = new List<AsociereSeturi>();
            var newSet = new Seturi
            {
                NumeSet = modifiedSet.NumeSetDto,
                DescriereSet = modifiedSet.DescriereSetDto,
                PretSet = modifiedSet.PretSetDto,
                PretRedusSet = modifiedSet.PretRedusSetDto,
                SetActivInMagazin = false,
                IsDeleted = false
            };

            await seturiRepository.AddAsync(newSet);
            await _unitOfWork.CommitAsync();


            foreach (var productOnSet in modifiedSet.ProductsOnSet)
            {
                // blue-08 , grey-03
                var selectedColors = productOnSet.SelectedColors;
                // 200x190 , 200x140
                var selectedDimensions = productOnSet.SelectedDimensions;
                // de veriificat cazurile -> fara culori / cu dimesiuno
                // -> cu culori / fara dimensiuni
                // -> cu culori si dimensiuni

                if (selectedColors.Count != 0 && selectedDimensions.Count != 0)
                {
                    foreach (var color in selectedColors)
                    {
                        var splitColorByCode = color.Split('-');
                        var colorName = splitColorByCode[0];
                        var colorCode = splitColorByCode[1];
                        
                        var colorCodeInDb = await colorCodesRepository
                            .FindQueryable(cc => cc.CodCuloare == colorCode)
                            .FirstAsync();

                        var colorInDb = await colorsRepository
                            .FindQueryable(c => c.NumeCuloare == colorName &&
                                                c.IdCodCuloare == colorCodeInDb.IdCodCuloare)
                            .FirstAsync();

                        if (selectedDimensions.Count == 0)
                        {
                            var newOptionOnSet = new AsociereSeturi
                            {
                                IdProdus = productOnSet.IdProdusDto,
                                IdSet = newSet.IdSet,
                                IdCuloare = colorInDb.IdCuloare,
                                IdDimensiune = null
                            };

                            optionsToBeAdded.Add(newOptionOnSet);
                        }

                        foreach (var selectedDimension in selectedDimensions)
                        {
                            // lungime190xlatime200-recomandarePat140x200
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

                            var newOptionOnSet = new AsociereSeturi
                            {
                                IdProdus = productOnSet.IdProdusDto,
                                IdSet = newSet.IdSet,
                                IdCuloare = colorInDb.IdCuloare,
                                IdDimensiune = dimensionInDb.IdDimensiune
                            };

                            optionsToBeAdded.Add(newOptionOnSet);
                        }
                    }
                }else if (selectedColors.Count != 0 && selectedDimensions.Count == 0)
                {
                    foreach (var color in selectedColors)
                    {
                        var splitColorByCode = color.Split('-');
                        var colorName = splitColorByCode[0];
                        var colorCode = splitColorByCode[1];
                        
                        var colorCodeInDb = await colorCodesRepository
                            .FindQueryable(cc => cc.CodCuloare == colorCode)
                            .FirstAsync();

                        var colorInDb = await colorsRepository
                            .FindQueryable(c => c.NumeCuloare == colorName &&
                                                c.IdCodCuloare == colorCodeInDb.IdCodCuloare)
                            .FirstAsync();

                       
                        var newOptionOnSet = new AsociereSeturi
                        {
                            IdProdus = productOnSet.IdProdusDto,
                            IdSet = newSet.IdSet,
                            IdCuloare = colorInDb.IdCuloare,
                            IdDimensiune = null
                        };

                        optionsToBeAdded.Add(newOptionOnSet);
                        
                    }
                }
                else if(selectedDimensions.Count != 0 && selectedColors.Count == 0)
                {
                    foreach (var selectedDimension in selectedDimensions)
                    {
                        // lungime190xlatime200-recomandarePat140x200
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

                        var newOptionOnSet = new AsociereSeturi
                        {
                            IdProdus = productOnSet.IdProdusDto,
                            IdSet = newSet.IdSet,
                            IdCuloare = null,
                            IdDimensiune = dimensionInDb.IdDimensiune
                        };

                        optionsToBeAdded.Add(newOptionOnSet);
                    }
                }
                else
                {
                    _logger.LogInformation($"No colors or dimension was choosen for the product {productOnSet.CodProdusDto}");
                }
                
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
            Console.WriteLine(e.Message);
            return -1;
        }
    }

    public async Task<ProductForSetDto?> GetProductDataForSetAdd(string codProdus)
    {
        var productsRepository = _unitOfWork.Repository<Produse>();
        var dimensionsOnProductsRepository = _unitOfWork.Repository<ProduseCuDimensiuni>();
        var colorsOnProductsRepository = _unitOfWork.Repository<ProduseCuCulori>();
        
        var productToGetData = await productsRepository
            .FindQueryable(p => p.CodProdus == codProdus.ToUpper())
            .FirstAsync();

        var productMapped = _mapper.Map<ProductForSetDto>(productToGetData);
        
        var dimensionsLinkedWithCurrentProduct = await dimensionsOnProductsRepository
            .FindQueryable(pd => pd.IdProdus == productToGetData.IdProdus)
            .Include(d => d.PdDimensiune)
            .ToListAsync();

        if (dimensionsLinkedWithCurrentProduct.Count != 0)
        {
            foreach (var dimensionLink in dimensionsLinkedWithCurrentProduct)
            {
                var mappedDimensionToDto = _mapper.Map<DimensiuniDto>(dimensionLink.PdDimensiune, opt => 
                {
                    opt.Items["Product"] = productToGetData;  
                });
                
                productMapped.ProductOptions.DimensionVariaties.Add(mappedDimensionToDto);
            }
        }
        var colorsLinkedWithTheProduct = await colorsOnProductsRepository
            .FindQueryable(pc => pc.IdProdus == productToGetData.IdProdus)
            .Include(c => c.Culoare)
                .ThenInclude(cc => cc.CodCuloare)
            .ToListAsync();

        if (colorsLinkedWithTheProduct.Count != 0)
        {
            foreach (var colorLink in colorsLinkedWithTheProduct)
            {
                var mappedColorToDto = _mapper.Map<CuloriDto>(colorLink.Culoare, opt => 
                {
                    opt.Items["Product"] = productToGetData;  
                });
                
                productMapped.ProductOptions.ColorsVariaties.Add(mappedColorToDto);
            }
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
            .Where(set => productName.IsNullOrEmpty() || set.SAsociereSeturi!
                .Any(product => product.Produs.NumeProdus!.ToLower().Contains(productName!.ToLower())))
            .Where(set => !set.IsDeleted && set.SetActivInMagazin)
            .Where(set => productTypes.IsNullOrEmpty() || set.SAsociereSeturi!
                .Any(product => productTypes!.Contains(product.Produs.TipulProdusului)))
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
            .OrderBy(set => set.NumeSet)
            .Skip((pageNumber ?? 0) * PageSize)
            .Take(PageSize)
            .Select(set => new SetsListingForUsers
            {
                EncodedIdSet  = set.EncodedIdSet,
                NumeSetDto = set.NumeSet,
                PretSetDto =
                    currency == "EUR" ? UserHelpers.ConvertCurrency("RON", "EUR", set.PretSet, 0) : set.PretSet,
                PretRedusSetDto = currency == "EUR"
                    ? UserHelpers.ConvertCurrency("RON", "EUR", set.PretRedusSet, 0)
                    : set.PretRedusSet,
                SetProductsDto = set.SAsociereSeturi!
                    .Select(asoc => new ProductsInSet
                    {
                        NumeProdusDto = asoc.Produs.NumeProdus!,
                        CuloriProdusDto = asoc.Produs.PProduseCuCulori!
                            .Select(color => new ColorsWithImages
                            {
                                NumeCuloareDto = color.Culoare.NumeCuloare,
                                ImaginiProdusDto = color.ImagProduseCuCulori!.Select(imag => new ImagesDtoForUsers
                                {
                                    CaleImagineDto = imag.CaleImagine!,
                                    FisierInBucketDto = imag.FisierInBucket,
                                    PresignedUrl = null
                                }).ToList()
                            }).ToList()
                    }).ToList()
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
            
            var ringTypesDto = await _cache.GetOrCreateAsync($"ringTypes_{currency}", async entry =>
                {
                    var ringTypesRepository = _unitOfWork.Repository<InelePrindere>();
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);

                    // If cache does not exist, run the retrieval and mapping logic
                    var rings = await ringTypesRepository.GetAllAsync();
                    var mappedRings = rings.IsNullOrEmpty() ? new List<TipIneleDto>() : _mapper.Map<IList<TipIneleDto>>(rings);

                    // Generate presigned URLs for each ring type
                    var ringTasks = mappedRings.Select(async ringType =>
                    {
                        ringType.PresignedUrl = ringType.CaleRelativa != null
                            ? await _bucketAcces.GenerateUrl(ringType.CaleRelativa, "inele_prindere")
                            : null;
                    }).ToList();

                    await Task.WhenAll(ringTasks);

                    return mappedRings;
                });

            var rejanseTypesDto = await _cache.GetOrCreateAsync($"rejanse_{currency}", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
                var rejansaRepository = _unitOfWork.Repository<TipuriGalerie>();
                var rejanse = await rejansaRepository.GetAllAsync();
                var mappedRejanse = rejanse.IsNullOrEmpty() ? new List<TipRejansaDto>() : _mapper.Map<IList<TipRejansaDto>>(rejanse);

                var rejansaTasks = mappedRejanse.Select(async rejansa =>
                {
                    rejansa.PresignedUrl = rejansa.CaleRelativa != null
                        ? await _bucketAcces.GenerateUrl(rejansa.CaleRelativa, "tipuri_galerie")
                        : null;
                    rejansa.PretTipRejansa = currency == "EUR" 
                        ? UserHelpers.ConvertCurrency("RON", "EUR", rejansa.PretTipRejansa, 0)
                        : rejansa.PretTipRejansa;
                }).ToList();

                await Task.WhenAll(rejansaTasks);

                return mappedRejanse;
            });

            var liningTypesDto = await _cache.GetOrCreateAsync($"liningTypes_{currency}", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
                var liningTypesRepository = _unitOfWork.Repository<TipuriLinie>();
                var linings = await liningTypesRepository.GetAllAsync();
                var mappedLinings = linings.IsNullOrEmpty() ? new List<TipLinieDto>() : _mapper.Map<IList<TipLinieDto>>(linings);

                var liningTasks = mappedLinings.Select(async lineType =>
                {
                    lineType.PresignedUrl = lineType.CaleRelativa != null
                        ? await _bucketAcces.GenerateUrl(lineType.CaleRelativa, "tipuri_linie")
                        : null;
                    lineType.PretTipCusaturaColt = currency == "EUR" 
                        ? UserHelpers.ConvertCurrency("RON", "EUR", lineType.PretTipCusaturaColt, 0)
                        : lineType.PretTipCusaturaColt;
                }).ToList();

                await Task.WhenAll(liningTasks);

                return mappedLinings;
            });
            var setPageInfo = await _unitOfWork.Repository<Seturi>()
                .GetSimpleQueryable()
                .Where(set => set.NumeSet.ToLower() == setName.ToLower() && set.IdSet == setId
                              && set.SetActivInMagazin
                              && !set.IsDeleted)
                .Select(set => new SetPage
                {
                    NumeSetDto = set.NumeSet,
                    PretSetDto = currency == "EUR" ? set.PretSet * (decimal)0.2 : set.PretSet,
                    PretRedusSetDto =  currency == "EUR" ? set.PretRedusSet * (decimal)0.2 : set.PretRedusSet,
                    DescriereSetDto = set.DescriereSet,
                    ProdusePeSet = set.SAsociereSeturi!
                        .Select(asoc => new ProductOnSet
                        {
                            CodProdusDto = asoc.Produs.CodProdus,
                            DescriereDto = asoc.Produs.Descriere,
                            NumeProdusDto = asoc.Produs.NumeProdus,
                            CompozitieDto = asoc.Produs.Compozitie,
                            TvaDto = asoc.Produs.Tva,
                            IngrijireDto = asoc.Produs.Ingrijire,
                            FataReversibilaDto = asoc.Produs.FataReversibila,
                            TipulProdusuluiDto = asoc.Produs.TipulProdusului,
                            NumeProducatorDto = asoc.Produs.Producator!.NumeProducator,
                            TipuriInele = asoc.Produs.TipulProdusului == "perdea" || asoc.Produs.TipulProdusului == "draperie" ?
                                ringTypesDto : null,
                            TipuriLinie = asoc.Produs.TipulProdusului == "perdea" || asoc.Produs.TipulProdusului == "draperie" ?
                                liningTypesDto : null,
                            TipuriRejansa = asoc.Produs.TipulProdusului == "perdea" || asoc.Produs.TipulProdusului == "draperie" ?
                                rejanseTypesDto : null,
                            SelectedColors = asoc.Produs.PProduseCuCulori!
                                .AsQueryable()
                                .Where(color => asoc.IdCuloare == null || asoc.IdCuloare == color.IdCuloare)
                                .Select(colorDto => new CuloriDto
                                {
                                    NumeCuloareDto = colorDto.Culoare.NumeCuloare,
                                    CodCuloareDto = colorDto.Culoare.CodCuloare.CodCuloare!,
                                    JustAdded = false,
                                    ImaginiProdusDto = colorDto.ImagProduseCuCulori!
                                        .Select(imag => new ImagesDto
                                        {
                                            CaleImagineDto = imag.CaleImagine!,
                                            FisierInBucketDto = imag.FisierInBucket,
                                            PresignedUrl = null,
                                            JustAdded = false,
                                        }).ToList()
                                }).ToList(),
                            SelectedDimensions = asoc.Produs.PProduseCuDimensiuni!
                                .AsQueryable()
                                .Where(dimension => asoc.IdDimensiune == null 
                                                    || dimension.IdDimensiune == null 
                                                    || asoc.IdDimensiune == dimension.IdDimensiune)
                                .Select(dimensionDto => new DimensiuniDto
                                {
                                    LungimeDto = dimensionDto.PdDimensiune!.Lungime,
                                    LatimeDto = dimensionDto.PdDimensiune!.Latime,
                                    RecomandarePat = dimensionDto.PdDimensiune!.RecomandarePat,
                                    JustAdded = false,
                                }).ToList()
                        }).ToList()
                }).FirstAsync();

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
            _logger.LogError("General error occured");
            return new KeyValuePair<int, SetPage?>(0,null);
                
        }
    }
}