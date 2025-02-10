using AutoMapper;
using E_Commerce_BackEnd.CustomExceptions;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.BulkOperationsDto;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.TipuriLinieDtos;
using E_Commerce_BackEnd.Models.ProductRelatedModels;
using E_Commerce_BackEnd.Services.Helpers.AWS_Secret.AWSBucket_CRUD;
using E_Commerce_BackEnd.Services.uBucketService;
using E_Commerce_BackEnd.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.IdentityModel.Tokens;

namespace E_Commerce_BackEnd.Services.uTipuriLinieService;

public class TipuriLinieService : ITipuriLinieService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TipuriLinieService> _logger;
    private readonly IMapper _mapper;
    private readonly IBucketAcces _bucketAcces;

    public TipuriLinieService(IUnitOfWork unitOfWork, ILogger<TipuriLinieService> logger, IMapper mapper, IBucketAcces bucketAcces)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _mapper = mapper;
        _bucketAcces = bucketAcces;
    }

    public async Task<IList<TipuriLinieDisplayDto>?> GetAllTipuriLinie()
    {
        var tipuriLinieRepository = _unitOfWork.Repository<TipuriLinie>();

        var listOfTipuriLinie = await tipuriLinieRepository
            .FindQueryable(tl => !tl.IsDeleted)
            .ToListAsync();

        var toDtoList = _mapper.Map<IList<TipuriLinieDisplayDto>>(listOfTipuriLinie);
       
        return toDtoList;
    }

    public async Task<IList<string>?> GetAllTipuriLinieNames()
    {
        var tipuriLinieRepository = _unitOfWork.Repository<TipuriLinie>();

        var listOfTipuriLinie = await tipuriLinieRepository
            .GetSimpleQueryable()
            .Select(tl => tl.NumeTipLinie)
            .ToListAsync();

        return listOfTipuriLinie;
    }

    public async Task<TipuriLinieDto?> GetCurrentTipLinie(int idTipLinie)
    {
        var tipuriLinieRepository = _unitOfWork.Repository<TipuriLinie>();

        var tipLinieToReturn = await tipuriLinieRepository
            .FindQueryable(tl => tl.IdTipLinie == idTipLinie)
            .FirstOrDefaultAsync();

        if (tipLinieToReturn is null)
        {
            _logger.LogError("tip linie to be returned not found");
            return null;
        }

        var url = "";

        if (tipLinieToReturn.CaleRelativa is not null)
        {
            var presignedUrl = await _bucketAcces.GenerateUrl(tipLinieToReturn.CaleRelativa, "tipuri_linie");
            if (presignedUrl is not null)
            {
                url = presignedUrl;
            }
        }

        var tipLinieDto = new TipuriLinieDto
        {
            NumeTipLinieDto = tipLinieToReturn.NumeTipLinie,
            PretPeTipLinieDto = tipLinieToReturn.PretPeTipLinie,
            CaleRelativa = tipLinieToReturn.CaleRelativa,
            PresignedUrl =  url
        };

        return tipLinieDto;
    }

    public async Task<int?> DeleteTipLinie(int idTipLinie)
    {
        IDbContextTransaction? deleteTransaction = null;
        var tipuriLinieRepository = _unitOfWork.Repository<TipuriLinie>();
        try
        {
            deleteTransaction = await _unitOfWork.BeginTransactionAsync();
            var tipLinieToBeDeleted = await tipuriLinieRepository
                .FindQueryable(tl => tl.IdTipLinie == idTipLinie)
                    .Include(tl => tl.TipLiniePeManopere)
                .FirstOrDefaultAsync();

            if (tipLinieToBeDeleted is null)
            {
                throw new Exception("tip linie  to be deleted is not in the database");
            }
            
            if (tipLinieToBeDeleted.IsLocked)
            {
                return -3; // Line tye locked.
            }

            if (tipLinieToBeDeleted.CaleRelativa is not null)
            {
                await _bucketAcces.DeleteFromBucket($"images/tipuri_linie/{tipLinieToBeDeleted.CaleRelativa}");
                // await _bucketService.DeleteImageFromBucket($"{tipLinieToBeDeleted.CaleRelativa}" , "tipuri_linie");
                
                _logger.LogInformation("Succesfully deleted the image associated with the current tip linie");
                
            }
            else
            {
                _logger.LogInformation("No image to delete here (DeleteTipLinie)");
            }

            var hasManopereAssociated = tipLinieToBeDeleted.TipLiniePeManopere.IsNullOrEmpty();
            if (hasManopereAssociated)
            {
                await tipuriLinieRepository.DeleteAsync(tipLinieToBeDeleted);
            }
            else
            {
                tipLinieToBeDeleted.IsDeleted = true;
                await tipuriLinieRepository.UpdateAsync(tipLinieToBeDeleted);

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

    public async Task<int> DeleteTipLinieImage(int idTipLinie)
    {
        IDbContextTransaction? updateTransaction = null;
        var tipuriLinieRepository = _unitOfWork.Repository<TipuriLinie>();
        try
        {
            updateTransaction = await _unitOfWork.BeginTransactionAsync();
            var imageToDeleteOnTipLinie = await tipuriLinieRepository
                .FindQueryable(tl => tl.IdTipLinie == idTipLinie)
                .FirstOrDefaultAsync();

            if (imageToDeleteOnTipLinie is null)
            {
                throw new Exception("Image of tip linie  that needs deletion is not in the database");
            }
            
            if (imageToDeleteOnTipLinie.IsLocked)
            {
                return -3;
            }

            if (imageToDeleteOnTipLinie.CaleRelativa is not null)
            {
                await _bucketAcces.DeleteFromBucket($"images/tipuri_linie/{imageToDeleteOnTipLinie.CaleRelativa}");
                // await _bucketService.DeleteImageFromBucket($"{imageToDeleteOnTipLinie.CaleRelativa}" , "tipuri_linie");
                
                _logger.LogInformation("Succesfully deleted the image associated with the current tip linie ");
                
            }
            else
            {
                _logger.LogInformation("No image for this tip linie to delete");
                await _unitOfWork.CommitTransactionAsync(updateTransaction);
                return 1;
            }

            imageToDeleteOnTipLinie.CaleRelativa = null;
            await tipuriLinieRepository.UpdateAsync(imageToDeleteOnTipLinie);
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
        var tipuriLinieRepository = _unitOfWork.Repository<TipuriLinie>();

        IDbContextTransaction? deleteBulkTransaction = null;

        try
        {
            deleteBulkTransaction = await _unitOfWork.BeginTransactionAsync();
            List<TipuriLinie> tipuriLinieToBeDeleted = [];
            List<TipuriLinie> tipuriLinieToBeUpdatedToBeDeleted = [];

            foreach (var item in deleteSelected.SelectedItemsToDoBulkOperations!)
            {
                var tipLinieToBeDeleted = await tipuriLinieRepository
                    .FindQueryable(tl => tl.IdTipLinie == int.Parse(item.ToString()!))
                    .FirstOrDefaultAsync();

                if (tipLinieToBeDeleted is not null)
                {
                    
                    if (tipLinieToBeDeleted.IsLocked)
                    {
                        return -3;
                    }

                    var hasManopereOnTipGalerie = tipLinieToBeDeleted.TipLiniePeManopere.IsNullOrEmpty();
                    if (hasManopereOnTipGalerie)
                    {
                        tipuriLinieToBeDeleted.Add(tipLinieToBeDeleted);
                    }
                    else
                    {
                        tipuriLinieToBeUpdatedToBeDeleted.Add(tipLinieToBeDeleted);
                    }
                }
                else
                {
                    _logger.LogInformation("Tip linie already has been deleted (bulk operation delete)");
                }

            }

            if (tipuriLinieToBeUpdatedToBeDeleted.Count != 0)
            {
                foreach (var item in tipuriLinieToBeUpdatedToBeDeleted)
                {
                    if (item.CaleRelativa is not null)
                    {
                        await _bucketAcces.DeleteFromBucket($"images/tipuri_linie/{item.CaleRelativa}");

                        // await _bucketService.DeleteImageFromBucket($"{item.CaleRelativa}" , "tipuri_linie");
                        item.CaleRelativa = null;
                        item.IsDeleted = true;
                    }
                    else
                    {
                        _logger.LogInformation("The material has no images associated (bulk operation delete)");
                    }
                }
                await tipuriLinieRepository.UpdateRangeAsync(tipuriLinieToBeUpdatedToBeDeleted);
            }
            
            if (tipuriLinieToBeDeleted.Count != 0)
            {
                foreach (var item in tipuriLinieToBeDeleted)
                {
                    if (item.CaleRelativa is not null)
                    {
                        await _bucketAcces.DeleteFromBucket($"images/tipuri_linie/{item.CaleRelativa}");

                        // await _bucketService.DeleteImageFromBucket($"{item.CaleRelativa}" , "tipuri_linie");
                    }
                    else
                    {
                        _logger.LogInformation("The tip galerie has no images associated (bulk operation delete)");
                    }
                }
                await tipuriLinieRepository.DeleteRangeAsync(tipuriLinieToBeDeleted);
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

    public async Task<int> ModifyOrAddTipLinie(TipuriLinieDto tipuriLinieDto, IFormFileCollection? image, int idTipLinie, bool isAdded)
    {
        var tipuriLinieRepository = _unitOfWork.Repository<TipuriLinie>();
        IDbContextTransaction? addOrUpdateTransaction = null;

        try
        {
            addOrUpdateTransaction = await _unitOfWork.BeginTransactionAsync();

            if (isAdded)
            {
                // Check if the image name already exists in the database
                var imageNameAlreadyInDb = await tipuriLinieRepository
                    .FindQueryable(tl => !string.IsNullOrEmpty(tipuriLinieDto.CaleRelativa) 
                                        && tl.CaleRelativa == tipuriLinieDto.CaleRelativa)
                    .FirstOrDefaultAsync();

                if (imageNameAlreadyInDb != null)
                {
                    throw new DuplicateNameException("Duplicate name for image");
                }

                // Add new material
                var newTipLinie = new TipuriLinie
                {
                    NumeTipLinie = tipuriLinieDto.NumeTipLinieDto,
                    PretPeTipLinie = tipuriLinieDto.PretPeTipLinieDto,
                    CaleRelativa = tipuriLinieDto.CaleRelativa
                };

                if (image != null && !string.IsNullOrEmpty(newTipLinie.CaleRelativa))
                {
                    using var imageStream = new MemoryStream();
                    await image[0].CopyToAsync(imageStream);
                    imageStream.Position = 0;
                    await _bucketAcces.AddOrUpdateToBucket(imageStream, "tipuri_linie", newTipLinie.CaleRelativa);
                }
                await tipuriLinieRepository.AddAsync(newTipLinie);
            }
            else
            {
                // Update existing tip galerie
                var tipLinieToBeModified = await tipuriLinieRepository
                    .FindQueryable(tl => tl.IdTipLinie == idTipLinie)
                    .FirstOrDefaultAsync();

                if (tipLinieToBeModified == null)
                {
                    throw new NullReferenceException("tip galerie to be modified is null");
                }
                
                if (tipLinieToBeModified.IsLocked)
                {
                    return -3;
                }

                var oldImageName = tipLinieToBeModified.CaleRelativa;
                // case both null
                // Check if image name has changed
                if (tipuriLinieDto.CaleRelativa != oldImageName)
                {
                    if (tipuriLinieDto.CaleRelativa == null)
                    {
                        if (oldImageName is not null)
                        {
                            // await _bucketService.DeleteImageFromBucket(oldImageName, "tipuri_linie");
                            await _bucketAcces.DeleteFromBucket($"images/tipuri_linie/{tipuriLinieDto.CaleRelativa}");

                        }
                      
                    }
                    else if (oldImageName != image![0].FileName)
                    {
                        // Replace the image
                        if (oldImageName is not null)
                        {
                            // await _bucketService.DeleteImageFromBucket(oldImageName, "tipuri_linie");
                            await _bucketAcces.DeleteFromBucket($"images/tipuri_linie/{tipuriLinieDto.CaleRelativa}");

                            if (!string.IsNullOrEmpty(tipuriLinieDto.CaleRelativa))
                            {
                                using var imageStream = new MemoryStream();
                                await image[0].CopyToAsync(imageStream);
                                imageStream.Position = 0;
                                await _bucketAcces.AddOrUpdateToBucket(imageStream, "tipuri_linie", tipuriLinieDto.CaleRelativa);
                            }
                        }
                        else
                        {
                            if ( !string.IsNullOrEmpty(tipuriLinieDto.CaleRelativa))
                            {
                                using var imageStream = new MemoryStream();
                                await image[0].CopyToAsync(imageStream);
                                imageStream.Position = 0;
                                await _bucketAcces.AddOrUpdateToBucket(imageStream, "tipuri_linie", tipuriLinieDto.CaleRelativa);
                            }
                        }
                    }
                }else if (tipuriLinieDto.CaleRelativa == oldImageName &&
                          oldImageName is null)
                {
                    _logger.LogInformation("Both new and old images null.");
                }
                else
                {
                    _logger.LogInformation("Same image , no need for replacement");

                }

                _mapper.Map(tipuriLinieDto, tipLinieToBeModified);
                await tipuriLinieRepository.UpdateAsync(tipLinieToBeModified);
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