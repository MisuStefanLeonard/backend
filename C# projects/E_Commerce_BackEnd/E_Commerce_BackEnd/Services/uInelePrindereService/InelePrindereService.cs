using AutoMapper;
using E_Commerce_BackEnd.CustomExceptions;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.BulkOperationsDto;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.InelePrindereDtos;
using E_Commerce_BackEnd.Models.ProductRelatedModels;
using E_Commerce_BackEnd.Models.ProductRelatedModels.JSON_Models;
using E_Commerce_BackEnd.Services.Helpers.AWS_Secret.AWSBucket_CRUD;
// using E_Commerce_BackEnd.Services.uBucketService;
using E_Commerce_BackEnd.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.IdentityModel.Tokens;

namespace E_Commerce_BackEnd.Services.uInelePrindereService;

public class InelePrindereService : IInelePrindereService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<InelePrindereService> _logger;
    private readonly IMapper _mapper;
    private readonly IBucketAcces _bucketAcces;

    public InelePrindereService(IUnitOfWork unitOfWork, ILogger<InelePrindereService> logger, IMapper mapper, IBucketAcces bucketAcces)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _mapper = mapper;
        
        _bucketAcces = bucketAcces;
    }

    public async Task<IList<IneleDisplayDto>?> GetAllInelePrindere()
    {
        var inelePrindereRepository = _unitOfWork.Repository<InelePrindere>();

        var listOfInele = await inelePrindereRepository
            .FindQueryable(i => !i.IsDeleted)
            .ToListAsync();

        var toDtoList = _mapper.Map<IList<IneleDisplayDto>>(listOfInele);

        return toDtoList;
    }

    public async Task<IList<Culoare>?> GetAllIneleColors()
    {
        var inelePrindereRepository = _unitOfWork.Repository<InelePrindere>();

        var listOfAllIneleColors = await inelePrindereRepository
            .GetSimpleQueryable()
            .Select(i => i.CuloareInelJson)
            .ToListAsync();

        return listOfAllIneleColors;
    }

    public async Task<IneleDto?> GetCurrentInelPage(int idInelPrindere)
    {
        
        var inelePrindereRepository = _unitOfWork.Repository<InelePrindere>();

        var inelToReturn = await inelePrindereRepository
            .FindQueryable(i => i.IdInel == idInelPrindere)
            .FirstOrDefaultAsync();

        if (inelToReturn is null)
        {
            _logger.LogError("Inel to be returned not found");
            return null;
        }

        var url = "";

        if (inelToReturn.CaleRelativa is not null)
        {
            var presignedUrl = await _bucketAcces.GenerateUrl(inelToReturn.CaleRelativa, "inele_prindere");
            if (presignedUrl is not null)
            {
                url = presignedUrl;
            }
        }

        var inelDto = new IneleDto
        {
            // CuloareInelDto = inelToReturn.CuloareInel,
            CuloareInelJsonDto = inelToReturn.CuloareInelJson,
            CaleRelativa = inelToReturn.CaleRelativa,
            PresignedUrl =  url
            
        };

        return inelDto;
    }

    public async Task<int?> DeleteInelPrindere(int idInelPrindere)
    {
        IDbContextTransaction? deleteTransaction = null;
        var inelePrindereRepository = _unitOfWork.Repository<InelePrindere>();
        try
        {
            deleteTransaction = await _unitOfWork.BeginTransactionAsync();
            var inelToBeDeleted = await inelePrindereRepository
                .FindQueryable(i => i.IdInel == idInelPrindere)
                    .Include(m => m.InelPeManopere)
                .FirstOrDefaultAsync();

            if (inelToBeDeleted is null)
            {
                throw new Exception("Inel to be deleted is not in the database");
            }
            
            if (inelToBeDeleted.IsLocked)
            {
                return -3;
            }

            if (inelToBeDeleted.CaleRelativa is not null)
            {
                await _bucketAcces.DeleteFromBucket($"images/inele_prindere/{inelToBeDeleted.CaleRelativa}");
                // await _bucketService.DeleteImageFromBucket($"{inelToBeDeleted.CaleRelativa}" , "inele_prindere");
                
                _logger.LogInformation("Succesfully deleted the image associated with the current inel prindere");
                
            }
            else
            {
                _logger.LogInformation("No image to delete here (DeleteInelPrindere)");
            }

            var hasManopereAssociated = inelToBeDeleted.InelPeManopere.IsNullOrEmpty();
            if (hasManopereAssociated)
            {
                await inelePrindereRepository.DeleteAsync(inelToBeDeleted);
            }
            else
            {
                inelToBeDeleted.IsDeleted = true;
                await inelePrindereRepository.UpdateAsync(inelToBeDeleted);

            }
            
            await _unitOfWork.CommitTransactionAsync(deleteTransaction);
            return 1;

        }
        catch (Exception e)
        {
            if (deleteTransaction is not null)
            {
                await _unitOfWork.RollBackTransactionAsync(deleteTransaction);
            }
            _logger.LogError(e.Message);
            return -1;
        }
    }

    public async Task<int> DeleteInelPrindereImage(int idInelPrindere)
    {
        IDbContextTransaction? updateTransaction = null;
        var inelePrindereRepository = _unitOfWork.Repository<InelePrindere>();
        try
        {
            updateTransaction = await _unitOfWork.BeginTransactionAsync();
            var imageToDeleteOnInel = await inelePrindereRepository
                .FindQueryable(i => i.IdInel == idInelPrindere)
                .FirstOrDefaultAsync();

            if (imageToDeleteOnInel is null)
            {
                throw new Exception("Image of inel that needs deletion is not in the database");
            }
            
            if (imageToDeleteOnInel.IsLocked)
            {
                return -3;
            }

            if (imageToDeleteOnInel.CaleRelativa is not null)
            {
                await _bucketAcces.DeleteFromBucket($"images/inele_prindere/{imageToDeleteOnInel.CaleRelativa}");

                // await _bucketService.DeleteImageFromBucket($"{imageToDeleteOnInel.CaleRelativa}" , "inele_prindere");
                
                _logger.LogInformation("Succesfully deleted the image associated with the current inel prindere");
                
            }
            else
            {
                _logger.LogInformation("No image for this product to delete");
                await _unitOfWork.CommitTransactionAsync(updateTransaction);
                return 1;
            }

            imageToDeleteOnInel.CaleRelativa = null;
            await inelePrindereRepository.UpdateAsync(imageToDeleteOnInel);
            await _unitOfWork.CommitTransactionAsync(updateTransaction);
            return 1;

        }
        catch (Exception e)
        {
            if (updateTransaction is not null)
            {
                await _unitOfWork.RollBackTransactionAsync(updateTransaction);
            }
            _logger.LogError(e.Message);
            return -1;
        }
    }

    public async Task<int> DeleteSelected(BulkOperationsDto deleteSelected)
    {
        var inelePrindereRepository = _unitOfWork.Repository<InelePrindere>();

        IDbContextTransaction? deleteBulkTransaction = null;

        try
        {
            deleteBulkTransaction = await _unitOfWork.BeginTransactionAsync();
            List<InelePrindere> ineleToBeDeleted = [];
            List<InelePrindere> ineleToBeUpdatedToDeleted = [];

            foreach (var item in deleteSelected.SelectedItemsToDoBulkOperations!)
            {
                var inelToBeDeleted = await inelePrindereRepository
                    .FindQueryable(i => i.IdInel == int.Parse(item.ToString()!))
                        .Include(m => m.InelPeManopere)
                    .FirstOrDefaultAsync();

                if (inelToBeDeleted is not null)
                {
                    
                    if (inelToBeDeleted.IsLocked)
                    {
                        return -3;
                    }
                    var hasManopereOnInel = inelToBeDeleted.InelPeManopere.IsNullOrEmpty();
                    if (hasManopereOnInel)
                    {
                        ineleToBeDeleted.Add(inelToBeDeleted);
                    }
                    else
                    {
                        ineleToBeUpdatedToDeleted.Add(inelToBeDeleted);
                    }
                    
                }
                else
                {
                    _logger.LogInformation("Inel already has been deleted (bulk operation delete)");
                }

            }
            

            if (ineleToBeDeleted.Count != 0)
            {
                foreach (var item in ineleToBeDeleted)
                {
                    if (item.CaleRelativa is not null)
                    {
                        // await _bucketService.DeleteImageFromBucket($"{item.CaleRelativa}", "inele_prindere");
                        await _bucketAcces.DeleteFromBucket($"images/inele_prindere/{item.CaleRelativa}");

                    
                    }
                    else
                    {
                        _logger.LogInformation("The material has no images associated (bulk operation delete)");
                    }
                }

                await inelePrindereRepository.DeleteRangeAsync(ineleToBeDeleted);
            }

            if (ineleToBeUpdatedToDeleted.Count != 0)
            {
                foreach (var item in ineleToBeUpdatedToDeleted) 
                {
                    if (item.CaleRelativa is not null)
                    {
                        // await _bucketService.DeleteImageFromBucket($"{item.CaleRelativa}", "inele_prindere");
                        await _bucketAcces.DeleteFromBucket($"images/inele_prindere/{item.CaleRelativa}");
                        item.CaleRelativa = null;
                        item.IsDeleted = true;
                    }
                    else
                    {
                        _logger.LogInformation("The material has no images associated (bulk operation delete)");
                    }
                }
                await inelePrindereRepository.UpdateRangeAsync(ineleToBeUpdatedToDeleted);

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

    public async Task<int> ModifyOrAddInelPrindere(IneleDto ineleDto, IFormFileCollection? image, int idInelPrindere, bool isAdded)
    {
        var inelePrindereRepository = _unitOfWork.Repository<InelePrindere>();
        IDbContextTransaction? addOrUpdateTransaction = null;

        try
        {
            addOrUpdateTransaction = await _unitOfWork.BeginTransactionAsync();

            if (isAdded)
            {
                // Check if the image name already exists in the database
                var imageNameAlreadyInDb = await inelePrindereRepository
                    .FindQueryable(i => !string.IsNullOrEmpty(ineleDto.CaleRelativa) && i.CaleRelativa == ineleDto.CaleRelativa)
                    .FirstOrDefaultAsync();

                if (imageNameAlreadyInDb != null)
                {
                    throw new DuplicateNameException("Duplicate name for image");
                }

                // Add new material
                var newInel = new InelePrindere
                {
                    // CuloareInel = ineleDto.CuloareInelDto,
                    CuloareInelJson = ineleDto.CuloareInelJsonDto,
                    CaleRelativa = ineleDto.CaleRelativa
                };

                if (image != null && !string.IsNullOrEmpty(newInel.CaleRelativa))
                {
                    using var imageStream = new MemoryStream();
                    await image[0].CopyToAsync(imageStream);
                    imageStream.Position = 0;
                    await _bucketAcces.AddOrUpdateToBucket(imageStream, "inele_prindere", newInel.CaleRelativa);
                }
                
                // await _bucketService.UploadImageToBucket(image, newInel.CaleRelativa, "inele_prindere");
                await inelePrindereRepository.AddAsync(newInel);
            }
            else
            {
                // Update existing material
                var inelToBeModified = await inelePrindereRepository
                    .FindQueryable(i => i.IdInel == idInelPrindere)
                    .FirstOrDefaultAsync();

                if (inelToBeModified == null)
                {
                    throw new NullReferenceException("Inel to be modified is null");
                }
                
                if (inelToBeModified.IsLocked)
                {
                    return -3;
                }

                var oldImageName = inelToBeModified.CaleRelativa;

                // Check if image name has changed
                if (ineleDto.CaleRelativa != oldImageName)
                {
                    if (ineleDto.CaleRelativa == null)
                    {
                        if (oldImageName is not null)
                        {
                            // await _bucketService.DeleteImageFromBucket(oldImageName, "inele_prindere");
                            await _bucketAcces.DeleteFromBucket($"images/inele_prindere/{ineleDto.CaleRelativa}");

                        }
                      
                    }
                    else if (oldImageName != image![0].FileName)
                    {
                        // Replace the image
                        if (oldImageName is not null)
                        {
                            // await _bucketService.DeleteImageFromBucket(oldImageName, "inele_prindere");
                            if (!string.IsNullOrEmpty(inelToBeModified.CaleRelativa))
                            {
                                using var imageStream = new MemoryStream();
                                await image[0].CopyToAsync(imageStream);
                                imageStream.Position = 0;
                                await _bucketAcces.AddOrUpdateToBucket(imageStream, "inele_prindere", inelToBeModified.CaleRelativa);
                            }
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(inelToBeModified.CaleRelativa))
                            {
                                using var imageStream = new MemoryStream();
                                await image[0].CopyToAsync(imageStream);
                                imageStream.Position = 0;
                                await _bucketAcces.AddOrUpdateToBucket(imageStream, "inele_prindere", inelToBeModified.CaleRelativa);
                            }
                        }
                    }
                }else if (oldImageName is null  && oldImageName == ineleDto.CaleRelativa)
                {
                    _logger.LogInformation("Null images");
                }
                else
                {
                    _logger.LogInformation("Same image , no need for replacement");

                }

                _mapper.Map(ineleDto, inelToBeModified);
                await inelePrindereRepository.UpdateAsync(inelToBeModified);
            }

            // Commit the transaction if everything is successful
            await _unitOfWork.CommitTransactionAsync(addOrUpdateTransaction);
            return 1;
        }
        catch (Exception ex)
        {
            // Rollback the transaction and handle exceptions
            if (addOrUpdateTransaction != null)
            {
                await _unitOfWork.RollBackTransactionAsync(addOrUpdateTransaction);
            }

            _logger.LogError(ex.Message);

            return ex switch
            {
                NullReferenceException => -1,
                DuplicateNameException => -2,
                _ => -3
            };
        }
    }
    

}