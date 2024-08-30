using System.Drawing;
using System.Text.RegularExpressions;
using AutoMapper;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductOptionsDto;
using E_Commerce_BackEnd.Models.ProductRelatedModels;
using E_Commerce_BackEnd.Models.ProductVouchersModels;
using E_Commerce_BackEnd.Services.Helpers.AWS_Secret.AWSBucket_CRUD;
using E_Commerce_BackEnd.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;

namespace E_Commerce_BackEnd.Services.uProductsService;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<Produse> _logger;
    private readonly IMemoryCache _cache;
    private readonly IBucketAcces _bucketAcces;
    private readonly IMapper _mapper;


    public ProductService(
        IUnitOfWork unitOfWork, 
        ILogger<Produse> logger, 
        IMemoryCache cache, IBucketAcces bucketAcces, IMapper mapper)
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

            var produs = await repository.GetByIdAsync(codProdus);

            if (produs is null)
            {
                return -1;
            }

            produs.IsDeleted = true;
            produs.ActivInMagazin = false;

            await repository.UpdateAsync(produs);
            await _unitOfWork.CommitTransactionAsync(transaction);
            _logger.LogInformation("Product deleted succesfully!");
            return 1;
            
        }
        catch (Exception e)
        {
            if (transaction == null)
            {
                await _unitOfWork.RollBackTransactionAsync(transaction!);
            }
            _logger.LogError("Errors when trying to delete the product");
            return -2;
        }
       

    }
    /// <summary>
    /// The method to update a product using the interface
    /// </summary>
    /// <param name="modifiedProduct">The Dto with the updated product data</param>
    /// <returns>And integer array containing the succes codes for each operation
    /// to display on the frontend and see where it was an error
    /// succesCode[0] = product types
    /// succesCode[1] = product colors
    /// succesCode[2] = product images
    /// succesCode[3] = product dimensions
    /// succesCode[4] = product vouchers</returns>
    public async Task<int[]> UpdateProduct(ProduseDtoForAdminModification modifiedProduct)
    {
        int[] succesCodes = new int[5];
        // main tables repositories
        var productRepository = _unitOfWork.Repository<Produse>();
        var vouchereRepository = _unitOfWork.Repository<Vouchere>();
        var colorRepository = _unitOfWork.Repository<Culori>();
        var colorCodesRepository = _unitOfWork.Repository<CodCulori>();
        var dimensionsRepository = _unitOfWork.Repository<Dimensiuni>();
        var imagesRepository = _unitOfWork.Repository<Imagini>();
        var productTypesRepository = _unitOfWork.Repository<TipuriProduse>();
        var manufacturersRepository = _unitOfWork.Repository<Producatori>();
        // join tables repositories
        var colorsOnProductsRepository = _unitOfWork.Repository<ProduseCuCulori>();
        var dimensionOnProductsRepository = _unitOfWork.Repository<ProduseCuDimensiuni>();
        var typesOnProductsRepository = _unitOfWork.Repository<TipuriPeProduse>();
        var vouchersOnProductsRepository = _unitOfWork.Repository<ProduseCuVouchere>();
        IDbContextTransaction? modifiyingTransaction = null;
        try
        {
            modifiyingTransaction = await _unitOfWork.BeginTransactionAsync();
            var productToBeModified = await productRepository
                .FindQueryable(p => p.CodProdus == modifiedProduct.CodProdusDto)
                .FirstOrDefaultAsync();

            if (productToBeModified is null)
            {
                throw new Exception("Product to be modified not found");
            }

            var numeProducator = modifiedProduct.NumeProducatorDto;
            if (numeProducator is null)
            {
                _logger.LogInformation("No producator was choose on the update");
            }

            _mapper.Map(modifiedProduct, productToBeModified);
            
            _logger.LogInformation("Succesfully mapped general characteristics");

            var productId = productToBeModified.IdProdus;

            
            /* Types updates
             * ---------------
             * -------------
             * */
            
            var oldProductTypes = await typesOnProductsRepository
                .FindQueryable(tp => tp.IdProdus == productId)
                .ToListAsync();
            
            var modifiedProductTypes = modifiedProduct.TipuriProduseDto;
            
            // Early exit if both old and new types are empty
            if (oldProductTypes.IsNullOrEmpty() && modifiedProductTypes.IsNullOrEmpty())
            {
                _logger.LogInformation("No updates on product types (both null)");
                succesCodes[0] = 1;
            }
            
            // Handle the case where all product types are removed
            if (modifiedProductTypes.IsNullOrEmpty())
            {
                _logger.LogInformation("All product types were removed");
                await typesOnProductsRepository.DeleteRangeAsync(oldProductTypes);
                succesCodes[0] = 1;
            }
            
            var updatedTypes = new List<TipuriPeProduse>();
            
            foreach (var newType in modifiedProductTypes)
            {
                var findTypeInDb = await productTypesRepository
                    .FindQueryable(t => t.Categorie == newType.CategorieDto.ToUpper()
                                        && t.TipProdus == newType.TipProdusDto.ToUpper())
                    .FirstOrDefaultAsync();
            
                if (findTypeInDb is null)
                {
                    findTypeInDb = new TipuriProduse
                    {
                        TipProdus = newType.TipProdusDto.ToUpper(),
                        Categorie = newType.CategorieDto.ToUpper()
                    };
            
                    await productTypesRepository.AddAsync(findTypeInDb);
                    await _unitOfWork.CommitAsync();
                }
            
                var typeOnProduct = oldProductTypes.FirstOrDefault(tp =>
                    tp.IdTipProdus == findTypeInDb.IdTipProdus);
            
                if (typeOnProduct is null)
                {
                    updatedTypes.Add(new TipuriPeProduse
                    {
                        IdTipProdus = findTypeInDb.IdTipProdus,
                        IdProdus = productId
                    });
                }
                else
                {
                    oldProductTypes.Remove(typeOnProduct);
                }
            }

            // Add new product types associations
            if (updatedTypes.Count != 0)
            {
                await typesOnProductsRepository.AddRangeAsync(updatedTypes);
                succesCodes[0] = 1;
            }
            
            // Delete remaining old types that are no longer associated
            if (oldProductTypes.Count != 0)
            {
                await typesOnProductsRepository.DeleteRangeAsync(oldProductTypes);
                succesCodes[0] = 1;
            }
            
            _logger.LogInformation("Successfully updated product types");
            
            /* Color updates
             * ---------------
             * -------------
             */
            
            var oldProductWithColors = await colorsOnProductsRepository
                .FindQueryable(pc => pc.IdProdus == productId)
                .ToListAsync();
            
            var modifiedProductColors = modifiedProduct.CuloriProdusDto;
            
            // Early exit if both old and new colors are empty
            if (oldProductWithColors.IsNullOrEmpty() && modifiedProductColors.IsNullOrEmpty())
            {
                _logger.LogInformation("No updates on product colors (both null)");
                succesCodes[1] = 1;
                
            }
            
            // Handle the case where all product colors are removed
            if (modifiedProductColors.IsNullOrEmpty())
            {
                _logger.LogInformation("All product colors were removed");
                await colorsOnProductsRepository.DeleteRangeAsync(oldProductWithColors);
                succesCodes[1] = 1;
                
            }
            
            var updatedColors = new List<ProduseCuCulori>();

            if (!modifiedProductColors.IsNullOrEmpty())
            {
                foreach (var color in modifiedProductColors)
                {
                    var isCurrentUpdatedColorCodeInDb = await colorCodesRepository
                        .FindQueryable(cc => cc.CodCuloare == color.CodCuloareDto)
                        .FirstOrDefaultAsync();

                    if (isCurrentUpdatedColorCodeInDb is null)
                    {
                        // Add new color code if it doesn't exist
                        var newUpdatedColorCode = new CodCulori
                        {
                            CodCuloare = color.CodCuloareDto
                        };

                        await colorCodesRepository.AddAsync(newUpdatedColorCode);
                        await _unitOfWork.CommitAsync();
                        isCurrentUpdatedColorCodeInDb = newUpdatedColorCode;
                    }

                    // Check if color name exists
                    var isCurrentUpdatedColorInDb = await colorRepository
                        .FindQueryable(c => c.NumeCuloare == color.NumeCuloareDto
                                            && c.IdCodCuloare == isCurrentUpdatedColorCodeInDb.IdCodCuloare)
                        .FirstOrDefaultAsync();

                    if (isCurrentUpdatedColorInDb is null)
                    {
                        // Add new color if it doesn't exist
                        var newUpdatedColor = new Culori
                        {
                            NumeCuloare = color.NumeCuloareDto,
                            IdCodCuloare = isCurrentUpdatedColorCodeInDb.IdCodCuloare
                        };

                        await colorRepository.AddAsync(newUpdatedColor);
                        await _unitOfWork.CommitAsync();
                        isCurrentUpdatedColorInDb = newUpdatedColor;
                    }

                    // Check if this color is already associated with the product
                    var isCurrentColorOnProduct = await colorsOnProductsRepository
                        .FindQueryable(pc => pc.IdProdus == productId
                                             && pc.IdCuloare == isCurrentUpdatedColorInDb.IdCuloare)
                        .FirstOrDefaultAsync();

                    if (isCurrentColorOnProduct is null)
                    {
                        // Add new ProduseCuCulori entity to the database
                        var newProduseCuCulori = new ProduseCuCulori
                        {
                            IdProdus = productId,
                            IdCuloare = isCurrentUpdatedColorInDb.IdCuloare
                        };

                        await colorsOnProductsRepository.AddAsync(newProduseCuCulori);
                        await _unitOfWork.CommitAsync();

                        // Retrieve the generated IdProdusCuCuloare
                        var currentIdProdusCuCuloare = newProduseCuCulori.IdProdusCuCuloare;

                        var oldImages = await imagesRepository
                            .FindQueryable(i => i.IdProdusCuCuloare == currentIdProdusCuCuloare)
                            .ToListAsync();

                        // Add the associated Imagini entities
                        if (!color.ImaginiProdusDto.IsNullOrEmpty())
                        {
                            _logger.LogInformation("There are new images");
                            var newImages = new List<Imagini>();
                            // initial -> A,B,
                            // final -> B,C,D
                            foreach (var newImage in color.ImaginiProdusDto!)
                            {
                                var isImageInDb = await imagesRepository
                                    .FindQueryable(i => i.CaleImagine == newImage.CaleImagineDto)
                                    .FirstOrDefaultAsync();

                                if (isImageInDb is null)
                                {
                                    var imageToAdd = new Imagini
                                    {
                                        CaleImagine = newImage.CaleImagineDto,
                                        FisierInBucket = newImage.FisierInBucketDto,
                                        IdProdusCuCuloare = currentIdProdusCuCuloare
                                    };
                                    newImages.Add(imageToAdd);
                                    using var currentImageStream = new MemoryStream();
                                    await newImage.ImageStream!.CopyToAsync(currentImageStream);
                                    currentImageStream.Position = 0;

                                    var bucketResponse = await _bucketAcces.AddOrUpdateToBucket(currentImageStream,
                                        newImage.FisierInBucketDto, newImage.CaleImagineDto);

                                    if (bucketResponse == 1)
                                    {
                                        _logger.LogInformation("Succesfully added new images to bucket");
                                    }
                                    else
                                    {
                                        _logger.LogInformation("An error happenned when adding to bucket ");
                                    }

                                }
                                else
                                {
                                    _logger.LogInformation("Updated image was the same with the old one");
                                    oldImages.Remove(isImageInDb);
                                }
                            }

                            if (oldImages.Count != 0)
                            {
                                _logger.LogInformation("Removing old images from db");
                                await imagesRepository.DeleteRangeAsync(oldImages);
                                foreach (var oldImage in oldImages)
                                {
                                    var keyName = $"images/{oldImage.FisierInBucket}/{oldImage.CaleImagine}";
                                    var deletingFromBucketResponse = await _bucketAcces.DeleteFromBucket(keyName);

                                    if (deletingFromBucketResponse == 1)
                                    {
                                        _logger.LogInformation("Succesfully removed old image from bucket");
                                    }
                                    else if (deletingFromBucketResponse == -1)
                                    {
                                        _logger.LogInformation($"No file named with this key {oldImage.CaleImagine}");
                                    }
                                    else
                                    {
                                        _logger.LogInformation($"Error when removing from bucket");
                                    }
                                }
                            }

                            if (newImages.Count != 0)
                            {
                                _logger.LogInformation("Adding new images to db");
                                await imagesRepository.AddRangeAsync(newImages);
                            }
                            
                            await _unitOfWork.CommitAsync();
                        }
                        else
                        {
                            await imagesRepository.DeleteRangeAsync(oldImages);
                        }

                        succesCodes[2] = 1;
                    }
                    else
                    {
                        // If the association already exists, remove it from the old list (to avoid deletion later)
                        oldProductWithColors.Remove(isCurrentColorOnProduct);
                    }
                }

                // Add new color associations
                if (updatedColors.Any())
                {
                    await colorsOnProductsRepository.AddRangeAsync(updatedColors);
                }

                // Delete old color associations that are no longer needed
                if (oldProductWithColors.Any())
                {
                    await colorsOnProductsRepository.DeleteRangeAsync(oldProductWithColors);
                }

                _logger.LogInformation("Successfully updated product colors");
                succesCodes[1] = 1;
               
            }
            var oldDimensions = await dimensionOnProductsRepository
                .FindQueryable(pd => pd.IdProdus == productId)
                .ToListAsync();
            
            var modifiedDimensionOnProduct = modifiedProduct.DimensiuniProduseDto;
            
            // Early exit if both old and new types are empty
            if (oldDimensions.IsNullOrEmpty() && modifiedDimensionOnProduct.IsNullOrEmpty())
            {
                _logger.LogInformation("No updates on product dimensions (both null)");
                succesCodes[3] = 1;
            }
            
            // Handle the case where all product types are removed
            if (modifiedDimensionOnProduct.IsNullOrEmpty())
            {
                _logger.LogInformation("All product dimensions were removed");
                await typesOnProductsRepository.DeleteRangeAsync(oldProductTypes);
                succesCodes[3] = 1;
            }
            
            var updatedDimensions = new List<ProduseCuDimensiuni>();

            if (!modifiedDimensionOnProduct.IsNullOrEmpty())
            {
                foreach (var updatedDimension in modifiedDimensionOnProduct!)
                {
                    var isNewDimensionInDb = await dimensionsRepository
                        .FindQueryable(d => d.Lungime == updatedDimension.LungimeDto
                                            && d.Latime == updatedDimension.LatimeDto
                                            && d.RecomandarePat == updatedDimension.RecomandarePat)
                        .FirstOrDefaultAsync();

                    if (isNewDimensionInDb is null)
                    {
                        var newUpdatedDimension = new Dimensiuni
                        {
                            Lungime = updatedDimension.LungimeDto,
                            Latime = updatedDimension.LatimeDto,
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


                    if (isNewDimensionLinkedWithProductInDb is null)
                    {
                        var newUpdatedDimensionLinkedWithProduct = new ProduseCuDimensiuni
                        {
                            Pret = updatedDimension.PretDto,
                            PretRedus = updatedDimension.PretRedusDto,
                            IdDimensiune = isNewDimensionInDb.IdDimensiune,
                            IdProdus = productId
                        };
                        
                        updatedDimensions.Add(newUpdatedDimensionLinkedWithProduct);
                        
                    }
                    else
                    {
                        oldDimensions.Remove(isNewDimensionLinkedWithProductInDb);
                    }

                }

                if (oldDimensions.Count != 0)
                {
                    _logger.LogInformation("Removing old dimensions ");
                    await dimensionOnProductsRepository.DeleteRangeAsync(oldDimensions);
                }

                if (updatedDimensions.Count != 0)
                {
                    _logger.LogInformation("Adding new dimensions ");

                    await dimensionOnProductsRepository.AddRangeAsync(updatedDimensions);
                }

                succesCodes[3] = 1;

            }

            var oldVouhersOnProducts = await vouchersOnProductsRepository
                .FindQueryable(v => v.IdProdus == productId)
                .ToListAsync();
            
            var modifiedProductVouchers = modifiedProduct.VouchereProdusDto;
            
            // Early exit if both old and new vouchers are empty
            if (oldVouhersOnProducts.IsNullOrEmpty() && modifiedProductVouchers.IsNullOrEmpty())
            {
                _logger.LogInformation("No updates on product vouchers (both null)");
                succesCodes[4] = 1;
                
            }
            
            // Handle the case where all product vouchers are removed
            if (modifiedProductVouchers.IsNullOrEmpty())
            {
                _logger.LogInformation("All product vouchers were removed");
                await vouchersOnProductsRepository.DeleteRangeAsync(oldVouhersOnProducts);
                succesCodes[4] = 1;
                
            }
            
            var updatedVouchers = new List<ProduseCuVouchere>();


            if (!modifiedProductVouchers.IsNullOrEmpty())
            {
                foreach (var updatedVoucher in modifiedProductVouchers!)
                {
                    var isNewVoucherInDb = await vouchereRepository
                        .FindQueryable(v => v.CodVoucher == updatedVoucher.CodVoucherDto)
                        .FirstOrDefaultAsync();

                    if (isNewVoucherInDb is null)
                    {
                        var newUpdatedVoucher = new Vouchere
                        {
                            CodVoucher = updatedVoucher.CodVoucherDto,
                            Reducere = updatedVoucher.ReducereDto / 100,
                            DataExpirare = updatedVoucher.ExpirareDto
                        };

                        await vouchereRepository.AddAsync(newUpdatedVoucher);
                        await _unitOfWork.CommitAsync();
                        isNewVoucherInDb = newUpdatedVoucher;
                    }

                    var isNewVoucherLinkedWithProductInDb = await vouchersOnProductsRepository
                        .FindQueryable(pv => pv.IdProdus == productId
                                             && pv.IdVoucher == isNewVoucherInDb.IdVoucher)
                        .FirstOrDefaultAsync();


                    if (isNewVoucherLinkedWithProductInDb is null)
                    {
                        var newUpdatedDimensionLinkedWithProduct = new ProduseCuVouchere
                        {
                            IdProdus = productId,
                            IdVoucher = isNewVoucherInDb.IdVoucher,
                        };
                        
                        updatedVouchers.Add(newUpdatedDimensionLinkedWithProduct);
                        
                    }
                    else
                    {
                        oldVouhersOnProducts.Remove(isNewVoucherLinkedWithProductInDb);
                    }

                }

                if (oldVouhersOnProducts.Count != 0)
                {
                    _logger.LogInformation("Removing old vouchers ");
                    await vouchersOnProductsRepository.DeleteRangeAsync(oldVouhersOnProducts);
                }

                if (updatedVouchers.Count != 0)
                {
                    _logger.LogInformation("Adding new vouchers ");

                    await vouchersOnProductsRepository.AddRangeAsync(updatedVouchers);
                }

                succesCodes[4] = 1;
            }
            
            return succesCodes;
        }
        catch (Exception e)
        {
            if (modifiyingTransaction is not null)
            {
                _logger.LogInformation("Rolling back transaction");
                await _unitOfWork.RollBackTransactionAsync(modifiyingTransaction);
            }
            Console.WriteLine(e);
            throw;
        }
        
        return [];
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
                        Greutate = produseDto.GreutateDto,
                        FataReversibila = produseDto.FataReversibilaDto,
                        Stoc = produseDto.StocDto,
                        IsDeleted = produseDto.IsDeletedDto,
                        ActivInMagazin = produseDto.ActivInMagazinDto,
                        TipulProdusului = produseDto.TipProdusDto,
                        IdProducator = idProducator,
                        Producator = manufacturer
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
                        Greutate = produseDto.GreutateDto,
                        FataReversibila = produseDto.FataReversibilaDto,
                        Stoc = produseDto.StocDto,
                        IsDeleted = produseDto.IsDeletedDto,
                        ActivInMagazin = produseDto.ActivInMagazinDto,
                        TipulProdusului = produseDto.TipProdusDto,
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
                            throw new DbUpdateException($"Product code already used : {produseDto.CodProdusDto}");
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
                        Greutate = produseDto.GreutateDto,
                        FataReversibila = produseDto.FataReversibilaDto,
                        Stoc = produseDto.StocDto,
                        IsDeleted = produseDto.IsDeletedDto,
                        ActivInMagazin = produseDto.ActivInMagazinDto,
                        TipulProdusului = produseDto.TipProdusDto,
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
                        Greutate = produseDto.GreutateDto,
                        FataReversibila = produseDto.FataReversibilaDto,
                        Stoc = produseDto.StocDto,
                        IsDeleted = produseDto.IsDeletedDto,
                        ActivInMagazin = produseDto.ActivInMagazinDto,
                        TipulProdusului = produseDto.TipProdusDto,
                        IdProducator = null
                    };
                    
                    _logger.LogDebug("After creating the product without productor");


                    _mapper.Map(updatedProduct, findProductAlreadyInDb);
                    
                    _logger.LogDebug("After mapping2");
                    
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
                
                // List to keep track of items to remove after the iteration
                // cazul 1: 
                // imagini vechi : A B C 
                // imagini noi : A B C D
                // cazul 2: 
                // imagini vechi : A B C
                // imagini noi : A B
                // cazul 3: 
                // imagini vechi : A B C
                // imagini noi : A B C
                // cazul 4: 
                // imagini vechi : A B C
                // imagini noi : E F
                // cazul 5: 
                // imagini vechi : A B C
                // imagini noi : E F G H
                // cazul 6: 
                // imagini vechi : E F
                // imagini noi : A B C


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

    public async Task<int> DeleteTypeOnProduct(string codProdus, string tipProdus, string categorieProdus)
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
                .FindQueryable(t => t.Categorie == categorieProdus && t.TipProdus == tipProdus)
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
            if (deleteTransaction is null)
            {
                _logger.LogInformation("An error occured, rolling back transaction");
                await _unitOfWork.RollBackTransactionAsync(deleteTransaction);
            }
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
            if (deleteTransaction is null)
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
            if (deleteTransaction is null)
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
            if (deleteTransaction is null)
            {
                _logger.LogInformation("An error occured, rolling back transaction");
                _logger.LogInformation($"{e.Message}");

                await _unitOfWork.RollBackTransactionAsync(deleteTransaction);
            }

            return -1;
        }
    }

    public async Task<int> DeleteVoucherOnProduct(string codProdus, string codVoucher)
    {
        var productRepository = _unitOfWork.Repository<Produse>();
        var voucherRepository = _unitOfWork.Repository<Vouchere>();
        var vouchersOnProductsRepository = _unitOfWork.Repository<ProduseCuVouchere>();
        
        IDbContextTransaction? deleteTransaction = null;
        try
        {
          
            deleteTransaction = await _unitOfWork.BeginTransactionAsync();
           
            var product = await productRepository
                .FindQueryable(p => p.CodProdus == codProdus)
                .FirstOrDefaultAsync();

            if (product is null)
            {
                _logger.LogInformation("Product not found to delete the image on it");
                return 0;
            }

            var voucher = await voucherRepository
                .FindQueryable(p => p.CodVoucher == codVoucher)
                .FirstOrDefaultAsync();
            
            
            if (voucher is null)
            {
                _logger.LogInformation("Voucher that is associated with the product not found");
                return 0;
            }

            var voucherOnProduct = await vouchersOnProductsRepository
                .FindQueryable(pv => pv.IdVoucher == voucher.IdVoucher &&
                                     pv.IdProdus == product.IdProdus)
                .FirstOrDefaultAsync();
            
            
            if (voucherOnProduct is null)
            {
                _logger.LogInformation("Voucher that is associated in the join tables with the product not found");
                return 0;
            }


            await vouchersOnProductsRepository.DeleteAsync(voucherOnProduct);
            await _unitOfWork.CommitTransactionAsync(deleteTransaction);
            _logger.LogInformation("Voucher on product was deleted succesufully");
            return 1;
        }
        catch (Exception e)
        {
            if (deleteTransaction is null)
            {
                _logger.LogInformation("An error occured, rolling back transaction");
                _logger.LogInformation($"{e.Message}");

                await _unitOfWork.RollBackTransactionAsync(deleteTransaction);
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
            .Select(group => new KeyValuePair<string?, int>(
                group.Key, 
                group.Select(prod => prod.IdProducator).FirstOrDefault()))
            .ToListAsync();


        var vouchereDtos = await _unitOfWork.Repository<Vouchere>()
            .GetSimpleQueryable()
            .Select(v => new VouchereDto
            {
                CodVoucherDto = v.CodVoucher,
                ReducereDto = v.Reducere,
                ExpirareDto = v.DataExpirare,
                JustAdded = false
            })
            .ToListAsync();
        
        var productCategories = await _unitOfWork.Repository<TipuriProduse>()
            .GetSimpleQueryable()
            .GroupBy(tip => tip.Categorie)
            .Select(g => g.Key)
            .ToListAsync();
    
        var productTypes = await _unitOfWork.Repository<TipuriProduse>()
            .GetSimpleQueryable()
            .GroupBy(tip => tip.TipProdus)
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
    
        var directoriesInS3Bucket = await _unitOfWork.Repository<Imagini>()
            .GetSimpleQueryable()
            .GroupBy(imag => imag.FisierInBucket)
            .Select(g => g.Key)
            .ToListAsync();
    
        var productOptions = new ProductOptionsForComboBox
        {
            ProductTypesForBox = productTypes,
            ProductCategoriesForBox = productCategories,
            LatimiForBox = widths,
            LungimiForBox = heights,
            RecomandariForBox = bedRecommendations,
            CoduriCuloriForBox = colorCodes,
            CuloriForBox = colors,
            DirectoriesInBucket = directoriesInS3Bucket,
            VoucherCodes = vouchereDtos,
            ManuFacturersDto = manuFacturersDto
        };
    
        return productOptions;
    }

    
}