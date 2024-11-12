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
using E_Commerce_BackEnd.Models.ProductRelatedModels;
using E_Commerce_BackEnd.Services.Helpers.AWS_Secret.AWSBucket_CRUD;
using E_Commerce_BackEnd.Services.Helpers.UserHelpers;
using E_Commerce_BackEnd.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;

namespace E_Commerce_BackEnd.Services.uProductsService;

public partial class ProductService : IProductService
{
    [GeneratedRegex("^[a-z-0-9A-Z]+$")]
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
                    Descriere = modifiedProduct.DescriereDto,
                    NumeProdus = modifiedProduct.NumeProdusDto,
                    Compozitie = modifiedProduct.CompozitieDto,
                    Tva = modifiedProduct.TvaDto,
                    Ingrijire = modifiedProduct.IngrijireDto,
                    FataReversibila = modifiedProduct.FataReversibilaDto,
                    Stoc = modifiedProduct.StocDto,
                    TipulProdusului = modifiedProduct.TipulProdusuluiDto,
                    IsDeleted = false,
                    ActivInMagazin = modifiedProduct.ActivInMagazinDto,
                    PretDeBaza = modifiedProduct.PretBazaDto,
                    IdProducator = producator?.IdProducator,
                };
            
                await productRepository.AddAsync(newProduct);
                await _unitOfWork.CommitAsync();
                productToBeModified = newProduct;
                successCodes[0] = 1;
            }
            else
            {
                // If updating an existing product
                _mapper.Map(modifiedProduct, productToBeModified);
                await productRepository.UpdateAsync(productToBeModified!);
                successCodes[0] = 1;
            }
            
            var productId = productToBeModified!.IdProdus;

    
    
            // Handle product types
            var typesOnProductsRepository = _unitOfWork.Repository<TipuriPeProduse>();
            var productTypesRepository = _unitOfWork.Repository<TipuriProduse>();
            var modifiedProductTypes = modifiedProduct.TipuriProduseDto;
    
            if (!modifiedProductTypes.IsNullOrEmpty())
            {
                foreach (var newType in modifiedProductTypes)
                {
                    var findTypeInDb = await productTypesRepository
                        .FindQueryable(t => t.Categorie == newType.CategorieDto.ToUpper())
                        .FirstOrDefaultAsync();
    
                    if (findTypeInDb == null)
                    {
                        findTypeInDb = new TipuriProduse
                        {
                            Categorie = newType.CategorieDto.ToUpper()
                        };
    
                        await productTypesRepository.AddAsync(findTypeInDb);
                        await _unitOfWork.CommitAsync();
                    }
    
                    var typeOnProduct = await typesOnProductsRepository
                        .FindQueryable(tp => tp.IdTipProdus == findTypeInDb.IdTipProdus && tp.IdProdus == productId)
                        .FirstOrDefaultAsync();
    
                    if (typeOnProduct == null)
                    {
                        var newTypeOnProduct = new TipuriPeProduse
                        {
                            IdTipProdus = findTypeInDb.IdTipProdus,
                            IdProdus = productId
                        };
    
                        await typesOnProductsRepository.AddAsync(newTypeOnProduct);
                    }
                }
    
                successCodes[1] = 1;
            }
            else
            {
                successCodes[1] = 1;

            }
    
            // Handle colors and images
            var colorsOnProductsRepository = _unitOfWork.Repository<ProduseCuCulori>();
            var colorRepository = _unitOfWork.Repository<Culori>();
            var colorCodesRepository = _unitOfWork.Repository<CodCulori>();
            var imagesRepository = _unitOfWork.Repository<Imagini>();
            var modifiedProductColors = modifiedProduct.CuloriProdusDto;
    
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
    
                    var isCurrentUpdatedColorInDb = await colorRepository
                        .FindQueryable(c => c.NumeCuloare == color.NumeCuloareDto
                                            && c.IdCodCuloare == isCurrentUpdatedColorCodeInDb.IdCodCuloare)
                        .FirstOrDefaultAsync();
    
                    if (isCurrentUpdatedColorInDb == null)
                    {
                        var newUpdatedColor = new Culori
                        {
                            NumeCuloare = color.NumeCuloareDto.ToUpper(),
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
    
                            if (existingImage == null)
                            {
                                // New image to add
                                var imageFile = images.FirstOrDefault(img => img.FileName == imageDto.CaleImagineDto);
                                if (imageFile != null)
                                {
                                    var newImage = new Imagini
                                    {
                                        CaleImagine = imageDto.CaleImagineDto,
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
    
                        await dimensionsRepository.AddAsync(newUpdatedDimension);
                        await _unitOfWork.CommitAsync();
                        isNewDimensionInDb = newUpdatedDimension;
                    }
    
                    var isNewDimensionLinkedWithProductInDb = await dimensionOnProductsRepository
                        .FindQueryable(pd => pd.IdProdus == productId
                                             && pd.IdDimensiune == isNewDimensionInDb.IdDimensiune)
                        .FirstOrDefaultAsync();

                    if (isNewDimensionLinkedWithProductInDb != null) 
                        continue;
                    
                    var newUpdatedDimensionLinkedWithProduct = new ProduseCuDimensiuni
                    {
                        Pret = updatedDimension.PretDto,
                        PretRedus = updatedDimension.PretRedusDto,
                        IdDimensiune = isNewDimensionInDb.IdDimensiune,
                        IdProdus = productId
                    };
    
                    await dimensionOnProductsRepository.AddAsync(newUpdatedDimensionLinkedWithProduct);
                }
    
                successCodes[4] = 1;
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
                successCodes[4] = 1;
            }
    
            
    
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
    /// <param name="folderName">directory name in S3 bucket </param>
    /// <returns>A success for value 1 and error for value 0 (Int)</returns>
    public async Task<int> AddOrEditProductFromExcel(ProduseDto produseDto, IList<int> idDimensiuni, 
        string[] filePath, IList<int> idTipProduse, IList<int> culori,
        string[] preturiPerDimensiuni, int idProducator,string tipProdus,
        string folderName)
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
                        Descriere = produseDto.DescriereDto,
                        NumeProdus = produseDto.NumeProdusDto,
                        Compozitie = produseDto.CompozitieDto,
                        Tva = produseDto.TvaDto,
                        Ingrijire = produseDto.IngrijireDto,
                        FataReversibila = produseDto.FataReversibilaDto,
                        Stoc = produseDto.StocDto,
                        IsDeleted = produseDto.IsDeletedDto,
                        ActivInMagazin = produseDto.ActivInMagazinDto,
                        TipulProdusului = produseDto.TipProdusDto,
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
                        Descriere = produseDto.DescriereDto,
                        NumeProdus = produseDto.NumeProdusDto,
                        Compozitie = produseDto.CompozitieDto,
                        Tva = produseDto.TvaDto,
                        Ingrijire = produseDto.IngrijireDto,
                        FataReversibila = produseDto.FataReversibilaDto,
                        Stoc = produseDto.StocDto,
                        IsDeleted = produseDto.IsDeletedDto,
                        ActivInMagazin = produseDto.ActivInMagazinDto,
                        TipulProdusului = produseDto.TipProdusDto,
                        PretDeBaza = produseDto.PretBazaDto,
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

                var dirNameInS3 = folderName;
                if (!filePath.IsNullOrEmpty())
                {
                    var imagesDirPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}/imagini_import";
                    
                    foreach (var imageName in filePath)
                    {
                       
                        var machineFullImagePath = Path.Join(imagesDirPath,imageName); // image path for the PC
                       
                        var responseFromBucketAdd = await _bucketAcces.AddOrUpdateToBucket(machineFullImagePath,dirNameInS3, imageName);

                        if (responseFromBucketAdd == -1)
                        {
                            throw new DbUpdateException("Problem when uploading file to S3 Bucket");
                        }
                      
                        _logger.LogInformation($"Response from AddToBucket: {responseFromBucketAdd} (SUCCESS)");
                        
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
                            throw new DbUpdateException("Color code not found");
                        }
                        
                        // id of the color linked with the currentColorCode
                        var colorRelatedToTheCurrentColorCode = await colorsRepository
                            .FindQueryable(c => c.IdCodCuloare == currentColorCode.IdCodCuloare)
                            .FirstOrDefaultAsync();
                        
                        if (colorRelatedToTheCurrentColorCode is null)
                        {
                            throw new DbUpdateException($"Color  with code {currentColorCode} not found");
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
                            FisierInBucket = dirNameInS3
                        };

                        await imagesRepository.AddAsync(newImage);
                        await _unitOfWork.CommitAsync();

                        _logger.LogInformation("Succesfully saved relative image path to database");
                        return 1;
                    }
                }
                else
                {
                    _logger.LogInformation($"No images added for the product  : {findProductAlreadyInDb.CodProdus} ");
                    return 1;
                }
                
            }
            else
            {
                var productId = findProductAlreadyInDb.IdProdus;
                var productCode = findProductAlreadyInDb.CodProdus;
                _logger.LogInformation($"Updating product with code {productCode}");
                if (idProducator != 0)
                {
                    _logger.LogInformation($"Updating product with code 1 {productCode}");

                    var manufacturer = await producatoriRepository
                        .GetByIdAsync(idProducator);

                    _logger.LogInformation($"Updating product with code 2 {productCode}");

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
                        Descriere = produseDto.DescriereDto,
                        NumeProdus = produseDto.NumeProdusDto,
                        Compozitie = produseDto.CompozitieDto,
                        Tva = produseDto.TvaDto,
                        Ingrijire = produseDto.IngrijireDto,
                        FataReversibila = produseDto.FataReversibilaDto,
                        Stoc = produseDto.StocDto,
                        IsDeleted = produseDto.IsDeletedDto,
                        ActivInMagazin = produseDto.ActivInMagazinDto,
                        TipulProdusului = produseDto.TipProdusDto,
                        PretDeBaza = produseDto.PretBazaDto,
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
                        Descriere = produseDto.DescriereDto,
                        NumeProdus = produseDto.NumeProdusDto,
                        Compozitie = produseDto.CompozitieDto,
                        Tva = produseDto.TvaDto,
                        Ingrijire = produseDto.IngrijireDto,
                        FataReversibila = produseDto.FataReversibilaDto,
                        Stoc = produseDto.StocDto,
                        PretDeBaza = produseDto.PretBazaDto,
                        IsDeleted = produseDto.IsDeletedDto,
                        ActivInMagazin = produseDto.ActivInMagazinDto,
                        TipulProdusului = produseDto.TipProdusDto,
                        IdProducator = null
                    };
                    
                    
                    _mapper.Map(updatedProduct, findProductAlreadyInDb);
                    
                    
                    await _unitOfWork.CommitAsync();
                    _logger.LogInformation($"Updated product with cod : {produseDto.CodProdusDto} without manufacturer");
                }

                var dimensionsUpdatedId = idDimensiuni.ToArray();
                
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
                var imagesToAdd = (from newImage in filePath
                                             let isNewImageAnOldImage = oldImageNames.FirstOrDefault(pair => pair.Key == newImage && pair.Value == folderName) 
                                             where isNewImageAnOldImage.Key == null 
                                             select newImage).ToList();

                if (imagesToRemove.IsNullOrEmpty() && imagesToAdd.IsNullOrEmpty())
                {
                    _logger.LogInformation($"No photo has been changed for product: {productCode}");
                    return 2;
                }
                
                if (imagesToRemove.Count > 0)
                {
                    foreach (var name in imagesToRemove)
                    {
                        _logger.LogInformation($"Image to remove anme -> {name.Key}");
                    }
                }
                else
                {
                    _logger.LogInformation($"No images to remove");

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
                    foreach (var name in imagesToAdd)
                    {
                        _logger.LogInformation($"Image to add anme -> {name}");
                    }
                }
                else
                {
                    _logger.LogInformation($"No images to add");

                }

                if (imagesToAdd.Count > 0)
                {
                    var imageDirPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}/imagini_import/imagini_update";
                    var dirNameInS3 = folderName;
                
                    foreach (var updatedImageName in imagesToAdd)
                    {
                        var fullImagePath = Path.Join(imageDirPath, updatedImageName);
                
                        var responseFromBucketAdd = await _bucketAcces.AddOrUpdateToBucket(fullImagePath, dirNameInS3, updatedImageName);
                
                        if (responseFromBucketAdd == -1)
                        {
                            throw new DbUpdateException($"An error happened when uploading the new photo with name: {updatedImageName} ");
                        }
                
                        // Extract color code
                        var patternCode = @"X(\d{2})";
                        string colorCode;
                        Match matchColor = Regex.Match(updatedImageName, patternCode);
                
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
                            CaleImagine = updatedImageName,
                            IdProdusCuCuloare = productWithColorIdToBeLinkedToImage!.IdProdusCuCuloare,
                            FisierInBucket = dirNameInS3
                        };
                
                        await imagesRepository.AddAsync(newUpdatedImage);
                        newAddedImages++;
                
                        _logger.LogInformation("Succesfully updated the images");
                    }
                    _logger.LogInformation($"Added : {newAddedImages} new images");
                }
                return 2;
            }
            return 0;
        }
        catch (DbUpdateException e)
        {
            Console.WriteLine(e.Message);
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
        
            _logger.LogInformation("Succesfully found the product to delete the type on it");

            var typeToDeleteOnProduct = await typeRepository
                .FindQueryable(t => t.Categorie == categorieProdus)
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

            var colorCodeAssociatedWithColor = await colorCodesRepository
                .FindQueryable(cc => cc.CodCuloare == codCuloare)
                .FirstOrDefaultAsync();

            if (colorCodeAssociatedWithColor is null)
            {
                _logger.LogInformation("Color code  not found");
                return 0;
            }
            
            var colorToDeleteOnProduct = await colorRepository
                .FindQueryable(c => c.NumeCuloare == numeCuloare && c.IdCodCuloare == colorCodeAssociatedWithColor.IdCodCuloare)
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

            var colorCodeAssociatedWithColor = await colorCodesRepository
                .FindQueryable(cc => cc.CodCuloare == codCuloare)
                .FirstOrDefaultAsync();

            if (colorCodeAssociatedWithColor is null)
            {
                _logger.LogInformation("Color code  not found image deletion");
                return 0;
            }
            
            var colorToDeleteOnProduct = await colorRepository
                .FindQueryable(c => c.NumeCuloare == numeCuloare && c.IdCodCuloare == colorCodeAssociatedWithColor.IdCodCuloare)
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
        var productsToDelete = new List<Produse>();
        try
        {
            deletingTransaction = await _unitOfWork.BeginTransactionAsync();
            var productRepository = _unitOfWork.Repository<Produse>();
            
            // Fetch the products to delete
            foreach (var productCode in bulkOperationsDto.SelectedItemsToDoBulkOperations!)
            {
                var productToDelete = await productRepository
                    .FindQueryable(p => p.CodProdus == productCode.ToString())
                    .FirstOrDefaultAsync();

                if (productToDelete is null)
                {
                    throw new Exception($"Product with code {productCode} is not in the database");
                }
                productToDelete.IsDeleted = true;
                productToDelete.ActivInMagazin = false;
                productsToDelete.Add(productToDelete);
            }

         
            await productRepository.UpdateRangeAsync(productsToDelete);

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
            return -1;
        }
    }

    public async Task<int> ActivateSelectedProducts(BulkOperationsDto bulkOperationsDto)
    {
        IDbContextTransaction? updatingTransaction = null;
        var productsToUpdate = new List<Produse>();
        try
        {
            updatingTransaction = await _unitOfWork.BeginTransactionAsync();
            var productRepository = _unitOfWork.Repository<Produse>();
            
            // Fetch the products to delete
            foreach (var productCode in bulkOperationsDto.SelectedItemsToDoBulkOperations!)
            {
                var productToUpdate = await productRepository
                    .FindQueryable(p => p.CodProdus == productCode.ToString())
                    .FirstOrDefaultAsync();

                if (productToUpdate is null)
                {
                    throw new Exception($"Product with code {productCode} is not in the database");
                }
                
                productToUpdate.ActivInMagazin = true;
                productsToUpdate.Add(productToUpdate);
            }

         
            await productRepository.UpdateRangeAsync(productsToUpdate);

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
            return -1;
        }
    }

    public Task<string> GeneratePresignedUrl(string caleImagine, string fisierInBucket)
    {
        throw new NotImplementedException();
    }

    

    public async Task<ProductOptionsForComboBox?> GetProductTypes()
    {
        var manuFacturersDto = await _unitOfWork.Repository<Producatori>()
            .GetSimpleQueryable()
            .GroupBy(prod => prod.NumeProducator)
            .Select(prod => prod.Key)
            .ToListAsync();
        
        var productCategories = await _unitOfWork.Repository<TipuriProduse>()
            .GetSimpleQueryable()
            .GroupBy(tip => tip.Categorie)
            .Select(g => g.Key)
            .ToListAsync();
        
        var colors = await _unitOfWork.Repository<Culori>()
            .GetSimpleQueryable()
            .GroupBy(culoare => culoare.NumeCuloare)
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

        var productTypes = await _unitOfWork.Repository<Produse>()
            .GetSimpleQueryable()
            .GroupBy(types => types.TipulProdusului)
            .Select(g => g.Key)
            .ToListAsync();

        var directoriesInS3Bucket = await _bucketAcces
            .ListDirsFromBuckets();
    
        var productOptions = new ProductOptionsForComboBox
        {
            ProductCategoriesForBox = productCategories,
            LatimiForBox = widths,
            LungimiForBox = heights,
            RecomandariForBox = bedRecommendations,
            CoduriCuloriForBox = colorCodes,
            CuloriForBox = colors,
            DirectoriesInBucket = directoriesInS3Bucket!,
            ManuFacturersDto = manuFacturersDto,
            ProductTypes = productTypes
        };
    
        return productOptions;
    }

    public async Task<IList<ProductInfo>?> GetProductCodesAndNames()
    {
        var productsRepository = _unitOfWork.Repository<Produse>();

        var namesOfProducts = await productsRepository.GetSimpleQueryable()
            .Select(p => new ProductInfo
            {
                NumeProdus = p.NumeProdus!,
                CodProdus = p.CodProdus
            })
            .ToListAsync();
         
        return namesOfProducts;

    }

    public async Task<IList<ProductsListingForUsers>> GetProductsForUsers(
        int? pageNumber, List<string>? productTypes, List<string>? productColors,
        List<string>? productDimensions, List<decimal>? productPrices, bool? reverseFace,
        string currency = "RON")
    {
        var onlyLettersAndSpacesBetween = ValidateQueryParams();
        var onlyNumbers = ValidateNumberesOnly();
        if (!productTypes.IsNullOrEmpty())
        {
            productTypes = productTypes!.Where(type => onlyLettersAndSpacesBetween.IsMatch(type)).ToList();
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
            .Where(product => productTypes.IsNullOrEmpty() || productTypes!.Contains(product.TipulProdusului.ToUpper()))
            .Where(product => productColors.IsNullOrEmpty() || 
                              product.PProduseCuCulori!.Any(culoare => productColors!.Contains(culoare.Culoare.NumeCuloare.ToUpper())))
            .Where(product => productDimensions.IsNullOrEmpty() || 
                              product.PProduseCuDimensiuni!
                                  .Any(dimensiune => Convert.ToInt16(dimensiune.PdDimensiune!.Lungime) >= Convert.ToInt16(productDimensions![0]) && 
                                                     Convert.ToInt16(dimensiune.PdDimensiune!.Lungime) <= Convert.ToInt16(productDimensions[1]) && 
                                                     Convert.ToInt16(dimensiune.PdDimensiune!.Latime) >= Convert.ToInt16(productDimensions[2]) &&
                                                     Convert.ToInt16(dimensiune.PdDimensiune!.Latime) <= Convert.ToInt16(productDimensions[3])))
            .Where(product => productPrices.IsNullOrEmpty() || 
                              (product.PProduseCuDimensiuni != null && product.PProduseCuDimensiuni.Count > 0
                                  ? product.PProduseCuDimensiuni!.Any(dimension =>
                                      currency == "EUR" 
                                          ? dimension.PretRedus != 0 
                                              ? dimension.PretRedus * (decimal)0.2 >= Convert.ToDecimal(productPrices![0]) && dimension.PretRedus <= Convert.ToDecimal(productPrices[1])
                                              : dimension.Pret * (decimal)0.2 >= Convert.ToDecimal(productPrices![0]) && dimension.Pret <= Convert.ToDecimal(productPrices[1])
                                          : dimension.PretRedus != 0 
                                              ? dimension.PretRedus  >= Convert.ToDecimal(productPrices![0]) && dimension.PretRedus <= Convert.ToDecimal(productPrices[1])
                                              : dimension.Pret  >= Convert.ToDecimal(productPrices![0]) && dimension.Pret <= Convert.ToDecimal(productPrices[1])
                                  )
                                  : currency == "EUR" 
                                      ? product.PretDeBazaRedus > 0 
                                          ? product.PretDeBazaRedus * (decimal)0.2 >= Convert.ToDecimal(productPrices![0]) && product.PretDeBazaRedus <= Convert.ToDecimal(productPrices[1])
                                          : product.PretDeBaza * (decimal)0.2 >= Convert.ToDecimal(productPrices![0]) && product.PretDeBaza <= Convert.ToDecimal(productPrices[1]) 
                                      : product.PretDeBazaRedus > 0 
                                          ? product.PretDeBazaRedus >= Convert.ToDecimal(productPrices![0]) && product.PretDeBazaRedus <= Convert.ToDecimal(productPrices[1])
                                          : product.PretDeBaza >= Convert.ToDecimal(productPrices![0]) && product.PretDeBaza <= Convert.ToDecimal(productPrices[1]) 
                              )
            )
            .Where(product => reverseFace == null || product.FataReversibila == reverseFace)
            .Where(product => !product.IsDeleted && product.ActivInMagazin);

        var totalProductsFiltered = await mainQuery.CountAsync();

        var takePaginatedProducts = await mainQuery
        .OrderBy(product => product.TipulProdusului)
        .Skip((pageNumber ?? 0) * PageSize)
        .Take(PageSize)
        .Select(product => new ProductsListingForUsers
        {
            CodProdusDto = product.CodProdus,
            NumeProdusDto = product.NumeProdus!,
            TipulProdusuluiDto = product.TipulProdusului,
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
                    NumeCuloareDto = culori.Culoare.NumeCuloare,
                    ImaginiProdusDto = culori.ImagProduseCuCulori!
                        .Select( image => new ImagesDtoForUsers
                        {
                            CaleImagineDto = image.CaleImagine!,
                            FisierInBucketDto = image.FisierInBucket,
                            PresignedUrl = null
                        }).ToList()
                }).ToList()
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
        var productTypes = await _unitOfWork.Repository<Produse>()
            .GetSimpleQueryable()
            .GroupBy(types => types.TipulProdusului)
            .Select(g => g.Key.ToUpper())
            .ToListAsync();
        
        
        var colors = await _unitOfWork.Repository<Culori>()
            .GetSimpleQueryable()
            .GroupBy(culoare => culoare.NumeCuloare)
            .Select(g => g.Key.ToUpper())
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
            FilterColors = colors.ToImmutableHashSet(),
            FilterProductTypes = productTypes.ToImmutableHashSet(),
            PricesRange = pricesRange.ToImmutableHashSet()
        };
    }

    public async Task<KeyValuePair<int , ProductPageForUser?>> GetProductPage(string codProdus,string tipProdus,string currency = "RON")
    {
        var productsRepository = _unitOfWork.Repository<Produse>();
       
        try
        {
            
            if (string.Equals(tipProdus.ToLower(), "perdea") || string.Equals(tipProdus.ToLower(), "draperie"))
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

           
                var productForUser = await  productsRepository
                    .GetSimpleQueryable()
                    .Where(product => product.CodProdus == codProdus.ToUpper() && product.TipulProdusului == tipProdus.ToLower())
                    .Select(product => new ProductPageForUser
                    {
                        CodProdusDto = product.CodProdus,
                        DescriereDto = product.Descriere,
                        NumeProdusDto = product.NumeProdus,
                        CompozitieDto = product.Compozitie,
                        TvaDto = product.Tva,
                        IngrijireDto = product.Ingrijire,
                        FataReversibilaDto = product.FataReversibila,
                        TipulProdusuluiDto = product.TipulProdusului,
                        NumeProducatorDto = product.Producator == null ? null : product.Producator.NumeProducator,
                        PretBazaDto = currency == "EUR" ? UserHelpers.ConvertCurrency("RON" , "EUR" ,product.PretDeBaza , 0 ) : product.PretDeBaza,
                        PretBazaRedusDto = currency == "EUR" ? UserHelpers.ConvertCurrency("RON" , "EUR" , product.PretDeBazaRedus , 0 ) : product.PretDeBazaRedus,
                        TipuriInele = ringTypesDto!,
                        TipuriRejansa = rejanseTypesDto!,
                        TipuriLinie = liningTypesDto!,
                        CuloriProdus = product.PProduseCuCulori!
                            .Select(pc => new CuloriDto
                            {
                                NumeCuloareDto = pc.Culoare.NumeCuloare,
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
                        CategoriiProdus = product.PTipuriPeProduse!
                            .Select(type => type.TppTipProdus.Categorie)
                            .ToList()
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
                    .Where(product => product.CodProdus == codProdus.ToUpper() && product.TipulProdusului == tipProdus.ToLower())
                    .Select(product => new ProductPageForUser
                    {
                        CodProdusDto = product.CodProdus,
                        DescriereDto = product.Descriere,
                        NumeProdusDto = product.NumeProdus,
                        CompozitieDto = product.Compozitie,
                        TvaDto = product.Tva,
                        IngrijireDto = product.Ingrijire,
                        FataReversibilaDto = product.FataReversibila,
                        TipulProdusuluiDto = product.TipulProdusului,
                        NumeProducatorDto = product.Producator == null ? null : product.Producator.NumeProducator,
                        PretBazaDto = currency == "EUR" ? UserHelpers.ConvertCurrency("RON" , "EUR" ,product.PretDeBaza , 0 ) : product.PretDeBaza,
                        PretBazaRedusDto = currency == "EUR" ? UserHelpers.ConvertCurrency("RON" , "EUR" , product.PretDeBazaRedus , 0 ) : product.PretDeBazaRedus,
                        DimensiuniProdus = product.PProduseCuDimensiuni!
                            .OrderByDescending(dimensiune => dimensiune.Pret)
                            .Select(dimensiune => new DimensiuniDto
                            {
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
                                NumeCuloareDto = pc.Culoare.NumeCuloare,
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
                        CategoriiProdus = product.PTipuriPeProduse!
                        .Select(type => type.TppTipProdus.Categorie)
                        .ToList()
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
                _logger.LogError("Product does not exist anymore");
                return new KeyValuePair<int, ProductPageForUser?>(1,null);
            }
            Console.WriteLine(e.Message);
            _logger.LogError("General error occured");
            return new KeyValuePair<int, ProductPageForUser?>(0,null);
                
        }
        
    }
}