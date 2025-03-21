using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.RegularExpressions;
using AutoMapper;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.BulkOperationsDto;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductOptionsDto;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.Options;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.ProductPage;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.ProductPage.OptionsForCurtain;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.ReviewsDto;
using E_Commerce_BackEnd.Models.ProductRelatedModels;
using E_Commerce_BackEnd.Models.ProductRelatedModels.JSON_Models;
using E_Commerce_BackEnd.Services.Helpers.AWS_Secret.AWSBucket_CRUD;
using E_Commerce_BackEnd.Services.Helpers.UserHelpers;
using E_Commerce_BackEnd.UnitOfWork;
using Google.Analytics.Data.V1Beta;
using Google.Apis.Auth.OAuth2;
using Grpc.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;

namespace E_Commerce_BackEnd.Services.uProductsService;

public partial class ProductService : IProductService
{
    [GeneratedRegex(@"^[a-zA-Z0-9!@#$%^&*()_+\-\.]+$")]
    private static partial Regex ValidateQueryParams();

    [GeneratedRegex("^[0-9]+$")]
    private static partial Regex ValidateNumberesOnly();
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<Produse> _logger;
    private const int PageSize = 15;
    private readonly IBucketAcces _bucketAcces;
    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;


    public ProductService(
        IUnitOfWork unitOfWork, 
        ILogger<Produse> logger, 
         IBucketAcces bucketAcces, IMapper mapper, IMemoryCache cache)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _bucketAcces = bucketAcces;
        _mapper = mapper;
        _cache = cache;
    }
    public async Task<int> DeleteProduct(string codProdus)
    {
        IDbContextTransaction? transaction = null;
        try
        {
            transaction = await _unitOfWork.BeginTransactionAsync();
            var repository = _unitOfWork.Repository<Produse>();
            
            var produs = await repository
                .FindQueryable(p => p.CodProdus == codProdus)
                .Include(p => p.ComenziProduse)
                .FirstOrDefaultAsync();
            
            if (produs is null)
            {
                return -1;
            }

            if (produs.IsLocked)
            {
                return -3; // Product is being bought !!, cannot modify
            }


            var areOrdersOnProduct = produs.ComenziProduse.IsNullOrEmpty();

            if (areOrdersOnProduct)
            {
                await repository.DeleteAsync(produs);
            }
            else
            {
                produs.IsDeleted = true;
                produs.ActivInMagazin = false;
                await repository.UpdateAsync(produs);
            }
            
            await _unitOfWork.CommitTransactionAsync(transaction);
           
            return 1;
            
        }
        catch (Exception e)
        {
            if (transaction != null)
            {
                await _unitOfWork.RollBackTransactionAsync(transaction);
            }
            _logger.LogError("Errors when trying to delete the product");
            return -2;
        }
       

    }

    /// <summary>
    /// The method to update a product using the interface
    /// </summary>
    /// <param name="modifiedProduct">The Dto with the updated product data</param>
    /// <param name="images">the images uploaded</param>
    /// <returns>
    ///     And integer array containing the succes codes for each operation
    ///     to display on the frontend and see where it was an error
    /// </returns>
    /// succesCode[0] = product characteristics
    /// succesCode[1] = product types
    /// succesCode[2] = product colors
    /// succesCode[3] = product images
    /// succesCode[4] = product dimensions
   public async Task<int[]> UpdateProduct(ProduseDtoForAdminModification modifiedProduct, IFormFileCollection images)
    {
        var successCodes = new int[5];
    
        IDbContextTransaction? modifyingTransaction = null;
        try
        {
            modifyingTransaction = await _unitOfWork.BeginTransactionAsync();
            var productRepository = _unitOfWork.Repository<Produse>();
            var manufacturersRepository = _unitOfWork.Repository<Producatori>();
            
            // Find the product to be modified or add if it doesn't exist
            var productToBeModified = await productRepository
                .FindQueryable(p => p.CodProdus == modifiedProduct.OldCodProdusDto)
                .FirstOrDefaultAsync();
            
            var isAddingFlag = productToBeModified == null;
            var numeProducator = modifiedProduct.NumeProducatorDto;
            Producatori? producator = null;
            
            // Handle manufacturer existence or creation
            if (!numeProducator.IsNullOrEmpty())
            {
                producator = await manufacturersRepository
                    .FindQueryable(p => p.NumeProducator == numeProducator)
                    .FirstOrDefaultAsync();
            
                if (producator == null)
                {
                    producator = new Producatori { NumeProducator = numeProducator };
                    await manufacturersRepository.AddAsync(producator);
                    await _unitOfWork.CommitAsync();
                }
            }
            
            // If adding a new product
            if (isAddingFlag)
            {
                var newProduct = new Produse
                {
                    CodProdus = modifiedProduct.CodProdusDto!.ToUpper(),
                    NumeProdusJson = new Nume
                    {
                        NumeRomana = modifiedProduct.NumeProdusJsonDto.NumeRomana,
                        NumeEngleza = modifiedProduct.NumeProdusJsonDto.NumeEngleza,
                    },
                    CompozitieJson = new Compozitie
                    {
                        CompozitieRomana = modifiedProduct.CompozitieJsonDto!.CompozitieRomana,
                        CompozitieEngleza = modifiedProduct.CompozitieJsonDto!.CompozitieEngleza
                    },
                    DescriereJson = modifiedProduct.DescriereJsonDto != null ? new Descriere
                    {
                        DescriereRomana = modifiedProduct.DescriereJsonDto.DescriereRomana,
                        DescriereEngleza =  modifiedProduct.DescriereJsonDto.DescriereEngleza
                    } : null,
                    Tva = modifiedProduct.TvaDto,
                    IngrijireJson = modifiedProduct.IngrijireJsonDto != null ? new Ingrijire()
                    {
                        IngrijireRomana = modifiedProduct.IngrijireJsonDto.IngrijireRomana,
                        IngrijireEngleza =  modifiedProduct.IngrijireJsonDto.IngrijireEngleza
                    } : null,
                    FataReversibila = modifiedProduct.FataReversibilaDto,
                    Stoc = modifiedProduct.StocDto,
                    TipulProdusuluiJson = new TipProdus
                    {
                        TipProdusRomana = modifiedProduct.TipulProdusuluiJsonDto!.TipProdusRomana,
                        TipProdusEngleza = modifiedProduct.TipulProdusuluiJsonDto!.TipProdusEngleza
                    },
                    IsDeleted = false,
                    ActivInMagazin = modifiedProduct.ActivInMagazinDto,
                    PretDeBaza = modifiedProduct.PretBazaDto,
                    InaltimeMaxima =modifiedProduct.InaltimeMaximaDto,
                    PretDeBazaRedus = modifiedProduct.PretBazaRedusDto,
                    IdProducator = producator?.IdProducator,
                };
            
                await productRepository.AddAsync(newProduct);
                await _unitOfWork.CommitAsync();
                productToBeModified = newProduct;
                successCodes[0] = 1;
            }
            else
            {
                //Check for product locking
                if (productToBeModified!.IsLocked)
                {
                    throw new DbUpdateException("Product is locked");
                }
                // If updating an existing product
                _logger.LogInformation($" INALTIME{modifiedProduct.InaltimeMaximaDto}");
                _mapper.Map(modifiedProduct, productToBeModified);
                await productRepository.UpdateAsync(productToBeModified);
                successCodes[0] = 1;
            }
            
            var productId = productToBeModified.IdProdus;

    
    
            // Handle product types
            var typesOnProductsRepository = _unitOfWork.Repository<TipuriPeProduse>();
            var productTypesRepository = _unitOfWork.Repository<TipuriProduse>();
            // copii , matlasate , bucatarie
            var modifiedProductTypes = modifiedProduct.TipuriProduseDto;
            
            var allProductTypes = await productTypesRepository.GetSimpleQueryable().ToListAsync();
            var allTypesOnProducts = await typesOnProductsRepository
                .FindQueryable(tp => tp.IdProdus == productId)
                .ToListAsync();

            var newProductTypes = new List<TipuriProduse>();
            var newTypesOnProducts = new List<TipuriPeProduse>();

            // Step 1: Add new product types to the database if they don't exist
            foreach (var newType in modifiedProductTypes)
            {
                newType.CategorieJsonDto.CategorieEngleza = newType.CategorieJsonDto.CategorieEngleza.ToUpper();
                newType.CategorieJsonDto.CategorieRomana = newType.CategorieJsonDto.CategorieRomana.ToUpper();
                var existingType = allProductTypes.FirstOrDefault(t => 
                  t.CategorieJson.CategorieRomana == newType.CategorieJsonDto.CategorieRomana &&  
                 t.CategorieJson.CategorieEngleza == newType.CategorieJsonDto.CategorieEngleza);
                if (existingType != null) continue;
                existingType = new TipuriProduse
                {
                    CategorieJson = newType.CategorieJsonDto,
                };
                newProductTypes.Add(existingType);
            }

            if (newProductTypes.Count > 0)
            {
                await productTypesRepository.AddRangeAsync(newProductTypes);
                await _unitOfWork.CommitAsync();

                // Refresh the product types list after adding new ones
                allProductTypes = await productTypesRepository.GetSimpleQueryable().ToListAsync();
            }

            // Step 2: Prepare the new types-on-products entries
            
            foreach (var newType in modifiedProductTypes)
            {
                
                var associatedType = allProductTypes.First(t => t.CategorieJson.CategorieRomana == newType.CategorieJsonDto.CategorieRomana.ToUpper());
    
                // Check if the type-on-product already exists
                var existingTypeOnProduct = allTypesOnProducts
                    .FirstOrDefault(tp => tp.IdTipProdus == associatedType.IdTipProdus && tp.IdProdus == productId);

                if (existingTypeOnProduct == null)
                {
                    newTypesOnProducts.Add(new TipuriPeProduse
                    {
                        IdTipProdus = associatedType.IdTipProdus,
                        IdProdus = productId
                    });
                }
            }
            
            var oldTypesToDeleteOnProducts = (from typeOnProduct in allTypesOnProducts 
                let existsInModified = modifiedProductTypes
                    .Any(modType => modType.CategorieJsonDto.CategorieRomana 
                                    == typeOnProduct.TppTipProdus.CategorieJson.CategorieRomana) 
                where !existsInModified select typeOnProduct).ToList();


            // Step 3: Add the new types-on-products entries to the database
            if (newTypesOnProducts.Count > 0)
            {
                await typesOnProductsRepository.AddRangeAsync(newTypesOnProducts);
            }
            if (oldTypesToDeleteOnProducts.Count > 0)
            {
                await typesOnProductsRepository.DeleteRangeAsync(oldTypesToDeleteOnProducts);
            }

            successCodes[1] = 1;

            // de facut bulk insert
            // Handle colors and images
            var colorsOnProductsRepository = _unitOfWork.Repository<ProduseCuCulori>();
            var colorRepository = _unitOfWork.Repository<Culori>();
            var colorCodesRepository = _unitOfWork.Repository<CodCulori>();
            var imagesRepository = _unitOfWork.Repository<Imagini>();
            var modifiedProductColors = modifiedProduct.CuloriProdusDto;
            var newColorsLinkedWithProductsIds = new List<int>();
            // old entrys
            var existingColorsOnProducts = await colorsOnProductsRepository
                .FindQueryable(pc => pc.IdProdus == productId)
                .Include(color => color.Culoare)
                .ThenInclude(colorCode => colorCode.CodCuloare)
                .Include(colorImages => colorImages.ImagProduseCuCulori)
                .ToListAsync();
            var oldColorLinkedWithProductEntries = new List<ProduseCuCulori>();
            
            if (!modifiedProductColors.IsNullOrEmpty())
            {
                foreach (var color in modifiedProductColors)
                {
                    
                    var isCurrentUpdatedColorCodeInDb = await colorCodesRepository
                        .FindQueryable(cc => cc.CodCuloare == color.CodCuloareDto)
                        .FirstOrDefaultAsync();
    
                    if (isCurrentUpdatedColorCodeInDb == null)
                    {
                        var newUpdatedColorCode = new CodCulori { CodCuloare = color.CodCuloareDto };
                        await colorCodesRepository.AddAsync(newUpdatedColorCode);
                        await _unitOfWork.CommitAsync();
                        isCurrentUpdatedColorCodeInDb = newUpdatedColorCode;
                    }
                   

                    color.NumeCuloareJsonDto.CuloareRomana = color.NumeCuloareJsonDto.CuloareRomana.ToLower();
                    color.NumeCuloareJsonDto.CuloareEngleza = color.NumeCuloareJsonDto.CuloareEngleza.ToLower();
                    
                    var isCurrentUpdatedColorInDb = await colorRepository
                        .FindQueryable(c => EF.Functions.JsonUnquote(EF.Functions.JsonExtract<string>(c.NumeCuloareJson , "$.culoare_ro")) == color.NumeCuloareJsonDto.CuloareRomana
                                            && EF.Functions.JsonUnquote(EF.Functions.JsonExtract<string>(c.NumeCuloareJson , "$.culoare_en")) == color.NumeCuloareJsonDto.CuloareEngleza 
                                            && c.IdCodCuloare == isCurrentUpdatedColorCodeInDb.IdCodCuloare)
                        .FirstOrDefaultAsync();
    
                    if (isCurrentUpdatedColorInDb == null)
                    {
                        var newUpdatedColor = new Culori
                        {
                            NumeCuloareJson = color.NumeCuloareJsonDto,
                            IdCodCuloare = isCurrentUpdatedColorCodeInDb.IdCodCuloare
                        };
    
                        await colorRepository.AddAsync(newUpdatedColor);
                        await _unitOfWork.CommitAsync();
                        isCurrentUpdatedColorInDb = newUpdatedColor;
                    }
                   
    
                    var isCurrentColorOnProduct = await colorsOnProductsRepository
                        .FindQueryable(pc => pc.IdProdus == productId
                                             && pc.IdCuloare == isCurrentUpdatedColorInDb.IdCuloare)
                        .FirstOrDefaultAsync();
    
                    if (isCurrentColorOnProduct == null)
                    {
                        var newProduseCuCulori = new ProduseCuCulori
                        {
                            IdProdus = productId,
                            IdCuloare = isCurrentUpdatedColorInDb.IdCuloare
                        };
                        await colorsOnProductsRepository.AddAsync(newProduseCuCulori);
                        await _unitOfWork.CommitAsync();
                        isCurrentColorOnProduct = newProduseCuCulori;
                    }
                  

                    var currentIdProdusCuCuloare = isCurrentColorOnProduct.IdProdusCuCuloare;
                    newColorsLinkedWithProductsIds.Add(currentIdProdusCuCuloare); // (1,4,5)
                   
                    // Handle images
                    if (!color.ImaginiProdusDto.IsNullOrEmpty())
                    {
                       
                        foreach (var imageDto in color.ImaginiProdusDto!)
                        {
                            
                            var existingImage = await imagesRepository
                                .FindQueryable(i => i.CaleImagine == imageDto.CaleImagineDto 
                                                    && i.FisierInBucket == imageDto.FisierInBucketDto 
                                                    && i.IdProdusCuCuloare == currentIdProdusCuCuloare)
                                .FirstOrDefaultAsync();
                            _logger.LogInformation($"cale : {imageDto.CaleImagineDto}");
                            if (existingImage == null)
                            {
                                // New image to add
                               
                                var imageFile = images.FirstOrDefault(img =>
                                    img.FileName == imageDto.CaleImagineDto.Split("_")[0] + $"{Path.GetExtension(imageDto.CaleImagineDto)}");
                                if (imageFile != null)
                                {
                                   
                                    if (!MyRegex().IsMatch(imageDto.CaleImagineDto))
                                    { 
                                        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(imageDto.CaleImagineDto);
                                        var extension = Path.GetExtension(imageDto.CaleImagineDto);
                                        
                                        fileNameWithoutExt = fileNameWithoutExt.Replace('_', '-');

                                        // Append your custom string and then add back the extension.
                                        imageDto.CaleImagineDto = $"{fileNameWithoutExt}_X{color.CodCuloareDto}_{modifiedProduct.TipulProdusuluiJsonDto!.TipProdusRomana.ToUpper()}{extension}";
                                    }
                                    
                                    var newImage = new Imagini
                                    {
                                        CaleImagine =   imageDto.CaleImagineDto,
                                        FisierInBucket = imageDto.FisierInBucketDto,
                                        IdProdusCuCuloare = currentIdProdusCuCuloare
                                    };
    
                                    await imagesRepository.AddAsync(newImage);
    
                                    // Upload the image to the clouda
                                    using var currentImageStream = new MemoryStream();
                                    await imageFile.CopyToAsync(currentImageStream);
                                    currentImageStream.Position = 0;
    
                                    var bucketResponse = await _bucketAcces.AddOrUpdateToBucket(
                                        currentImageStream,
                                        newImage.FisierInBucket,
                                        newImage.CaleImagine);

                                    _logger.LogInformation(bucketResponse == 1
                                        ? "Successfully added new image to bucket"
                                        : "An error occurred while adding the image to the bucket");
                                }
                            }
                        }
    
                        successCodes[3] = 1;
                    } else
                    {
                        successCodes[3] = 1;
                    }
                }

                var index = 0;
               
                foreach (var oldEntry in existingColorsOnProducts)
                {
                    var shouldDeleteOldEntry = modifiedProductColors
                        .Any(modifiedColor =>
                            modifiedColor.CodCuloareDto == oldEntry.Culoare.CodCuloare.CodCuloare
                            && modifiedColor.NumeCuloareJsonDto.CuloareRomana ==
                            oldEntry.Culoare.NumeCuloareJson.CuloareRomana);

                    if (!shouldDeleteOldEntry)
                    {
                        oldColorLinkedWithProductEntries.Add(oldEntry);
                        if (!oldEntry.ImagProduseCuCulori.IsNullOrEmpty())
                        {
                            var transferOldImagesToNewColor = new List<Imagini>();
                            foreach (var imageToDelete in oldEntry.ImagProduseCuCulori!)
                            {
                                var newImage = new Imagini
                                {
                                    CaleImagine =   imageToDelete.CaleImagine,
                                    FisierInBucket = imageToDelete.FisierInBucket,
                                    IdProdusCuCuloare = newColorsLinkedWithProductsIds[index]
                                };
                                transferOldImagesToNewColor.Add(newImage);
                                // retrieve old image
                                var retrieveImage = await _bucketAcces.DownloadFile(
                                    $"images/{imageToDelete.FisierInBucket}/{imageToDelete.CaleImagine}");
                                // delete old image
                                await _bucketAcces.DeleteFromBucket(
                                    $"images/{imageToDelete.FisierInBucket}/{imageToDelete.CaleImagine}");
                                // Transfer image
                                if (retrieveImage != null)
                                {
                                    using var currentImageStream = new MemoryStream();
                                    await retrieveImage.CopyToAsync(currentImageStream);
                                    currentImageStream.Position = 0;

                                    var bucketResponse = await _bucketAcces.AddOrUpdateToBucket(
                                        currentImageStream,
                                        newImage.FisierInBucket,
                                        newImage.CaleImagine!);

                                    _logger.LogInformation(bucketResponse == 1
                                        ? "Successfully transfered image to new color"
                                        : "An error occurred while adding the image to the bucket");
                                }
                               
                            }

                            await imagesRepository.AddRangeAsync(transferOldImagesToNewColor);
                        }
                       
                    }

                    index++;
                }
                
                if (oldColorLinkedWithProductEntries.Count > 0)
                {
                    await colorsOnProductsRepository.DeleteRangeAsync(oldColorLinkedWithProductEntries);
                }
                
    
                successCodes[2] = 1;
            }
            else
            {
                successCodes[2] = 1;
            }
    
            // Handle dimensions
            var dimensionOnProductsRepository = _unitOfWork.Repository<ProduseCuDimensiuni>();
            var dimensionsRepository = _unitOfWork.Repository<Dimensiuni>();
            var modifiedDimensions = modifiedProduct.DimensiuniProduseDto;
            var newDimensions = new List<Dimensiuni>();
            var newDImensionsLinkedWithProducts = new List<ProduseCuDimensiuni>();
            var allDimensionLinkedWithProduct = await dimensionOnProductsRepository
                .FindQueryable(d => d.IdProdus == productId)
                .ToListAsync();

            var oldDimensionsLinkedWithProductToDelete = new List<ProduseCuDimensiuni>();
            // initial : dim1 , dim2 , dim 3
            // modified : dim4 , dim 2 , dim 3
            if (!modifiedDimensions.IsNullOrEmpty())
            {
                foreach (var updatedDimension in modifiedDimensions!)
                {
                    var isNewDimensionInDb = await dimensionsRepository
                        .FindQueryable(d => d.Lungime == updatedDimension.LungimeDto
                                            && d.Latime == updatedDimension.LatimeDto
                                            && d.RecomandarePat == updatedDimension.RecomandarePat)
                        .FirstOrDefaultAsync();

                    if (isNewDimensionInDb == null)
                    {
                        var newUpdatedDimension = new Dimensiuni
                        {
                            Lungime = updatedDimension.LungimeDto!,
                            Latime = updatedDimension.LatimeDto!,
                            RecomandarePat = updatedDimension.RecomandarePat,
                        };

                        newDimensions.Add(newUpdatedDimension);
                       
                    }
                }

                if (newDimensions.Count > 0)
                {
                    await dimensionsRepository.AddRangeAsync(newDimensions);
                    await _unitOfWork.CommitAsync();
                    newDimensions = await dimensionsRepository.GetSimpleQueryable()
                        .ToListAsync();
                    
                    foreach (var dimension in modifiedDimensions)
                    {
                    
                        var newDimensionAdded = newDimensions.First(d => d.Lungime == dimension.LungimeDto
                                                                         && d.Latime == dimension.LatimeDto &&
                                                                         d.RecomandarePat == dimension.RecomandarePat);
                        var isNewDimensionLinkedWithProductInDb = allDimensionLinkedWithProduct
                            .FirstOrDefault(pd =>
                                pd.IdDimensiune == newDimensionAdded.IdDimensiune && pd.IdProdus == productId);

                        if (isNewDimensionLinkedWithProductInDb == null)
                        {
                            var newUpdatedDimensionLinkedWithProduct = new ProduseCuDimensiuni
                            {
                                Pret = dimension.PretDto,
                                PretRedus = dimension.PretRedusDto,
                                IdDimensiune = newDimensionAdded.IdDimensiune,
                                IdProdus = productId
                            };
                            newDImensionsLinkedWithProducts.Add(newUpdatedDimensionLinkedWithProduct);
                        }

                    
                    }

                    await dimensionOnProductsRepository.AddRangeAsync(newDImensionsLinkedWithProducts);

                }
                
                // remove old dimensions entry
                oldDimensionsLinkedWithProductToDelete.AddRange(from oldDimensionEntry in allDimensionLinkedWithProduct 
                    let shouldDeleteOldEntry = modifiedDimensions.Any(d => d.LungimeDto == oldDimensionEntry.PdDimensiune!.Lungime 
                                                                           && d.LatimeDto == oldDimensionEntry.PdDimensiune!.Latime 
                                                                           && d.RecomandarePat == oldDimensionEntry.PdDimensiune!.RecomandarePat) 
                    where !shouldDeleteOldEntry select oldDimensionEntry);

                if (oldDimensionsLinkedWithProductToDelete.Count > 0)
                {
                    await dimensionOnProductsRepository.DeleteRangeAsync(oldDimensionsLinkedWithProductToDelete);
                }
            }
            else
            {
                if (modifiedProduct.TipulProdusuluiDto is "perdea" or "draperie")
                {
                    var oldEntriesOfDimension = await dimensionOnProductsRepository
                        .FindQueryable(p => p.IdProdus == productId)
                        .ToListAsync();

                    if (oldEntriesOfDimension.Count > 0)
                    {
                        await dimensionOnProductsRepository.DeleteRangeAsync(oldEntriesOfDimension);
                    }
                }
               
            }

            
    
            successCodes[4] = 1;
    
            // Commit all changes in a transaction
            await _unitOfWork.CommitTransactionAsync(modifyingTransaction);

            _logger.LogInformation(isAddingFlag
                ? $"Succesfully added new product with code {productToBeModified.CodProdus}"
                : $"Succesfully updated the product with code {productToBeModified.CodProdus}");

            return successCodes;
        }
        catch (Exception ex)
        {
            if (modifyingTransaction != null)
            {
                await _unitOfWork.RollBackTransactionAsync(modifyingTransaction);
            }
            _logger.LogError(ex, "An error occurred while updating the product");
            if (ex is DbUpdateException)
            {
                return [];
            }
            throw;
        }
    }



    /// <summary>
    /// The method to add a product.
    /// </summary>
    /// <param name="produseDto">The DTO to transfer the data</param>
    /// <param name="idDimensiuni">List of the id's of the dimensions of the product</param>
    /// <param name="filePath">Relative path of the images</param>
    /// <param name="idTipProduse">The id's of the product types</param>
    /// <param name="culori">Pair array of colors and color codes [grey-08,blue-03]</param>
    /// <param name="preturiPerDimensiuni">The prices per dimensions</param>
    /// <param name="idProducator">The id of the manufacturer</param>
    /// <param name="tipProdus">Product type (blanket/curtain/etc)</param>
    /// <param name="folderNames">directory name in S3 bucket </param>
    /// <returns>A success for value 1 and error for value 0 (Int)</returns>
    public async Task<int> AddOrEditProductFromExcel(ProduseDto produseDto, IList<int> idDimensiuni, 
        string[] filePath, IList<int> idTipProduse, IList<int> culori,
        string[] preturiPerDimensiuni, int idProducator,string tipProdus,
        string[] folderNames)
    {
      
        try
        {
            // repositories
            var productRepository = _unitOfWork.Repository<Produse>();
            var producatoriRepository = _unitOfWork.Repository<Producatori>();
            var dimensiuniCuProduseRepository = _unitOfWork.Repository<ProduseCuDimensiuni>();
            var tipuriPeProduseRepository = _unitOfWork.Repository<TipuriPeProduse>();
            var culoriCuProduseRepository = _unitOfWork.Repository<ProduseCuCulori>();
            var colorCodesRepository = _unitOfWork.Repository<CodCulori>();
            var colorsRepository = _unitOfWork.Repository<Culori>();
            var imagesRepository = _unitOfWork.Repository<Imagini>();
            
            var findProductAlreadyInDb = await productRepository
                .FindQueryable(p => p.CodProdus == produseDto.CodProdusDto)
                .FirstOrDefaultAsync();


            if (findProductAlreadyInDb is null)
            {
                _logger.LogInformation($"No product found with cod {produseDto.CodProdusDto}");
                Produse newProduct;
                if (produseDto.InaltimeMaximaDto == 0 && string.Equals(produseDto.TipProdusJsonDto.TipProdusRomana , "perdea") || string.Equals(produseDto.TipProdusJsonDto.TipProdusRomana , "draperie")  )
                {
                    throw new DbUpdateException(
                        $"Produsul cu codul {produseDto.CodProdusDto} este o perdea/draperie si are inaltimea maxima a materialului la 0. ");
                } 
                
                if (produseDto.InaltimeMaximaDto != 0 && !string.Equals(produseDto.TipProdusJsonDto.TipProdusRomana, "perdea") &&
                          !string.Equals(produseDto.TipProdusJsonDto.TipProdusRomana, "draperie"))
                {
                    throw new DbUpdateException(
                        $"Produsul cu codul {produseDto.CodProdusDto} nu este o perdea/draperie si are inaltimea maxima o valoare diferita de 0. ");
                }
                
                
                if (idProducator != 0)
                {
                    var manufacturer = await producatoriRepository
                        .GetByIdAsync(idProducator);

                    if (manufacturer == null)
                    {
                        _logger.LogError($"No producator with id -> {idProducator} was found");
                        throw new DbUpdateException($"No producator was found with id: {idProducator}");
                    }
                    
                  
                
                    newProduct = new Produse
                    {
                        CodProdus = produseDto.CodProdusDto!,
                        // Descriere = produseDto.DescriereDto,
                        DescriereJson = produseDto.DescriereJsonDto,
                        // NumeProdus = produseDto.NumeProdusDto,
                        NumeProdusJson = produseDto.NumeProdusJsonDto,
                        // Compozitie = produseDto.CompozitieDto,
                        CompozitieJson = produseDto.CompozitieJsonDto,
                        Tva = produseDto.TvaDto,
                        // Ingrijire = produseDto.IngrijireDto,
                        IngrijireJson = produseDto.IngrijireJsonDto,
                        FataReversibila = produseDto.FataReversibilaDto,
                        Stoc = produseDto.StocDto,
                        IsDeleted = produseDto.IsDeletedDto,
                        ActivInMagazin = produseDto.ActivInMagazinDto,
                        // TipulProdusului = produseDto.TipProdusDto,
                        TipulProdusuluiJson = produseDto.TipProdusJsonDto,
                        InaltimeMaxima = produseDto.InaltimeMaximaDto,
                        PretDeBaza = produseDto.PretBazaDto,
                        PretDeBazaRedus = produseDto.PretBazaRedusDto,
                        IdProducator = idProducator,
                        Producator = manufacturer,
                    };
                    
                  

                    await productRepository.AddAsync(newProduct);
                    await _unitOfWork.CommitAsync();
                    _logger.LogInformation("Product saved succesfully with manufacturer");
                    findProductAlreadyInDb = newProduct;
                }
                else
                {
                    newProduct = new Produse
                    {
                        CodProdus = produseDto.CodProdusDto!,
                        // Descriere = produseDto.DescriereDto,
                        DescriereJson = produseDto.DescriereJsonDto,
                        // NumeProdus = produseDto.NumeProdusDto,
                        NumeProdusJson = produseDto.NumeProdusJsonDto,
                        // Compozitie = produseDto.CompozitieDto,
                        CompozitieJson = produseDto.CompozitieJsonDto,
                        Tva = produseDto.TvaDto,
                        // Ingrijire = produseDto.IngrijireDto,
                        IngrijireJson = produseDto.IngrijireJsonDto,
                        FataReversibila = produseDto.FataReversibilaDto,
                        Stoc = produseDto.StocDto,
                        IsDeleted = produseDto.IsDeletedDto,
                        ActivInMagazin = produseDto.ActivInMagazinDto,
                        // TipulProdusului = produseDto.TipProdusDto,
                        TipulProdusuluiJson = produseDto.TipProdusJsonDto,
                        PretDeBaza = produseDto.PretBazaDto,
                        InaltimeMaxima = produseDto.InaltimeMaximaDto,
                        PretDeBazaRedus = produseDto.PretBazaRedusDto,
                        IdProducator = null
                    };
                
                    await productRepository.AddAsync(newProduct);
                    await _unitOfWork.CommitAsync();
                    _logger.LogInformation("Product saved succesfully without manufacturer");
                    findProductAlreadyInDb = newProduct;
                }
                
                var dimensionsArray = idDimensiuni.ToArray();
                var productId = findProductAlreadyInDb.IdProdus;
                
                if (!dimensionsArray.IsNullOrEmpty() && !preturiPerDimensiuni.IsNullOrEmpty())
                {
                    for (var iterator = 0; iterator < preturiPerDimensiuni.Length; iterator++)
                    {
                        var newProductWithDimension = new ProduseCuDimensiuni
                        {
                            Pret = decimal.Parse(preturiPerDimensiuni[iterator]),
                            IdDimensiune = dimensionsArray[iterator] ,
                            IdProdus = productId
                        };

                        await dimensiuniCuProduseRepository.AddAsync(newProductWithDimension);
                        await _unitOfWork.CommitAsync();
                        _logger.LogInformation($"Dimension id : {dimensionsArray[iterator]} - Product id : {productId} SAVED");
                    
                    }
                }
                
                var productTypesArray = idTipProduse.ToArray();

                foreach (var type in productTypesArray)
                {
                    var newProductWithType = new TipuriPeProduse
                    {
                        IdTipProdus = type,
                        IdProdus = productId
                    };

                    await tipuriPeProduseRepository.AddAsync(newProductWithType);
                    await _unitOfWork.CommitAsync();
                    _logger.LogInformation($"Product type id: {type} - Product id: {productId} SAVED");
                }

                var colorsIdArray = culori.ToArray();
                IList<ProduseCuCulori> productWithColorsJustAdded = [];
                foreach (var currentColorId in colorsIdArray)
                {
                    var temp = new ProduseCuCulori
                    {
                        IdProdus = productId,
                        IdCuloare = currentColorId
                    };

                    await culoriCuProduseRepository.AddAsync(temp);
                    await _unitOfWork.CommitAsync();
                    _logger.LogInformation($"Color id: {currentColorId} - Product id: {productId} SAVED");
                    productWithColorsJustAdded.Add(temp);
                }

                // var dirNameInS3 = folderName;
                if (!filePath.IsNullOrEmpty())
                {
                    var imagesDirPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}/imagini_import";
                    var index = 0;
                    foreach (var imageName in filePath)
                    {
                        
                        // AYLIN_X03_CUVERTURA
                        // ProductWithColorsObjects that were added ( id_produs, id_culoare)
                        // link the image to respective color

                        var patternCode = @"X(\d{2})";
                        string colorCode;
                        Match matchColor = Regex.Match(imageName, patternCode);

                        if (matchColor.Success)
                        {
                            colorCode = matchColor.Groups[1].Value;
                            _logger.LogInformation($"Color code extrated {colorCode}");
                        }
                        else
                        {
                            throw new DbUpdateException("Color code not found in the image name");
                        }
                        
                        // 03
                        // look for the id of the color code

                        var currentColorCode = await colorCodesRepository
                            .FindQueryable(c => c.CodCuloare == colorCode)
                            .FirstOrDefaultAsync();

                        if (currentColorCode is null)
                        {
                            throw new DbUpdateException($"Codul de culoare {colorCode} nu a fost gasit");
                        }
                        
                        // id of the color linked with the currentColorCode
                        var colorRelatedToTheCurrentColorCode = await colorsRepository
                            .FindQueryable(c => c.IdCodCuloare == currentColorCode.IdCodCuloare)
                            .FirstOrDefaultAsync();
                        
                        if (colorRelatedToTheCurrentColorCode is null)
                        {
                            throw new DbUpdateException($"Culoarea cu codul {currentColorCode.CodCuloare} nu a fost gasita");
                        }

                        var productWithColorIdToBeLinkedToImage = productWithColorsJustAdded
                            .FirstOrDefault(p => p.IdProdus == productId
                                                 && p.IdCuloare == colorRelatedToTheCurrentColorCode.IdCuloare);
                        
                        if (productWithColorIdToBeLinkedToImage is null)
                        {
                            throw new DbUpdateException($"Product with id  {productId} with color id {colorRelatedToTheCurrentColorCode.IdCuloare} not found              ");
                        }
                        
                        
                        var newImage = new Imagini
                        {
                            CaleImagine = imageName,
                            IdProdusCuCuloare = productWithColorIdToBeLinkedToImage.IdProdusCuCuloare,
                            FisierInBucket = folderNames[index]
                        };

                        await imagesRepository.AddAsync(newImage);
                        await _unitOfWork.CommitAsync();
                        
                        var machineFullImagePath = Path.Join(imagesDirPath,imageName); // image path for the PC
                       
                        var responseFromBucketAdd = await _bucketAcces.AddOrUpdateToBucket(machineFullImagePath,folderNames[index], imageName);

                        if (responseFromBucketAdd == -1)
                        {
                            throw new DbUpdateException("Problem when uploading file to S3 Bucket");
                        }
                        
                        _logger.LogInformation("Succesfully saved relative image path to database");
                        index++;
                    }
                }
                else
                {
                    _logger.LogInformation($"No images added for the product  : {findProductAlreadyInDb.CodProdus} ");
                    return 1;
                }

                return 1;
            }
            else
            {
                
                if (string.Equals(produseDto.TipProdusJsonDto.TipProdusRomana, "perdea") ||
                    string.Equals(produseDto.TipProdusJsonDto.TipProdusRomana, "draperie"))
                {
                    if (produseDto.InaltimeMaximaDto == 0)
                    {
                        throw new DbUpdateException(
                            "Ati lasat inaltimea maxima la 0 , desi tipul produsului este perdea/draperie");
                    }
                }
                else
                {
                    if (produseDto.InaltimeMaximaDto != 0)
                    {
                        throw new DbUpdateException(
                            "Ati lasat inaltimea maxima la o valoare , desi tipul produsului nu este perdea/draperie");
                    }
                }
               
                // product locked , cannot update
                if (findProductAlreadyInDb.IsLocked)
                {
                    throw new DbUpdateException("Unul sau mai multe produse este blocat . Un utilizator cumpara acel / acele produs / produse");
                }
                var productId = findProductAlreadyInDb.IdProdus;
                var productCode = findProductAlreadyInDb.CodProdus;
                _logger.LogInformation($"Updating product with code {productCode}");
                if (idProducator != 0)
                {
                    
                    var manufacturer = await producatoriRepository
                        .GetByIdAsync(idProducator);
                    
                    if (manufacturer == null)
                    {
                        _logger.LogError($"No producator with id -> {idProducator} was found");
                        throw new DbUpdateException($"No producator was found with id: {idProducator}");
                    }

                    _logger.LogInformation($"Updating product with code 3 {productCode}");

                    if (produseDto.CodProdusDto != null && produseDto.CodProdusDto != productCode)
                    {
                        var isCodeAlreadyUsed = await productRepository
                            .FindQueryable(p => p.CodProdus == produseDto.CodProdusDto)
                            .FirstOrDefaultAsync();

                        if (isCodeAlreadyUsed is not null)
                        {
                            _logger.LogInformation("Use another product code please!");
                            throw new DbUpdateException($"Cod produs deja folosit : {produseDto.CodProdusDto}");
                        }
                    }
                    
                    _logger.LogInformation($"Updating product with code  {productCode}");

                    
                    var updatedProduct = new Produse
                    {
                        CodProdus = produseDto.CodProdusDto!,
                        // Descriere = produseDto.DescriereDto,
                        DescriereJson = produseDto.DescriereJsonDto,
                        // NumeProdus = produseDto.NumeProdusDto,
                        NumeProdusJson = produseDto.NumeProdusJsonDto,
                        // Compozitie = produseDto.CompozitieDto,
                        CompozitieJson = produseDto.CompozitieJsonDto,
                        Tva = produseDto.TvaDto,
                        // Ingrijire = produseDto.IngrijireDto,
                        IngrijireJson = produseDto.IngrijireJsonDto,
                        FataReversibila = produseDto.FataReversibilaDto,
                        Stoc = produseDto.StocDto,
                        IsDeleted = produseDto.IsDeletedDto,
                        ActivInMagazin = produseDto.ActivInMagazinDto,
                        // TipulProdusului = produseDto.TipProdusDto,
                        TipulProdusuluiJson = produseDto.TipProdusJsonDto,
                        PretDeBaza = produseDto.PretBazaDto,
                        InaltimeMaxima = produseDto.InaltimeMaximaDto,
                        PretDeBazaRedus = produseDto.PretBazaRedusDto,
                        IdProducator = idProducator
                    };
                    
                    _logger.LogDebug("After creating the product with productor");

                    _mapper.Map(updatedProduct, findProductAlreadyInDb);
                    
                    await _unitOfWork.CommitAsync();
                    _logger.LogInformation($"Updated product with cod : {produseDto.CodProdusDto} with manufacturer: {idProducator}");

                }
                else
                {
                    var updatedProduct = new Produse
                    {
                        CodProdus = produseDto.CodProdusDto!,
                        // Descriere = produseDto.DescriereDto,
                        DescriereJson = produseDto.DescriereJsonDto,
                        // NumeProdus = produseDto.NumeProdusDto,
                        NumeProdusJson = produseDto.NumeProdusJsonDto,
                        // Compozitie = produseDto.CompozitieDto,
                        CompozitieJson = produseDto.CompozitieJsonDto,
                        Tva = produseDto.TvaDto,
                        // Ingrijire = produseDto.IngrijireDto,
                        IngrijireJson = produseDto.IngrijireJsonDto,
                        FataReversibila = produseDto.FataReversibilaDto,
                        Stoc = produseDto.StocDto,
                        PretDeBaza = produseDto.PretBazaDto,
                        IsDeleted = produseDto.IsDeletedDto,
                        InaltimeMaxima = produseDto.InaltimeMaximaDto,
                        ActivInMagazin = produseDto.ActivInMagazinDto,
                        // TipulProdusului = produseDto.TipProdusDto,
                        TipulProdusuluiJson = produseDto.TipProdusJsonDto,
                        IdProducator = null
                    };
                    
                    
                    _mapper.Map(updatedProduct, findProductAlreadyInDb);
                    
                    
                    await _unitOfWork.CommitAsync();
                    _logger.LogInformation($"Updated product with cod : {produseDto.CodProdusDto} without manufacturer");
                }
                

                var dimensionsUpdatedId = idDimensiuni.ToArray();
                if (findProductAlreadyInDb.TipulProdusuluiJson.TipProdusRomana is "draperie" or "perdea")
                {
                    if (dimensionsUpdatedId.Length >= 1 )
                    {
                        throw new DbUpdateException(
                            $"Tipul noului produsul este o perdea/draperie dar ati lasat in fisierul excel dimensiuni pentru produsul : {findProductAlreadyInDb.CodProdus}");
                    }
                    
                    if (preturiPerDimensiuni.Length >= 1 )
                    {
                        throw new DbUpdateException(
                            $"Tipul noului produsul este o perdea/draperie dar ati lasat in fisierul excel preturi per dimensiune la produsul: {findProductAlreadyInDb.CodProdus}");
                    }
                    
                }
                
                _logger.LogInformation("Retrieving old entry's");

                var oldEntrysOfTheProductDimensions = await dimensiuniCuProduseRepository
                    .FindQueryable(pd => pd.IdProdus == productId)
                    .ToListAsync();

              
                _logger.LogInformation("Succesfully retrieved old entrys");
                
                // updating dimensions types
                var newAddedDimensionCount = 0;
                if (!dimensionsUpdatedId.IsNullOrEmpty() && !preturiPerDimensiuni.IsNullOrEmpty())
                {
                    for (var iterator = 0; iterator < preturiPerDimensiuni.Length; iterator++)
                    {
                        var alreadyProductWithDimensionInDb = oldEntrysOfTheProductDimensions
                            .FirstOrDefault(pd => pd.IdDimensiune == dimensionsUpdatedId[iterator] 
                            && pd.IdProdus == productId);
                        if (alreadyProductWithDimensionInDb is not null)
                        {
                            _logger.LogInformation("This dimension is already linked with the product");
                            var newPrice = decimal.Parse(preturiPerDimensiuni[iterator]);
                            if ( newPrice != alreadyProductWithDimensionInDb.Pret)
                            {
                                alreadyProductWithDimensionInDb.Pret = decimal.Parse(preturiPerDimensiuni[iterator]);
                                await dimensiuniCuProduseRepository.UpdateAsync(alreadyProductWithDimensionInDb);
                             
                                _logger.LogInformation($"Modified in product_dimensions table the entry: ID -> {alreadyProductWithDimensionInDb.IdProdusCuDimensiune}\n" +
                                                       $"with newPrice -< {newPrice} ");
                                oldEntrysOfTheProductDimensions.Remove(alreadyProductWithDimensionInDb);
                            }
                            else
                            {
                                _logger.LogInformation("No attibutes have been changed");
                                oldEntrysOfTheProductDimensions.Remove(alreadyProductWithDimensionInDb);
                            }
                        }
                        else
                        {
                            var newProductWithDimension = new ProduseCuDimensiuni
                            {
                                Pret = decimal.Parse(preturiPerDimensiuni[iterator]),
                                IdDimensiune = dimensionsUpdatedId[iterator] ,
                                IdProdus = productId
                            };

                            await dimensiuniCuProduseRepository.AddAsync(newProductWithDimension);
                            await _unitOfWork.CommitAsync();
                            _logger.LogInformation($"Dimension id : {dimensionsUpdatedId[iterator]} - Product id : {productId} SAVED");
                            newAddedDimensionCount++;
                        }
                    }
                }

                if (oldEntrysOfTheProductDimensions.Count != 0)
                {
                    _logger.LogInformation("Deleting old entrys of the product linked with the dimensions");
                    await dimensiuniCuProduseRepository.DeleteRangeAsync(oldEntrysOfTheProductDimensions);
                }
                else
                {
                    if (newAddedDimensionCount == 0)
                    {
                        _logger.LogInformation("No product with dimensions were updated ");
                    }
                    _logger.LogInformation($"Added: {newAddedDimensionCount} new product with dimensions");
                    

                }
                // updating product types

                var oldEntrysOfTypes = await tipuriPeProduseRepository
                    .FindQueryable(tp => tp.IdProdus == productId)
                    .ToListAsync();
                
                var updatedProductTypesId = idTipProduse.ToArray();
                var newAddedTypesCount = 0;
                foreach (var type in updatedProductTypesId)
                {
                    var isNewTypeInOldTypeList = oldEntrysOfTypes
                        .FirstOrDefault(tp => tp.IdTipProdus == type);

                    if (isNewTypeInOldTypeList is not null)
                    {
                        _logger.LogInformation($"The type with id : {type} was already in the old list \n" +
                                               $"Nothing has been modified ");
                        oldEntrysOfTypes.Remove(isNewTypeInOldTypeList);
                        continue;
                    }
                    
                    var newProductWithType = new TipuriPeProduse
                    {
                        IdTipProdus = type,
                        IdProdus = productId,
                    };

                    await tipuriPeProduseRepository.AddAsync(newProductWithType);
                    await _unitOfWork.CommitAsync();
                    _logger.LogInformation($"Product type id: {type} - Product id: {productId} SAVED");
                    newAddedTypesCount++;
                }
                
                if (oldEntrysOfTypes.Count != 0)
                {
                    _logger.LogInformation("Deleting old entrys of the product linked with the dimensions");
                    await tipuriPeProduseRepository.DeleteRangeAsync(oldEntrysOfTypes);
                }
                else
                {
                    if (newAddedTypesCount == 0)
                    {
                        _logger.LogInformation("No types were updated ");
                    }
                    
                    _logger.LogInformation($"Added: {newAddedTypesCount} new product with types entries");
                    
                }
                
                // updating color types
                var oldEntriesOfProductAsociatedWithColors = await culoriCuProduseRepository
                    .FindQueryable(pc => pc.IdProdus == productId)
                    .ToListAsync();
                
                var updatedColorsIdArray = culori.ToArray();
                IList<ProduseCuCulori> productWithColorsJustAdded = [];

                var newColorsWithProductsAdded = 0;
                // here we have all the old image names for the product with the key name
                IList<KeyValuePair<string,string>> oldImageNames = [];
                foreach (var entry in oldEntriesOfProductAsociatedWithColors)
                {
                    var oldEntrysOfTheImages = await imagesRepository
                        .FindQueryable(i => i.IdProdusCuCuloare == entry.IdProdusCuCuloare)
                        .ToListAsync();

                    foreach (var oldImageEntry in oldEntrysOfTheImages)
                    {
                        oldImageNames.Add(new KeyValuePair<string, string>(oldImageEntry.CaleImagine!,oldImageEntry.FisierInBucket));
                    }
                }
                
                foreach (var currentColorId in updatedColorsIdArray)
                {
                    var isOldEntryInNewEntryOfProductColors = oldEntriesOfProductAsociatedWithColors
                        .FirstOrDefault(pc => pc.IdCuloare == currentColorId );

                    if (isOldEntryInNewEntryOfProductColors is not null)
                    {
                        _logger.LogInformation($"Same entry with new color id : {currentColorId} " +
                                               $"is the same with old one : {isOldEntryInNewEntryOfProductColors.IdCuloare} \n" +
                                               $"Nothing has been modified");
                        oldEntriesOfProductAsociatedWithColors.Remove(isOldEntryInNewEntryOfProductColors);
                        productWithColorsJustAdded.Add(isOldEntryInNewEntryOfProductColors);
                        continue;
                    }
                    
                    var temp = new ProduseCuCulori
                    {
                        IdProdus = productId,
                        IdCuloare = currentColorId,
                    };

                    await culoriCuProduseRepository.AddAsync(temp);
                    await _unitOfWork.CommitAsync();
                    _logger.LogInformation($"Color id: {currentColorId} - Product id: {productId} SAVED");
                    productWithColorsJustAdded.Add(temp);
                    newColorsWithProductsAdded++;
                }
                

                if (oldEntriesOfProductAsociatedWithColors.Count != 0)
                {
                    // here we are deleting the images associated with this 
                    _logger.LogInformation($"Deleting the old entrys of product {productCode} linked with the colors");
                    await culoriCuProduseRepository.DeleteRangeAsync(oldEntriesOfProductAsociatedWithColors);
                }
                else
                {
                    if (newColorsWithProductsAdded == 0)
                    {
                        _logger.LogInformation("The updated entries corresponded with the old ones. No modifications");
                    }
                    _logger.LogInformation($"Added: {newColorsWithProductsAdded} new product with colors entities");
                }
                
                var newAddedImages = 0;


                // Populate imagesToRemove
                var imagesToRemove = oldImageNames.Where(oldImage => 
                    !filePath.Contains(oldImage.Key)).ToList();

                // Populate imagesToAdd
                var index = 0;
                var imagesToAdd = new List<KeyValuePair<string,string>>();
                // image_name , bucket_dir
                foreach (var newImage in filePath)
                {
                    var isNewImageAnOldImage = oldImageNames.FirstOrDefault(pair =>
                        pair.Key == newImage && pair.Value == folderNames[index]);
                    if (isNewImageAnOldImage.Key == null)
                    {
                        imagesToAdd.Add(new KeyValuePair<string, string>(newImage , folderNames[index]));
                    }
                    index++;
                }
                // var imagesToAdd = (from newImage in filePath
                //                              let isNewImageAnOldImage = oldImageNames.FirstOrDefault(pair => pair.Key == newImage && pair.Value == folderName) 
                //                              where isNewImageAnOldImage.Key == null 
                //                              select newImage).ToList();

                if (imagesToRemove.IsNullOrEmpty() && imagesToAdd.IsNullOrEmpty())
                {
                    _logger.LogInformation($"No photo has been changed for product: {productCode}");
                    return 2;
                }
                

                // Case 2: Remove old images no longer present in new set
                if (imagesToRemove.Count > 0)
                {
                    foreach (var (key, dirInS3OfImageToBeRemoved) in imagesToRemove)
                    {
                        // Remove from database
                        var imagesRemainedInTheDb = await imagesRepository
                            .FindQueryable(i => i.CaleImagine == key)
                            .FirstOrDefaultAsync();
                
                        if (imagesRemainedInTheDb != null)
                        {
                            await imagesRepository.DeleteAsync(imagesRemainedInTheDb);
                        }
                
                        // Remove from S3 bucket
                        var keyNameInS3OfFileToBeRemoved = $"images/{dirInS3OfImageToBeRemoved}/{key}";
                
                        var deletingFromBucketResponse = await _bucketAcces.DeleteFromBucket(keyNameInS3OfFileToBeRemoved);
                
                        if (deletingFromBucketResponse == 1)
                        {
                            _logger.LogInformation($"Succesfully deleted item {keyNameInS3OfFileToBeRemoved} from bucket");
                        }
                        else
                        {
                            throw new DbUpdateException($"Failed to delete item {keyNameInS3OfFileToBeRemoved} from bucket");
                        }
                    }
                }
                // Case 1, 4, 5, 6: Add new images that are not in the old set

                if (imagesToAdd.Count > 0)
                {
                    // key:image_name , value:bucket_dir
                    var imageDirPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}/imagini_import/imagini_update";
                    foreach (var (key,value) in imagesToAdd)
                    {
                        var fullImagePath = Path.Join(imageDirPath, key);
                
                        var responseFromBucketAdd = await _bucketAcces.AddOrUpdateToBucket(fullImagePath, value, key);
                
                        if (responseFromBucketAdd == -1)
                        {
                            throw new DbUpdateException($"An error happened when uploading the new photo with name: {key} ");
                        }
                
                        // Extract color code
                        const string patternCode = @"X(\d{2})";
                        string colorCode;
                        var matchColor = Regex.Match(key, patternCode);
                
                        if (matchColor.Success)
                        {
                            colorCode = matchColor.Groups[1].Value;
                            _logger.LogInformation($"Color code extrated {colorCode}");
                        }
                        else
                        {
                            throw new DbUpdateException("Color code not found in the image name");
                        }
                
                        // Look up color code and associated color
                        var currentColorCode = await colorCodesRepository
                            .FindQueryable(c => c.CodCuloare == colorCode)
                            .FirstOrDefaultAsync();
                
                        if (currentColorCode == null)
                        {
                            throw new DbUpdateException($"No color code with code {colorCode}");
                        }
                
                        var colorRelatedToTheCurrentColorCode = await colorsRepository
                            .FindQueryable(c => c.IdCodCuloare == currentColorCode.IdCodCuloare)
                            .FirstOrDefaultAsync();
                
                        if (colorRelatedToTheCurrentColorCode == null)
                        {
                            throw new DbUpdateException($"No color related to the code {currentColorCode}");
                        }
                
                        // Link image to the product color
                        var productWithColorIdToBeLinkedToImage = productWithColorsJustAdded
                            .FirstOrDefault(p => p.IdProdus == productId && p.IdCuloare == colorRelatedToTheCurrentColorCode.IdCuloare);
                        
                        var newUpdatedImage = new Imagini
                        {
                            CaleImagine = key,
                            IdProdusCuCuloare = productWithColorIdToBeLinkedToImage!.IdProdusCuCuloare,
                            FisierInBucket = value
                        };
                
                        await imagesRepository.AddAsync(newUpdatedImage);
                        newAddedImages++;
                
                        _logger.LogInformation("Succesfully updated the images");
                    }
                    _logger.LogInformation($"Added : {newAddedImages} new images");
                }
                return 2;
            }
        }
        catch (DbUpdateException e)
        {
            _logger.LogError(e.Message);
            throw;
        }
    }

    public async Task<int> DeleteTypeOnProduct(string codProdus, string categorieProdus)
    {
        var productRepository = _unitOfWork.Repository<Produse>();
        var typeOnProductRepository = _unitOfWork.Repository<TipuriPeProduse>();
        var typeRepository = _unitOfWork.Repository<TipuriProduse>();
        IDbContextTransaction? deleteTransaction = null;
        try
        {
            deleteTransaction = await _unitOfWork.BeginTransactionAsync();
            var productInDb = await productRepository
                .FindQueryable(p => p.CodProdus == codProdus)
                .FirstOrDefaultAsync();

            if (productInDb is null)
            {
                _logger.LogInformation("Product not found to delete the type on it");
                return 0;
            }

            if (productInDb.IsLocked)
            {
                // Check product locked
                return -3;
            }
        
            _logger.LogInformation("Succesfully found the product to delete the type on it");

            var typeToDeleteOnProduct = await typeRepository
                .FindQueryable(t => EF.Functions.JsonUnquote(EF.Functions.JsonExtract<string>(t.CategorieJson , "$.categorie_ro")) == categorieProdus)
                .FirstOrDefaultAsync();

            if (typeToDeleteOnProduct is null)
            {
                _logger.LogInformation("Type to delete not found");
                return 0;
            }

            var productTypeToDelete = await typeOnProductRepository
                .FindQueryable(tp =>
                    tp.IdProdus == productInDb.IdProdus && tp.IdTipProdus == typeToDeleteOnProduct.IdTipProdus)
                .FirstOrDefaultAsync();

            if (productTypeToDelete is null)
            {
                _logger.LogInformation("Type to delete on product not found");
                return 0;
            }

            await typeOnProductRepository.DeleteAsync(productTypeToDelete);
            await _unitOfWork.CommitTransactionAsync(deleteTransaction);

            return 1;
        }
        catch (Exception e)
        {
            if (deleteTransaction is not null)
            {
                _logger.LogInformation("An error occured, rolling back transaction");
                await _unitOfWork.RollBackTransactionAsync(deleteTransaction);
            }
            _logger.LogInformation(e.Message);
            return - 1;
        }

        
    }

    public async Task<int> DeleteDimensionOnProduct(string codProdus, string lungime, string latime, 
        string pret,string recomandarePat)
    {
        var productRepository = _unitOfWork.Repository<Produse>();
        var dimensionsRepository = _unitOfWork.Repository<Dimensiuni>();
        var dimensionsOnProductRepository = _unitOfWork.Repository<ProduseCuDimensiuni>();
        IDbContextTransaction? deleteTransaction = null;
        try
        {
            deleteTransaction = await _unitOfWork.BeginTransactionAsync();

            var product = await productRepository
                .FindQueryable(p => p.CodProdus == codProdus)
                .FirstOrDefaultAsync();

            if (product is null)
            {
                _logger.LogInformation("Product not found to delete the type on it");
                return 0;
            }
            
            if (product.IsLocked)
            {
                // Check product locked
                return -3;
            }

            var dimensiuneToDeleteOnProduct = await dimensionsRepository
                .FindQueryable(d => d.Lungime == lungime && d.Latime == latime && d.RecomandarePat == recomandarePat)
                .FirstOrDefaultAsync();

            if (dimensiuneToDeleteOnProduct is null)
            {
                _logger.LogInformation("Dimension not found to delete the product asociated it");
                return 0;
            }

            var dimensionLinkedWithProductToDelete = await dimensionsOnProductRepository
                .FindQueryable(pd => pd.IdProdus == product.IdProdus &&
                                     pd.IdDimensiune! == dimensiuneToDeleteOnProduct.IdDimensiune)
                .FirstOrDefaultAsync();
            
            if (dimensionLinkedWithProductToDelete is null)
            {
                _logger.LogInformation("Dimension linked with product in join table not found to delete");
                return 0;
            }

            await dimensionsOnProductRepository.DeleteAsync(dimensionLinkedWithProductToDelete);
            await _unitOfWork.CommitTransactionAsync(deleteTransaction);
            _logger.LogInformation("Dimension on product was deleted succesufully");
            return 1;
        }
        catch (Exception e)
        {
            if (deleteTransaction is not null)
            {
                _logger.LogInformation("An error occured, rolling back transaction");
                _logger.LogInformation($"{e.Message}");

                await _unitOfWork.RollBackTransactionAsync(deleteTransaction);
            }

            return -1;
        }
    }

    public async Task<int> DeleteColorOnProduct(string codProdus, string numeCuloare, string codCuloare)
    {
        var productRepository = _unitOfWork.Repository<Produse>();
        var colorRepository = _unitOfWork.Repository<Culori>();
        var colorCodesRepository = _unitOfWork.Repository<CodCulori>();
        var colorOnProductsRepository = _unitOfWork.Repository<ProduseCuCulori>();
        var imagesRepository = _unitOfWork.Repository<Imagini>();

        IDbContextTransaction? deleteTransaction = null;
        try
        {
            _logger.LogInformation($"{codProdus}");
            deleteTransaction = await _unitOfWork.BeginTransactionAsync();

            var product = await productRepository
                .FindQueryable(p => p.CodProdus == codProdus)
                .FirstOrDefaultAsync();

            if (product is null)
            {
                _logger.LogInformation("Product not found to delete the type on it");
                return 0;
            }
            
            if (product.IsLocked)
            {
                // Check product locked
                return -3;
            }

            var colorCodeAssociatedWithColor = await colorCodesRepository
                .FindQueryable(cc => cc.CodCuloare == codCuloare)
                .FirstOrDefaultAsync();

            if (colorCodeAssociatedWithColor is null)
            {
                _logger.LogInformation("Color code  not found");
                return 0;
            }
            
            var colorToDeleteOnProduct = await colorRepository
                .FindQueryable(c =>  EF.Functions.JsonUnquote(EF.Functions.JsonExtract<string>(c.NumeCuloareJson , "$.culoare_ro")) == numeCuloare && c.IdCodCuloare == colorCodeAssociatedWithColor.IdCodCuloare)
                .FirstOrDefaultAsync();

            if (colorToDeleteOnProduct is null)
            {
                _logger.LogInformation("Color not found to delete the product asociated with it");
                return 0;
            }

            var colorLinkedWithProduct = await colorOnProductsRepository
                .FindQueryable(pd => pd.IdProdus == product.IdProdus &&
                                     pd.IdCuloare == colorToDeleteOnProduct.IdCuloare)
                .FirstOrDefaultAsync();
            
            if (colorLinkedWithProduct is null)
            {
                _logger.LogInformation("Color linked with product in join table not found to delete");
                return 0;
            }

            var imagesLinkedWithTheColor = await imagesRepository
                .FindQueryable(i => i.IdProdusCuCuloare == colorLinkedWithProduct.IdProdusCuCuloare)
                .ToListAsync();

            foreach (var imageToDelete in imagesLinkedWithTheColor)
            {
                var dirNameInS3 = imageToDelete.FisierInBucket;
                var imageName = imageToDelete.CaleImagine;
                var keyName = $"images/{dirNameInS3}/{imageName}";
                var response = await _bucketAcces.DeleteFromBucket(keyName);

                if (response == 1)
                {
                    _logger.LogInformation("Succesfully deleted the images related to the color");
                }

                _logger.LogInformation("The product had no images associated");

            }

            await colorOnProductsRepository.DeleteAsync(colorLinkedWithProduct);
            await _unitOfWork.CommitTransactionAsync(deleteTransaction);
            _logger.LogInformation("Color on product was deleted succesufully");
            return 1;
        }
        catch (Exception e)
        {
            if (deleteTransaction is not null)
            {
                _logger.LogInformation("An error occured, rolling back transaction");
                _logger.LogInformation($"{e.Message}");

                await _unitOfWork.RollBackTransactionAsync(deleteTransaction);
            }

            return -1;
        }
    }

    public async Task<int> DeleteImageOnProduct(string codProdus, string numeCuloare, 
        string codCuloare, string caleImagine, string fisierInBucket)
    {
        var productRepository = _unitOfWork.Repository<Produse>();
        var colorRepository = _unitOfWork.Repository<Culori>();
        var colorCodesRepository = _unitOfWork.Repository<CodCulori>();
        var colorOnProductsRepository = _unitOfWork.Repository<ProduseCuCulori>();
        var imagesRepository = _unitOfWork.Repository<Imagini>();

        IDbContextTransaction? deleteTransaction = null;
        try
        {
          
            deleteTransaction = await _unitOfWork.BeginTransactionAsync();
            _logger.LogInformation($"{codProdus}  <- cod produs");
            var product = await productRepository
                .FindQueryable(p => p.CodProdus == codProdus)
                .FirstOrDefaultAsync();

            if (product is null)
            {
                _logger.LogInformation("Product not found to delete the image on it");
                return 0;
            }
            
            if (product.IsLocked)
            {
                // Check product locked
                return -3;
            }

            var colorCodeAssociatedWithColor = await colorCodesRepository
                .FindQueryable(cc => cc.CodCuloare == codCuloare)
                .FirstOrDefaultAsync();

            if (colorCodeAssociatedWithColor is null)
            {
                _logger.LogInformation("Color code  not found image deletion");
                return 0;
            }
            
            var colorToDeleteOnProduct = await colorRepository
                .FindQueryable(c => EF.Functions.JsonUnquote(EF.Functions.JsonExtract<string>(c.NumeCuloareJson , "$.culoare_ro")) == numeCuloare && c.IdCodCuloare == colorCodeAssociatedWithColor.IdCodCuloare)
                .FirstOrDefaultAsync();

            if (colorToDeleteOnProduct is null)
            {
                _logger.LogInformation("Color not found for image deletion");
                return 0;
            }

            var colorLinkedWithProduct = await colorOnProductsRepository
                .FindQueryable(pd => pd.IdProdus == product.IdProdus &&
                                     pd.IdCuloare == colorToDeleteOnProduct.IdCuloare)
                .FirstOrDefaultAsync();
            
            if (colorLinkedWithProduct is null)
            {
                _logger.LogInformation("Color linked with product in join table not found to delete");
                return 0;
            }

            var imageToDelete = await imagesRepository
                .FindQueryable(i => i.IdProdusCuCuloare == colorLinkedWithProduct.IdProdusCuCuloare
                && i.CaleImagine == caleImagine && i.FisierInBucket == fisierInBucket)
                .FirstOrDefaultAsync();
        
            if (imageToDelete is null)
            {
                _logger.LogInformation("Image not found to delete");
                return 0;
            }
            
            var dirNameInS3 = imageToDelete.FisierInBucket;
            var imageName = imageToDelete.CaleImagine;
            var keyName = $"images/{dirNameInS3}/{imageName}";
            var response = await _bucketAcces.DeleteFromBucket(keyName);
            _logger.LogInformation(response == 1
                ? "Succesfully deleted the images related to the color"
                : "The product had no images associated");

            await imagesRepository.DeleteAsync(imageToDelete);
            await _unitOfWork.CommitTransactionAsync(deleteTransaction);
            _logger.LogInformation("Image on product was deleted succesufully");
            return 1;
        }
        catch (Exception e)
        {
            if (deleteTransaction is not null)
            {
                _logger.LogInformation("An error occured, rolling back transaction");
                _logger.LogInformation($"{e.Message}");

                await _unitOfWork.RollBackTransactionAsync(deleteTransaction);
            }

            return -1;
        }
    }

    public async Task<int> ToggleActivationStateInShop(string productCode, bool activation)
    {
        IDbContextTransaction? toggleTransaction = null; 
        try
        {
            toggleTransaction = await _unitOfWork.BeginTransactionAsync();
            var productRepository = _unitOfWork.Repository<Produse>();

            var productToBeToggled = await productRepository
                .FindQueryable(p => p.CodProdus == productCode)
                .FirstOrDefaultAsync();


            if (productToBeToggled is null)
            {
                _logger.LogInformation("Product that needs to be toggled is not in the database");
                return -1;
            }
            
            if (productToBeToggled.IsLocked)
            {
                // Check product locked
                return -3;
            }

            productToBeToggled.ActivInMagazin = activation;

            await productRepository.UpdateAsync(productToBeToggled);
            await _unitOfWork.CommitTransactionAsync(toggleTransaction);

            return 1;
        }
        catch (Exception ex)
        {
            if (toggleTransaction != null)
            {
                _logger.LogInformation("Rolling toggling transaction back");
                _logger.LogError($"Error message is \n {ex.Message}");
                await _unitOfWork.RollBackTransactionAsync(toggleTransaction);
            }
            return -1;
        }
       
    }

    public async Task<int> DeleteSelectedProducts(BulkOperationsDto bulkOperationsDto)
    {
        IDbContextTransaction? deletingTransaction = null;
        try
        {
            deletingTransaction = await _unitOfWork.BeginTransactionAsync();
            var productRepository = _unitOfWork.Repository<Produse>();
            
            var stringCollection = bulkOperationsDto.SelectedItemsToDoBulkOperations!.Select(obj => obj.ToString()).ToList();

            var productsToDelete = await productRepository
                .FindQueryable(p => stringCollection.Contains(p.CodProdus))
                .Include(p => p.ComenziProduse)
                .ToListAsync();

            var productsToDeleteFromDb = productsToDelete
                .Select(a => a)
                .Where(p => p.ComenziProduse.IsNullOrEmpty())
                .ToList();
            
            var productsToSoftDeleteFromDb = productsToDelete
                .Select(a => a)
                .Where(p => !p.ComenziProduse.IsNullOrEmpty())
                .ToList();
            
            

            var isAnyProductLocked = productsToDelete.Any(p => p.IsLocked);

            if (isAnyProductLocked)
            {
                throw new DbUpdateException($"One of the product is locked . Cannot delete anything");
            }

            if (!productsToDeleteFromDb.IsNullOrEmpty())
            {
                await productRepository.DeleteRangeAsync(productsToDeleteFromDb);
            }

            if (!productsToSoftDeleteFromDb.IsNullOrEmpty())
            {
                foreach (var product in productsToSoftDeleteFromDb)
                {
                    product.IsDeleted = true;
                    product.ActivInMagazin = false;
                }
                
                await productRepository.UpdateRangeAsync(productsToSoftDeleteFromDb);
            }
            
            // Commit the transaction
            await _unitOfWork.CommitTransactionAsync(deletingTransaction);
            return 1;
        }
        catch (Exception ex)
        {
            if (deletingTransaction != null)
            {
                _logger.LogInformation("Rolling toggling transaction back");
                _logger.LogError($"Error message is \n {ex.Message}");
                await _unitOfWork.RollBackTransactionAsync(deletingTransaction);
            }
            if (ex is not DbUpdateException) return -1;
            
            _logger.LogError("Product is being bought . Cannot update");
            return -3;
        }
    }

    public async Task<int> ActivateSelectedProducts(BulkOperationsDto bulkOperationsDto)
    {
        IDbContextTransaction? updatingTransaction = null;
        try
        {
            updatingTransaction = await _unitOfWork.BeginTransactionAsync();
            var productRepository = _unitOfWork.Repository<Produse>();
            
            var stringCollection = bulkOperationsDto.SelectedItemsToDoBulkOperations!.Select(obj => obj.ToString()).ToList();

            var productsToActivate = await productRepository
                .FindQueryable(p => stringCollection.Contains(p.CodProdus))
                .ToListAsync();

            var isAnyProductLocked = productsToActivate.Any(p => p.IsLocked);

            if (isAnyProductLocked)
            {
                throw new DbUpdateException($"One of the product is locked . Cannot activate it");
            }

            if (!productsToActivate.IsNullOrEmpty())
            {
                foreach (var produs in productsToActivate)
                {
                    produs.ActivInMagazin = true;
                }
            }

            // Commit the transaction
            await _unitOfWork.CommitTransactionAsync(updatingTransaction);
            return 1;
        }
        catch (Exception ex)
        {
            if (updatingTransaction != null)
            {
                _logger.LogInformation("Rolling toggling transaction back");
                _logger.LogError($"Error message is \n {ex.Message}");
                await _unitOfWork.RollBackTransactionAsync(updatingTransaction);
            }

            if (ex is not DbUpdateException) return -1;
            
            _logger.LogError("Product is being bought . Cannot update");
            return -3;

        }
    }

    
    // DE MODIFICAT DE AICI IN JOS 
    public async Task<ProductOptionsForComboBox?> GetProductTypes()
    {
        var manuFacturersDto = await _unitOfWork.Repository<Producatori>()
            .GetSimpleQueryable()
            .GroupBy(prod => prod.NumeProducator)
            .Select(prod => prod.Key)
            .ToListAsync();
        // JSON
        var productCategoriesJson = await _unitOfWork.Repository<TipuriProduse>()
            .GetSimpleQueryable()
            .GroupBy(tip => tip.CategorieJson)
            .Select(g => g.Key)
            .ToListAsync();
        
        // JSON
        var colorsJson = await _unitOfWork.Repository<Culori>()
            .GetSimpleQueryable()
            .GroupBy(culoare => culoare.NumeCuloareJson)
            .Select(g => g.Key)
            .ToListAsync();
    
        var colorCodes = await _unitOfWork.Repository<CodCulori>()
            .GetSimpleQueryable()
            .GroupBy(codCuloare => codCuloare.CodCuloare)
            .Select(g => g.Key)
            .ToListAsync();
    
        var heights = await _unitOfWork.Repository<Dimensiuni>()
            .GetSimpleQueryable()
            .GroupBy(dimensions => dimensions.Lungime)
            .Select(g => g.Key)
            .ToListAsync();
    
        var widths = await _unitOfWork.Repository<Dimensiuni>()
            .GetSimpleQueryable()
            .GroupBy(dimensions => dimensions.Latime)
            .Select(g => g.Key)
            .ToListAsync();
    
        var bedRecommendations = await _unitOfWork.Repository<Dimensiuni>()
            .GetSimpleQueryable()
            .GroupBy(dimensions => dimensions.RecomandarePat)
            .Select(g => g.Key)
            .ToListAsync();
        
        // JSON
        var productTypesJson = await _unitOfWork.Repository<Produse>()
            .GetSimpleQueryable()
            .GroupBy(types => types.TipulProdusuluiJson)
            .Select(g => g.Key)
            .ToListAsync();

        var directoriesInS3Bucket = await _bucketAcces
            .ListDirsFromBuckets();
    
        var productOptions = new ProductOptionsForComboBox
        {
            ProductCategoriesForBoxJson = productCategoriesJson,
            LatimiForBox = widths,
            LungimiForBox = heights,
            RecomandariForBox = bedRecommendations,
            CoduriCuloriForBox = colorCodes,
            CuloriForBoxJson = colorsJson,
            DirectoriesInBucket = directoriesInS3Bucket!,
            ManuFacturersDto = manuFacturersDto,
            ProductTypesJson = productTypesJson
        };
    
        return productOptions;
    }

    public async Task<IList<ProductInfo>?> GetProductCodesAndNames()
    {
        var productsRepository = _unitOfWork.Repository<Produse>();

        var namesOfProducts = await productsRepository.GetSimpleQueryable()
            .Select(p => new ProductInfo
            {
                NumeProdus = p.NumeProdusJson.NumeRomana,
                CodProdus = p.CodProdus,
                NumeProdusJson = p.NumeProdusJson
            })
            .ToListAsync();
         
        return namesOfProducts;

    }

    public async Task<ProductTypesAndSubCategories> GetProductTypesAndSubCategories()
    {
        
        // JSON
        var productTypesJson = await _cache.GetOrCreateAsync("productTypesJson", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
            var productTypesJsonLocal = await _unitOfWork.Repository<Produse>()
                .GetSimpleQueryable()
                .GroupBy(types => types.TipulProdusuluiJson)
                .Select(g => g.Key)
                .ToListAsync();

            return productTypesJsonLocal;
        });
        

        return new ProductTypesAndSubCategories
        {
            ProductTypesJson = productTypesJson!
        };


    }

    public async Task<IList<ProductsListingForUsers>> GetProductsForUsers(
        int? pageNumber, List<string>? productTypes,List<string>? productCategories ,List<string>? productColors,
        List<string>? productDimensions, List<decimal>? productPrices, bool? reverseFace,
        string currency = "RON")
    {
        var onlyLettersAndSpacesBetween = ValidateQueryParams();
        var onlyNumbers = ValidateNumberesOnly();
        if (!productTypes.IsNullOrEmpty())
        {
            productTypes = productTypes!.Where(type => onlyLettersAndSpacesBetween.IsMatch(type)).ToList();
        }
        if (!productCategories.IsNullOrEmpty())
        {
            productCategories = productCategories!.Where(type => onlyLettersAndSpacesBetween.IsMatch(type)).ToList();
        }
        if (!productColors.IsNullOrEmpty())
        {
            productColors = productColors!.Where(color => onlyLettersAndSpacesBetween.IsMatch(color)).ToList();
        }
        if (!productDimensions.IsNullOrEmpty())
        {
            productDimensions = productDimensions!.Where(dimension => onlyLettersAndSpacesBetween.IsMatch(dimension)).ToList();
        }
        if (!productPrices.IsNullOrEmpty())
        {
           productPrices = productPrices!.Where(price => onlyNumbers.IsMatch(price.ToString())).ToList();
        }
        
        
        if (currency == "EUR")
        {
            productPrices![0] = UserHelpers.ConvertCurrency("RON", "EUR", productPrices[0], 0);
            productPrices[1] = UserHelpers.ConvertCurrency("RON", "EUR", productPrices[1], 0);

        }
        
        var timer = new Stopwatch();
        timer.Start();
        var productsRepository = _unitOfWork.Repository<Produse>();
       
        var mainQuery = productsRepository
            .GetSimpleQueryable()
            .Include(p => p.PProduseCuCulori!)
            .ThenInclude(p => p.Culoare)
            .Include(p => p.PProduseCuCulori!)
            .ThenInclude(p => p.ImagProduseCuCulori)
            .Include(p => p.PProduseCuDimensiuni!)
            .ThenInclude(p => p.PdDimensiune)
            .Where(product => productTypes.IsNullOrEmpty() || productTypes!.Contains(EF.Functions
                .JsonUnquote(EF.Functions.JsonExtract<string>(product.TipulProdusuluiJson, "$.tip_ro")).Trim()))
            .Where(product => productColors.IsNullOrEmpty() ||
                              product.PProduseCuCulori!.Any(culoare =>
                                  productColors!.Contains(EF.Functions
                                      .JsonUnquote(EF.Functions.JsonExtract<string>(culoare.Culoare.NumeCuloareJson,
                                          "$.culoare_ro")).Trim())))
            .Where(product => productCategories.IsNullOrEmpty() ||
                              product.PTipuriPeProduse!.Any(tip =>
                                  productCategories!.Contains(EF.Functions
                                      .JsonUnquote(EF.Functions.JsonExtract<string>(tip.TppTipProdus.CategorieJson,
                                          "$.categorie_ro")).Trim())))
            .Where(product =>
                productDimensions.IsNullOrEmpty() ||
                product.PProduseCuDimensiuni!.Count == 0 ||
                product.PProduseCuDimensiuni!.Any(dimensiune =>
                    Convert.ToInt16(dimensiune.PdDimensiune!.Lungime) >= Convert.ToInt16(productDimensions![0]) &&
                    Convert.ToInt16(dimensiune.PdDimensiune!.Lungime) <= Convert.ToInt16(productDimensions[1]) &&
                    Convert.ToInt16(dimensiune.PdDimensiune!.Latime) >= Convert.ToInt16(productDimensions[2]) &&
                    Convert.ToInt16(dimensiune.PdDimensiune!.Latime) <= Convert.ToInt16(productDimensions[3])
                )
            )
            .Where(product => productPrices.IsNullOrEmpty() || 
                              (product.PProduseCuDimensiuni != null && product.PProduseCuDimensiuni.Count > 0
                                  ? product.PProduseCuDimensiuni!.Any(dimension =>
                                      currency == "EUR" 
                                          ? dimension.PretRedus != 0 
                                              ? dimension.PretRedus * (decimal)0.2 >= Convert.ToDecimal(productPrices![0]) && dimension.PretRedus  * (decimal)0.2  <= Convert.ToDecimal(productPrices[1])
                                              : dimension.Pret * (decimal)0.2 >= Convert.ToDecimal(productPrices![0]) && dimension.Pret * (decimal)0.2 <= Convert.ToDecimal(productPrices[1])
                                          : dimension.PretRedus != 0 
                                              ? dimension.PretRedus  >= Convert.ToDecimal(productPrices![0]) && dimension.PretRedus <= Convert.ToDecimal(productPrices[1])
                                              : dimension.Pret  >= Convert.ToDecimal(productPrices![0]) && dimension.Pret <= Convert.ToDecimal(productPrices[1])
                                  )
                                  : currency == "EUR" 
                                      ? product.PretDeBazaRedus > 0 
                                          ? product.PretDeBazaRedus * (decimal)0.2 >= Convert.ToDecimal(productPrices![0]) && product.PretDeBazaRedus  * (decimal)0.2 <= Convert.ToDecimal(productPrices[1])
                                          : product.PretDeBaza * (decimal)0.2 >= Convert.ToDecimal(productPrices![0]) && product.PretDeBaza * (decimal)0.2 <= Convert.ToDecimal(productPrices[1]) 
                                      : product.PretDeBazaRedus > 0 
                                          ? product.PretDeBazaRedus >= Convert.ToDecimal(productPrices![0]) && product.PretDeBazaRedus <= Convert.ToDecimal(productPrices[1])
                                          : product.PretDeBaza >= Convert.ToDecimal(productPrices![0]) && product.PretDeBaza <= Convert.ToDecimal(productPrices[1]) 
                              )
            )
            .Where(product => reverseFace == null || product.FataReversibila == reverseFace)
            .Where(product => !product.IsDeleted && product.ActivInMagazin);

        var totalProductsFiltered = await mainQuery.CountAsync();

        var takePaginatedProducts = await mainQuery
        .OrderBy(product => product.TipulProdusuluiJson)
        .Skip((pageNumber ?? 0) * PageSize)
        .Take(PageSize)
        .Select(product => new ProductsListingForUsers
        {
            CodProdusDto = product.CodProdus,
            NumeProdusDto = currency == "RON" ?  product.NumeProdusJson.NumeRomana : product.NumeProdusJson.NumeEngleza ,
            // NumeProdusJsonDto = product.NumeProdusJson,
            TipulProdusuluiDto = currency == "RON" ?  product.TipulProdusuluiJson.TipProdusRomana : product.TipulProdusuluiJson.TipProdusEngleza ,
            // TipulProdusuluiJsonDto = product.TipulProdusuluiJson,
            PretBazaDto = currency == "EUR" ? UserHelpers.ConvertCurrency("RON" , "EUR" ,product.PretDeBaza , 0 ) : product.PretDeBaza,
            PretBazaRedusDto = currency == "EUR" ? UserHelpers.ConvertCurrency("RON" , "EUR" , product.PretDeBazaRedus , 0 ) : product.PretDeBazaRedus,
            DimensiuniProduseDto = product.PProduseCuDimensiuni!
                .OrderByDescending(dimensiune => dimensiune.Pret)
                .Select(dimensiune => new DimensiuniDto
                {
                    LungimeDto = dimensiune.PdDimensiune!.Lungime,
                    LatimeDto = dimensiune.PdDimensiune!.Latime,
                    PretDto = currency == "EUR" ? UserHelpers.ConvertCurrency("RON" , "EUR" , dimensiune.Pret , 0 ) : dimensiune.Pret,
                    PretRedusDto = currency == "EUR" ? UserHelpers.ConvertCurrency("RON" , "EUR" , dimensiune.PretRedus , 0 ) : dimensiune.PretRedus,
                })
                .ToList(),
            TotalProducts = totalProductsFiltered,
            CuloriProdusDto = product.PProduseCuCulori!
                .Select( culori => new ColorsWithImages
                {
                    NumeCuloareDto = currency == "RON" ?  culori.Culoare.NumeCuloareJson.CuloareRomana :  culori.Culoare.NumeCuloareJson.CuloareEngleza ,
                    ImaginiProdusDto = culori.ImagProduseCuCulori!
                        .Select( image => new ImagesDtoForUsers
                        {
                            CaleImagineDto = image.CaleImagine!,
                            FisierInBucketDto = image.FisierInBucket,
                            PresignedUrl = null
                        }).ToList()
                }).ToList(),
            ReviewsInfoGeneral = new ReviewsInfoForQuickDisplay
            {
                TotalReviews = product.ProductReviews!.Count,
                AverageRating = product.ProductReviews.Count > 0
                    ? Math.Round(product.ProductReviews.Average(avg => avg.NumarStele), 1)
                    : 0.0,
            }
        }).AsSplitQuery()
        .ToListAsync();



        foreach (var product in takePaginatedProducts)
        {
            foreach (var color in product.CuloriProdusDto)
            {
               
                if (color.ImaginiProdusDto.IsNullOrEmpty()) continue;
                foreach (var image in color.ImaginiProdusDto!)
                {
                    image.PresignedUrl = await _bucketAcces.GenerateUrl(image.CaleImagineDto, image.FisierInBucketDto);
                }

            }
        }
        
        timer.Stop();
        _logger.LogWarning($"Elapsed : {timer.ElapsedMilliseconds}");

        return takePaginatedProducts;
    }
    
    public async Task<ProductsFilterOptions> FilterOptions(string currency = "RON")
    {
        // var productTypes = await _unitOfWork.Repository<Produse>()
        //     .GetSimpleQueryable()
        //     .GroupBy(types => types.TipulProdusului)
        //     .Select(g => g.Key.ToUpper())
        //     .ToListAsync();
        
        var productTypesJson = await _unitOfWork.Repository<Produse>()
            .GetSimpleQueryable()
            .GroupBy(types => types.TipulProdusuluiJson)
            .Select(g => g.Key)
            .ToListAsync();
        
        // var colors = await _unitOfWork.Repository<Culori>()
        //     .GetSimpleQueryable()
        //     .GroupBy(culoare => culoare.NumeCuloare)
        //     .Select(g => g.Key.ToUpper())
        //     .ToListAsync();
        
        var colorsJson = await _unitOfWork.Repository<Culori>()
            .GetSimpleQueryable()
            .GroupBy(culoare => culoare.NumeCuloareJson)
            .Select(g => g.Key)
            .ToListAsync();

        var categoriesJson = await _unitOfWork.Repository<TipuriProduse>()
            .GetSimpleQueryable()
            .GroupBy(category => category.CategorieJson)
            .Select(g => g.Key)
            .ToListAsync();
        
        
        var pricesRange = new List<decimal>(2);
        if (currency == "EUR")
        {
            pricesRange.Add(0);
            pricesRange.Add(UserHelpers.ConvertCurrency("RON" , "EUR" , 2000 , 0));
        }
        else
        {
            pricesRange.Add(0);
            pricesRange.Add(2000);
        }
       


        return new ProductsFilterOptions
        {
            // FilterColors = colors.ToImmutableHashSet(),
            // FilterProductTypes = productTypes.ToImmutableHashSet(),
            FilterColorsJson = colorsJson.ToImmutableHashSet(),
            FilterProductTypesJson = productTypesJson.ToImmutableHashSet(),
            FilterProductCategoriesJson = categoriesJson.ToImmutableHashSet(),
            PricesRange = pricesRange.ToImmutableHashSet(),

        };
    }

    public async Task<KeyValuePair<int , ProductPageForUser?>> GetProductPage(string codProdus,string tipProdus,string currency = "RON")
    {
        var productsRepository = _unitOfWork.Repository<Produse>();
       
        try
        {
            
            if (string.Equals(tipProdus.ToLower(), "perdea") || string.Equals(tipProdus.ToLower(), "draperie"))
            {
            
              
                var ringTypesDto = await _cache.GetOrCreateAsync($"ringTypes", async entry =>
                {
                    var ringTypesRepository = _unitOfWork.Repository<InelePrindere>();
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);

                    // If cache does not exist, run the retrieval and mapping logic
                    var rings = await ringTypesRepository.FindQueryable(ring => EF.Functions.JsonUnquote(EF.Functions.JsonExtract<string>(ring.CuloareInelJson , "$.culoare_ro")) != "STAN" && ring.IsDeleted == false).ToListAsync();
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
                    var rejanse = await rejansaRepository.FindQueryable(gallery => EF.Functions.JsonUnquote(EF.Functions.JsonExtract<string>(gallery.NumeTipGalerieJson , "$.nume_ro")) != "STAN" && gallery.IsDeleted == false).ToListAsync();
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
                    var linings = await liningTypesRepository.FindQueryable(lineType => EF.Functions.JsonUnquote(EF.Functions.JsonExtract<string>(lineType.NumeTipLinieJson , "$.nume_ro")) != "STAN" && lineType.IsDeleted == false).ToListAsync();
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

           
                var productForUser = await  productsRepository
                    .GetSimpleQueryable()
                    .Include(p => p.Producator)
                    .Include(p => p.PProduseCuCulori!)
                        .ThenInclude(p => p.Culoare)
                            .ThenInclude(p => p.CodCuloare)
                    .Include(p => p.PTipuriPeProduse)
                    .Include(p => p.ProductReviews)
                    .Where(product => product.CodProdus == codProdus.ToUpper() 
                                      && EF.Functions.JsonUnquote(EF.Functions.JsonExtract<string>(product.TipulProdusuluiJson , "$.tip_ro")) == tipProdus.ToLower()
                                      && !product.IsDeleted
                                      && product.ActivInMagazin)
                    .Select(product => new ProductPageForUser
                    {
                        IdProdus = product.IdProdus,
                        CodProdusDto = product.CodProdus,
                        DescriereDto = currency == "RON" ?  product.DescriereJson!.DescriereRomana : product.DescriereJson!.DescriereEngleza,
                        NumeProdusDto = currency == "RON" ?  product.NumeProdusJson.NumeRomana : product.NumeProdusJson.NumeEngleza,
                        CompozitieDto = currency == "RON" ?  product.CompozitieJson!.CompozitieRomana : product.CompozitieJson!.CompozitieEngleza,
                        TvaDto = product.Tva,
                        IngrijireDto = currency == "RON" ?  product.IngrijireJson!.IngrijireRomana: product.IngrijireJson!.IngrijireEngleza,
                        FataReversibilaDto = product.FataReversibila,
                        TipulProdusuluiDto =  currency == "RON" ?  product.TipulProdusuluiJson.TipProdusRomana : product.TipulProdusuluiJson.TipProdusEngleza ,
                        TipulProdusuluiJsonDto =  product.TipulProdusuluiJson ,
                        NumeProducatorDto = product.Producator == null ? null : product.Producator.NumeProducator,
                        PretBazaDto = currency == "EUR" ? UserHelpers.ConvertCurrency("RON" , "EUR" ,product.PretDeBaza , 0 ) : product.PretDeBaza,
                        PretBazaRedusDto = currency == "EUR" ? UserHelpers.ConvertCurrency("RON" , "EUR" , product.PretDeBazaRedus , 0 ) : product.PretDeBazaRedus,
                        InaltimeMaximaDto = product.InaltimeMaxima,
                        TipuriInele = ringTypesDto!,
                        TipuriRejansa = rejanseTypesDto!,
                        TipuriLinie = liningTypesDto!,
                        CuloriProdus = product.PProduseCuCulori!
                            .Select(pc => new CuloriDto
                            {
                                IdCuloare = pc.IdCuloare,
                                NumeCuloareDto = currency == "RON" ?  pc.Culoare.NumeCuloareJson.CuloareRomana :  pc.Culoare.NumeCuloareJson.CuloareEngleza,
                                NumeCuloareJsonDto = pc.Culoare.NumeCuloareJson,
                                CodCuloareDto = pc.Culoare.CodCuloare.CodCuloare!,
                                JustAdded = false,
                                ImaginiProdusDto = pc.ImagProduseCuCulori != null 
                                    ? pc.ImagProduseCuCulori
                                        .Select(imag => new ImagesDto
                                        {
                                            CaleImagineDto = imag.CaleImagine!,
                                            FisierInBucketDto = imag.FisierInBucket,
                                            PresignedUrl = null,
                                        }).ToList()
                                    : null 
                            }).ToList(),
                        ReviewsProdus = product.ProductReviews!
                            .Select(reviews => new ReviewsDto
                            {
                                NumarSteleDto = reviews.NumarStele,
                                TextRecenzie = reviews.TextRecenzie,
                                NumeClient = reviews.Cont.Nume,
                                PrenumeClient = reviews.Cont.Prenume,
                                UsernameContClient = reviews.Cont.Username!,
                            }).ToList(),
                        CategoriiProdus = currency == "RON"
                            ? product.PTipuriPeProduse!
                                .Select(type => EF.Functions.JsonUnquote(EF.Functions.JsonExtract<string>(type.TppTipProdus.CategorieJson , "$.categorie_ro")))
                                .ToList()
                            : product.PTipuriPeProduse!
                                .Select(type => EF.Functions.JsonUnquote(EF.Functions.JsonExtract<string>(type.TppTipProdus.CategorieJson , "$.categorie_en")))
                                .ToList(),
                        ReviewsGeneral = new ReviewsInfo
                        {
                            TotalReviews = product.ProductReviews!.Count,
                            AverageRating = product.ProductReviews.Count > 0
                                ? Math.Round(product.ProductReviews.Average(avg => avg.NumarStele), 1)
                                : 0.0,
                            FiveStarsReviews = product.ProductReviews!.Count(fiveStars => fiveStars.NumarStele == 5),
                            FourStarsReviews = product.ProductReviews!.Count(fiveStars => fiveStars.NumarStele == 4),
                            ThreeStarsReviews = product.ProductReviews!.Count(fiveStars => fiveStars.NumarStele == 3),
                            TwoStarsReviews = product.ProductReviews!.Count(fiveStars => fiveStars.NumarStele == 2),
                            OneStarReviews = product.ProductReviews!.Count(fiveStars => fiveStars.NumarStele == 1)
                        }
                    }).AsSplitQuery()
                    .FirstAsync();
                
                
                
                foreach (var color in productForUser.CuloriProdus)
                {
                    if (color.ImaginiProdusDto.IsNullOrEmpty()) continue;
                    foreach (var image in color.ImaginiProdusDto!)
                    {
                        image.PresignedUrl = await _bucketAcces.GenerateUrl(image.CaleImagineDto, image.FisierInBucketDto);
                    }

                }
                
                
                return new KeyValuePair<int, ProductPageForUser?>(1, productForUser);
                
            }
            
            else
            {
                var productForUser = await productsRepository
                    .GetSimpleQueryable()
                    .Include(p => p.Producator)
                    .Include(p => p.PProduseCuDimensiuni!)
                    .Include(p => p.PProduseCuCulori!)
                        .ThenInclude(p => p.Culoare)
                            .ThenInclude(p => p.CodCuloare)
                    .Include(p => p.PTipuriPeProduse)
                    .Include(p => p.ProductReviews)
                    .Where(product => product.CodProdus == codProdus.ToUpper() 
                                                  && EF.Functions.JsonUnquote(EF.Functions.JsonExtract<string>(product.TipulProdusuluiJson , "$.tip_ro")) == tipProdus.ToLower()
                                                  && !product.IsDeleted
                                                  && product.ActivInMagazin
                                      )
                    .Select(product => new ProductPageForUser
                    {
                        IdProdus = product.IdProdus,
                        CodProdusDto = product.CodProdus,
                        DescriereDto = currency == "RON" ?  product.DescriereJson!.DescriereRomana : product.DescriereJson!.DescriereEngleza,
                        NumeProdusDto = currency == "RON" ?  product.NumeProdusJson.NumeRomana : product.NumeProdusJson.NumeEngleza,
                        CompozitieDto = currency == "RON" ?  product.CompozitieJson!.CompozitieRomana : product.CompozitieJson!.CompozitieEngleza,
                        TvaDto = product.Tva,
                        IngrijireDto = currency == "RON" ?  product.IngrijireJson!.IngrijireRomana: product.IngrijireJson!.IngrijireEngleza,
                        FataReversibilaDto = product.FataReversibila,
                        TipulProdusuluiDto =  currency == "RON" ?  product.TipulProdusuluiJson.TipProdusRomana : product.TipulProdusuluiJson.TipProdusEngleza ,
                        TipulProdusuluiJsonDto =  product.TipulProdusuluiJson ,
                        InaltimeMaximaDto = product.InaltimeMaxima,
                        NumeProducatorDto = product.Producator == null ? null : product.Producator.NumeProducator,
                        PretBazaDto = currency == "EUR" ? UserHelpers.ConvertCurrency("RON" , "EUR" ,product.PretDeBaza , 0 ) : product.PretDeBaza,
                        PretBazaRedusDto = currency == "EUR" ? UserHelpers.ConvertCurrency("RON" , "EUR" , product.PretDeBazaRedus , 0 ) : product.PretDeBazaRedus,
                        DimensiuniProdus = product.PProduseCuDimensiuni!
                            .OrderByDescending(dimensiune => dimensiune.Pret)
                            .Select(dimensiune => new DimensiuniDto
                            {
                                IdDimensiune = dimensiune.IdDimensiune,
                                LungimeDto = dimensiune.PdDimensiune!.Lungime,
                                LatimeDto = dimensiune.PdDimensiune!.Latime,
                                RecomandarePat = dimensiune.PdDimensiune!.RecomandarePat!,
                                PretDto = currency == "EUR" ? UserHelpers.ConvertCurrency("RON" , "EUR" , dimensiune.Pret , 0 ) : dimensiune.Pret,
                                PretRedusDto = currency == "EUR" ? UserHelpers.ConvertCurrency("RON" , "EUR" , dimensiune.PretRedus , 0 ) : dimensiune.PretRedus,
                            })
                            .ToList(),
                        CuloriProdus = product.PProduseCuCulori!
                            .Select(pc => new CuloriDto
                            {
                                IdCuloare = pc.IdCuloare,
                                NumeCuloareDto = currency == "RON" ?  pc.Culoare.NumeCuloareJson.CuloareRomana :  pc.Culoare.NumeCuloareJson.CuloareEngleza,    
                                NumeCuloareJsonDto = pc.Culoare.NumeCuloareJson,
                                CodCuloareDto = pc.Culoare.CodCuloare.CodCuloare!,
                                JustAdded = false,
                                ImaginiProdusDto = pc.ImagProduseCuCulori != null 
                                    ? pc.ImagProduseCuCulori
                                        .Select(imag => new ImagesDto
                                        {
                                            CaleImagineDto = imag.CaleImagine!,
                                            FisierInBucketDto = imag.FisierInBucket,
                                            PresignedUrl = null,
                                            IdProdusCuCuloareDto = 0,
                                            JustAdded = false
                                        }).ToList()
                                    : null 
                            }).ToList(),
                        ReviewsProdus = product.ProductReviews!
                            .Select(reviews => new ReviewsDto
                            {
                                NumarSteleDto = reviews.NumarStele,
                                TextRecenzie = reviews.TextRecenzie,
                                NumeClient = reviews.Cont.Nume,
                                PrenumeClient = reviews.Cont.Prenume,
                                UsernameContClient = reviews.Cont.Username!,
                            }).ToList(),
                        CategoriiProdus = currency == "RON"
                            ? product.PTipuriPeProduse!
                                .Select(type => EF.Functions.JsonUnquote(EF.Functions.JsonExtract<string>(type.TppTipProdus.CategorieJson , "$.categorie_ro")))
                                .ToList()
                            : product.PTipuriPeProduse!
                                .Select(type => EF.Functions.JsonUnquote(EF.Functions.JsonExtract<string>(type.TppTipProdus.CategorieJson , "$.categorie_en")))
                                .ToList(),
                        ReviewsGeneral = new ReviewsInfo
                        {
                            TotalReviews = product.ProductReviews!.Count,
                            AverageRating = product.ProductReviews.Count > 0
                                ? Math.Round(product.ProductReviews.Average(avg => avg.NumarStele), 1)
                                : 0.0,
                            FiveStarsReviews = product.ProductReviews!.Count(fiveStars => fiveStars.NumarStele == 5),
                            FourStarsReviews = product.ProductReviews!.Count(fiveStars => fiveStars.NumarStele == 4),
                            ThreeStarsReviews = product.ProductReviews!.Count(fiveStars => fiveStars.NumarStele == 3),
                            TwoStarsReviews = product.ProductReviews!.Count(fiveStars => fiveStars.NumarStele == 2),
                            OneStarReviews = product.ProductReviews!.Count(fiveStars => fiveStars.NumarStele == 1)
                        }
                    }).AsSplitQuery()
                    .FirstAsync();
                
                 
                foreach (var color in productForUser.CuloriProdus)
                {
                    if (color.ImaginiProdusDto.IsNullOrEmpty()) continue;
                    foreach (var image in color.ImaginiProdusDto!)
                    {
                        image.PresignedUrl = await _bucketAcces.GenerateUrl(image.CaleImagineDto, image.FisierInBucketDto);
                    }

                }
                

                return new KeyValuePair<int, ProductPageForUser?>(1, productForUser);

            }
        }
        // de rulat magaoaia asta de functie
        catch (Exception e)
        {
            if (e.InnerException is ArgumentNullException)
            {
                _logger.LogError("Product does not exist anymore / Or has been deactivated");
                return new KeyValuePair<int, ProductPageForUser?>(1,null);
            }
            Console.WriteLine(e.Message);
            _logger.LogError("General error occured");
            return new KeyValuePair<int, ProductPageForUser?>(0,null);
                
        }
        
    }

    public async Task<IList<MostViewedProduct>> GetMostViewedProducts(string currency = "RON")
    {

        var mostViewedProductsCached = await _cache.GetOrCreateAsync($"mostViewedProducts_{currency}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(20);
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
            const string lowerInterval = "2022-01-01";
            const string upperInterval = "today";

            var usersPerPage = new RunReportRequest
            {
                Property = "properties/" + propertyId,
                Dimensions =
                {
                    new Dimension { Name = "pagePath" },
                },
                Metrics =
                {
                    new Metric { Name = "screenPageViews" }
                },
                DateRanges = { new DateRange { StartDate = lowerInterval, EndDate = upperInterval } },
                DimensionFilter = new FilterExpression
                {
                    AndGroup = new FilterExpressionList
                    {
                        Expressions =
                        {
                            new FilterExpression
                            {
                                Filter = new Filter
                                {
                                    FieldName = "pagePath",
                                    StringFilter = new Filter.Types.StringFilter
                                    {
                                        MatchType = Filter.Types.StringFilter.Types.MatchType.BeginsWith,
                                        Value = "/product"
                                    }
                                }
                            },
                            new FilterExpression
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
                            }
                        }
                    }
                }

            };

            var responseFromProductAndSetsViews = await client.RunReportAsync(usersPerPage);

            var sortedViews = responseFromProductAndSetsViews.Rows
                .Select(row => new MostPageViews
                {
                    PagePath = row.DimensionValues[0].Value,
                    Views = int.Parse(row.MetricValues[0].Value)
                })
                .OrderByDescending(view => view.Views) // Sort by views in descending order
                .Take(20) // Take the top 20
                .Select(view => view.PagePath.Split("/")[2])
                .ToList();


            var productsRepository = _unitOfWork.Repository<Produse>();
            
            var mostViewedProducts = await productsRepository
                .GetSimpleQueryable()
                .Where(p => sortedViews.Contains(p.CodProdus))
                .Select(p => new MostViewedProduct
                {
                    CodProdusDto = p.CodProdus,
                    NumeProdusDto = currency == "RON" ?  p.NumeProdusJson.NumeRomana : p.NumeProdusJson.NumeEngleza,
                    // NumeProdusJsonDto = p.NumeProdusJson,
                    TipulProdusuluiDto = currency == "RON" ?  p.TipulProdusuluiJson.TipProdusRomana : p.TipulProdusuluiJson.TipProdusEngleza,
                    TipulProdusuluiJsonDto = p.TipulProdusuluiJson,
                    PretBazaDto = currency == "RON"
                        ? p.PProduseCuDimensiuni!.Count == 0
                            ? p.PretDeBaza
                            : p.PProduseCuDimensiuni.Min(dim => dim.Pret)
                        : p.PProduseCuDimensiuni!.Count == 0
                            ? p.PretDeBaza * (decimal)0.2
                            : p.PProduseCuDimensiuni.Min(dim => dim.Pret) * (decimal)0.2,
                    PretBazaRedusDto = currency == "RON"
                        ? p.PProduseCuDimensiuni!.Count == 0
                            ? p.PretDeBazaRedus
                            : p.PProduseCuDimensiuni.Min(dim => dim.PretRedus)
                        : p.PProduseCuDimensiuni!.Count == 0
                            ? p.PretDeBazaRedus * (decimal)0.2
                            : p.PProduseCuDimensiuni.Min(dim => dim.PretRedus) * (decimal)0.2,
                    CuloriProdusDto = p.PProduseCuCulori!
                        .Where(pc => pc.ImagProduseCuCulori!.Count > 0)
                        .Take(1)
                        .Select(culori => new ColorsWithImages
                        {
                            NumeCuloareDto = currency == "RON" ?  culori.Culoare.NumeCuloareJson.CuloareRomana :  culori.Culoare.NumeCuloareJson.CuloareEngleza,
                            ImaginiProdusDto = culori.ImagProduseCuCulori!
                                .OrderBy(image => image.CaleImagine) 
                                .Take(1)
                                .Select(image => new ImagesDtoForUsers
                                {
                                    CaleImagineDto = image.CaleImagine ?? "",
                                    FisierInBucketDto = image.FisierInBucket,
                                    PresignedUrl = null
                                }).ToList()
                        }).ToList(),
                    CuloriProdusJsonDto = p.PProduseCuCulori!
                        .Where(pc => pc.ImagProduseCuCulori!.Count > 0)
                        .Take(1)
                        .Select(culori => new ColorsWithImages
                        {
                            NumeCuloareJsonDto = culori.Culoare.NumeCuloareJson,
                            // NumeCuloareDto = culori.Culoare.NumeCuloare,
                            ImaginiProdusDto = culori.ImagProduseCuCulori!
                                .OrderBy(image => image.CaleImagine) 
                                .Take(1)
                                .Select(image => new ImagesDtoForUsers
                                {
                                    CaleImagineDto = image.CaleImagine ?? "",
                                    FisierInBucketDto = image.FisierInBucket,
                                    PresignedUrl = null
                                }).ToList()
                        }).ToList()
                })
                .AsSplitQuery()
                .ToListAsync();


            foreach (var product in mostViewedProducts)
            {
                foreach (var color in product.CuloriProdusDto)
                {

                    if (color.ImaginiProdusDto.IsNullOrEmpty()) continue;
                    foreach (var image in color.ImaginiProdusDto!)
                    {
                        image.PresignedUrl =
                            await _bucketAcces.GenerateUrl(image.CaleImagineDto, image.FisierInBucketDto);
                    }

                }
            }

            return mostViewedProducts;
        });
        return mostViewedProductsCached!;
    }

    public async Task<IList<MostViewedProduct>> GetProductsThatAreNew(string currency = "RON")
    {
       
        var getNewProducts = await _cache.GetOrCreateAsync($"newProducts_{currency}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
            var productsRepository = _unitOfWork.Repository<Produse>();
            var newProducts = await productsRepository
                .GetSimpleQueryable()
                .Where(p => p.ActivInMagazin && !p.IsDeleted && p.AfiseazaInNoutati)
                .Take(15)
                .Select(p => new MostViewedProduct
                {
                    CodProdusDto = p.CodProdus,
                    NumeProdusDto = currency == "RON" ?  p.NumeProdusJson.NumeRomana : p.NumeProdusJson.NumeEngleza,
                    // NumeProdusJsonDto = p.NumeProdusJson,
                    TipulProdusuluiDto = currency == "RON" ?  p.TipulProdusuluiJson.TipProdusRomana : p.TipulProdusuluiJson.TipProdusEngleza,
                    // TipulProdusuluiJsonDto = p.TipulProdusuluiJson,
                    PretBazaDto = currency == "RON"
                        ? p.PProduseCuDimensiuni!.Count == 0
                            ? p.PretDeBaza
                            : p.PProduseCuDimensiuni.Min(dim => dim.Pret)
                        : p.PProduseCuDimensiuni!.Count == 0
                            ? p.PretDeBaza * (decimal)0.2
                            : p.PProduseCuDimensiuni.Min(dim => dim.Pret) * (decimal)0.2,
                    PretBazaRedusDto = currency == "RON"
                        ? p.PProduseCuDimensiuni!.Count == 0
                            ? p.PretDeBazaRedus
                            : p.PProduseCuDimensiuni.Min(dim => dim.PretRedus)
                        : p.PProduseCuDimensiuni!.Count == 0
                            ? p.PretDeBazaRedus * (decimal)0.2
                            : p.PProduseCuDimensiuni.Min(dim => dim.PretRedus) * (decimal)0.2,
                    CuloriProdusDto = p.PProduseCuCulori!
                        .Where(pc => pc.ImagProduseCuCulori!.Count > 0)
                        .Take(1)
                        .Select(culori => new ColorsWithImages
                        {
                            NumeCuloareDto = currency == "RON" ?  culori.Culoare.NumeCuloareJson.CuloareRomana :  culori.Culoare.NumeCuloareJson.CuloareEngleza,
                            ImaginiProdusDto = culori.ImagProduseCuCulori!
                                .OrderBy(image => image.CaleImagine)
                                .Take(1)
                                .Select(image => new ImagesDtoForUsers
                                {
                                    CaleImagineDto = image.CaleImagine ?? "",
                                    FisierInBucketDto = image.FisierInBucket,
                                    PresignedUrl = null
                                }).ToList()
                        }).ToList(),
                })
                .AsSplitQuery()
                .ToListAsync();


            foreach (var product in newProducts)
            {
                foreach (var color in product.CuloriProdusDto)
                {

                    if (color.ImaginiProdusDto.IsNullOrEmpty()) continue;
                    foreach (var image in color.ImaginiProdusDto!)
                    {
                        image.PresignedUrl =
                            await _bucketAcces.GenerateUrl(image.CaleImagineDto, image.FisierInBucketDto);
                    }

                }
            }

            return newProducts;
        });

        return getNewProducts ?? [];

    }
    
    public async Task<IList<MostViewedProduct>> GetProductsThatAreLimitedEdition(string currency = "RON")
    {
        var getLimitedEditionProducts = await _cache.GetOrCreateAsync($"newProducts_{currency}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
            var productsRepository = _unitOfWork.Repository<Produse>();
            var limitedEditionProducts = await productsRepository
                .GetSimpleQueryable()
                .Where(p => p.ActivInMagazin && !p.IsDeleted && p.ProdusLimitat)
                .Take(15)
                .Select(p => new MostViewedProduct
                {
                    CodProdusDto = p.CodProdus,
                    NumeProdusDto = currency == "RON" ?  p.NumeProdusJson.NumeRomana : p.NumeProdusJson.NumeEngleza,
                    // NumeProdusJsonDto = p.NumeProdusJson,
                    TipulProdusuluiDto = currency == "RON" ?  p.TipulProdusuluiJson.TipProdusRomana : p.TipulProdusuluiJson.TipProdusEngleza,
                    // TipulProdusuluiJsonDto = p.TipulProdusuluiJson,
                    PretBazaDto = currency == "RON"
                        ? p.PProduseCuDimensiuni!.Count == 0
                            ? p.PretDeBaza 
                            : p.PProduseCuDimensiuni.Min(dim => dim.Pret)
                        : p.PProduseCuDimensiuni!.Count == 0
                            ? p.PretDeBaza * (decimal)0.2
                            : p.PProduseCuDimensiuni.Min(dim => dim.Pret) * (decimal)0.2,
                    PretBazaRedusDto = currency == "RON"
                        ? p.PProduseCuDimensiuni!.Count == 0
                            ? p.PretDeBazaRedus
                            : p.PProduseCuDimensiuni.Min(dim => dim.PretRedus)
                        : p.PProduseCuDimensiuni!.Count == 0
                            ? p.PretDeBazaRedus * (decimal)0.2
                            : p.PProduseCuDimensiuni.Min(dim => dim.PretRedus) * (decimal)0.2,
                    CuloriProdusDto = p.PProduseCuCulori!
                        .Where(pc => pc.ImagProduseCuCulori!.Count > 0)
                        .Take(1)
                        .Select(culori => new ColorsWithImages
                        {
                            NumeCuloareDto = currency == "RON" ?  culori.Culoare.NumeCuloareJson.CuloareRomana :  culori.Culoare.NumeCuloareJson.CuloareEngleza,
                            ImaginiProdusDto = culori.ImagProduseCuCulori!
                                .OrderBy(image => image.CaleImagine)
                                .Take(1)
                                .Select(image => new ImagesDtoForUsers
                                {
                                    CaleImagineDto = image.CaleImagine ?? "",
                                    FisierInBucketDto = image.FisierInBucket,
                                    PresignedUrl = null
                                }).ToList()
                        }).ToList(),
                })
                .AsSplitQuery()
                .ToListAsync();


            foreach (var product in limitedEditionProducts)
            {
                foreach (var color in product.CuloriProdusDto)
                {

                    if (color.ImaginiProdusDto.IsNullOrEmpty()) continue;
                    foreach (var image in color.ImaginiProdusDto!)
                    {
                        image.PresignedUrl =
                            await _bucketAcces.GenerateUrl(image.CaleImagineDto, image.FisierInBucketDto);
                    }

                }
            }

            return limitedEditionProducts;
        });

        return getLimitedEditionProducts ?? [];

    }
    [GeneratedRegex(".*_X\\d{2}_.*$")]
    private static partial Regex MyRegex();
}