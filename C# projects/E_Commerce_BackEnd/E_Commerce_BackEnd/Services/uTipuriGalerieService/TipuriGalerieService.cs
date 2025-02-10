using AutoMapper;
using E_Commerce_BackEnd.CustomExceptions;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.BulkOperationsDto;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.TipuriGalerieDtos;
using E_Commerce_BackEnd.Models.ProductRelatedModels;
using E_Commerce_BackEnd.Services.Helpers.AWS_Secret.AWSBucket_CRUD;
using E_Commerce_BackEnd.Services.uBucketService;
using E_Commerce_BackEnd.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.IdentityModel.Tokens;

namespace E_Commerce_BackEnd.Services.uTipuriGalerieService;

public class TipuriGalerieService : ITipuriGalerieService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TipuriGalerieService> _logger;
    private readonly IMapper _mapper;
    private readonly IBucketAcces _bucketAcces;

    public TipuriGalerieService( IMapper mapper, ILogger<TipuriGalerieService> logger, IUnitOfWork unitOfWork, IBucketAcces bucketAcces)
    {
        _mapper = mapper;
        _logger = logger;
        _unitOfWork = unitOfWork;
        _bucketAcces = bucketAcces;
    }


    public async Task<IList<TipuriGalerieDisplayDto>?> GetAllTipuriGalerie()
    {
        var tipuriGalerieRepository = _unitOfWork.Repository<TipuriGalerie>();

        var listOfTipuriGalerie = await tipuriGalerieRepository
            .FindQueryable(tl => !tl.IsDeleted)
            .ToListAsync();

        var toDtoList = _mapper.Map<IList<TipuriGalerieDisplayDto>>(listOfTipuriGalerie);
       
        return toDtoList;
    }

    public async Task<IList<string>?> GetAllTipGalerieNames()
    {
        var tipuriGalerieRepository = _unitOfWork.Repository<TipuriGalerie>();

        var listOfTipuriGalerie = await tipuriGalerieRepository
            .GetSimpleQueryable()
            .Select(tp => tp.NumeTipGalerie)
            .ToListAsync();

        return listOfTipuriGalerie;
    }

    public async Task<TipuriGalerieDto?> GetCurrentTipGalerie(int idTipGalerie)
    {
        var tipuriGalerieRepository = _unitOfWork.Repository<TipuriGalerie>();

        var tipGalerieToReturn = await tipuriGalerieRepository
            .FindQueryable(tp => tp.IdTipGalerie == idTipGalerie)
            .FirstOrDefaultAsync();

        if (tipGalerieToReturn is null)
        {
            _logger.LogError("tip galerie to be returned not found");
            return null;
        }

        var url = "";

        if (tipGalerieToReturn.CaleRelativa is not null)
        {
            var presignedUrl = await _bucketAcces.GenerateUrl(tipGalerieToReturn.CaleRelativa, "tipuri_galerie");
            if (presignedUrl is not null)
            {
                url = presignedUrl;
            }
        }

        var tipGalerieDto = new TipuriGalerieDto
        {
            NumeTipGalerieDto = tipGalerieToReturn.NumeTipGalerie,
            PretTipGalerieDto = tipGalerieToReturn.PretTipGalerie,
            CaleRelativa = tipGalerieToReturn.CaleRelativa,
            IncretireDto = tipGalerieToReturn.IncretireRejansa,
            PresignedUrl =  url,
            SePrindeCuIneleDto = tipGalerieToReturn.SePrindeCuInele
        };

        return tipGalerieDto;
    }

    public async Task<int?> DeleteTipGalerie(int idTipGalerie)
    {
        IDbContextTransaction? deleteTransaction = null;
        var tipuriGalerieRepository = _unitOfWork.Repository<TipuriGalerie>();
        try
        {
            deleteTransaction = await _unitOfWork.BeginTransactionAsync();
            var tipGalerieToBeDeleted = await tipuriGalerieRepository
                .FindQueryable(tp => tp.IdTipGalerie == idTipGalerie)
                    .Include(tp => tp.TipGalerieManopere)
                .FirstOrDefaultAsync();

            if (tipGalerieToBeDeleted is null)
            {
                throw new Exception("tip galerie  to be deleted is not in the database");
            }

            if (tipGalerieToBeDeleted.IsLocked)
            {
                return -3; // Tip galeri loced , client is buying;
            }

            if (tipGalerieToBeDeleted.CaleRelativa is not null)
            {
                // $"images/{bucketFolder}/{imageName}"
                await _bucketAcces.DeleteFromBucket($"images/tipuri_galerie/{tipGalerieToBeDeleted.CaleRelativa}");
                
                
                _logger.LogInformation("Succesfully deleted the image associated with the current tip galerie");
                
            }
            else
            {
                _logger.LogInformation("No image to delete here (DeleteTipGalerie)");
            }
            
            

            var hasManopereAssociated = tipGalerieToBeDeleted.TipGalerieManopere.IsNullOrEmpty();
            if (hasManopereAssociated)
            {
                await tipuriGalerieRepository.DeleteAsync(tipGalerieToBeDeleted);
            }
            else
            {
                tipGalerieToBeDeleted.IsDeleted = true;
                await tipuriGalerieRepository.UpdateAsync(tipGalerieToBeDeleted);

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

    public async Task<int> DeleteTipGalerieImage(int idTipGalerie)
    {
        IDbContextTransaction? updateTransaction = null;
        var tipuriGalerieRepository = _unitOfWork.Repository<TipuriGalerie>();

        try
        {
            updateTransaction = await _unitOfWork.BeginTransactionAsync();
            var imageToDeleteOnTipGalerie = await tipuriGalerieRepository
                .FindQueryable(tp => tp.IdTipGalerie == idTipGalerie)
                .FirstOrDefaultAsync();

            if (imageToDeleteOnTipGalerie is null)
            {
                throw new Exception("Image of tip galerie  that needs deletion is not in the database");
            }
            
            if (imageToDeleteOnTipGalerie.IsLocked)
            {
                return -3; // Tip galeri loced , client is buying;
            }

            if (imageToDeleteOnTipGalerie.CaleRelativa is not null)
            {
                await _bucketAcces.DeleteFromBucket($"images/tipuri_galerie/{imageToDeleteOnTipGalerie.CaleRelativa}");
                // await _bucketService.DeleteImageFromBucket($"{imageToDeleteOnTipGalerie.CaleRelativa}" , "tipuri_galerie");
                
                _logger.LogInformation("Succesfully deleted the image associated with the current inel prindere");
                
            }
            else
            {
                _logger.LogInformation("No image for this product to delete");
                await _unitOfWork.CommitTransactionAsync(updateTransaction);
                return 1;
            }

            imageToDeleteOnTipGalerie.CaleRelativa = null;
            await tipuriGalerieRepository.UpdateAsync(imageToDeleteOnTipGalerie);
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
        var tipuriGalerieRepository = _unitOfWork.Repository<TipuriGalerie>();

        IDbContextTransaction? deleteBulkTransaction = null;

        try
        {
            deleteBulkTransaction = await _unitOfWork.BeginTransactionAsync();
            List<TipuriGalerie> tipuriGalerieToBeDeleted = [];
            List<TipuriGalerie> tipuriGalerieToBeUpdatedToDeleted = [];
            

            foreach (var item in deleteSelected.SelectedItemsToDoBulkOperations!)
            {
                var tipGalerieToBeDeleted = await tipuriGalerieRepository
                    .FindQueryable(tp => tp.IdTipGalerie == int.Parse(item.ToString()!))
                    .Include(tp => tp.TipGalerieManopere)
                    .FirstOrDefaultAsync();
                

                if (tipGalerieToBeDeleted is not null)
                {
                    if (tipGalerieToBeDeleted.IsLocked)
                    {
                        return -3; // Gallery locked
                    }
                    
                    var hasManopereOnTipGalerie = tipGalerieToBeDeleted.TipGalerieManopere.IsNullOrEmpty();
                    if (hasManopereOnTipGalerie)
                    {
                        tipuriGalerieToBeDeleted.Add(tipGalerieToBeDeleted);
                    }
                    else
                    {
                        tipuriGalerieToBeUpdatedToDeleted.Add(tipGalerieToBeDeleted);
                    }
                }
                else
                {
                    _logger.LogInformation("Tip galerie already has been deleted (bulk operation delete)");
                }

            }

            if (tipuriGalerieToBeUpdatedToDeleted.Count != 0)
            {
                foreach (var item in tipuriGalerieToBeUpdatedToDeleted)
                {
                    if (item.CaleRelativa is not null)
                    {
                        await _bucketAcces.DeleteFromBucket($"images/tipuri_galerie/{item.CaleRelativa}");
                        // await _bucketService.DeleteImageFromBucket($"{item.CaleRelativa}" , "tipuri_galerie");
                        item.CaleRelativa = null;
                        item.IsDeleted = true;
                    }
                    else
                    {
                        _logger.LogInformation("The material has no images associated (bulk operation delete)");
                    }
                }
                await tipuriGalerieRepository.UpdateRangeAsync(tipuriGalerieToBeUpdatedToDeleted);
            }
            
            if (tipuriGalerieToBeDeleted.Count != 0)
            {
                foreach (var item in tipuriGalerieToBeDeleted)
                {
                    if (item.CaleRelativa is not null)
                    {
                        await _bucketAcces.DeleteFromBucket($"images/tipuri_galerie/{item.CaleRelativa}");

                        // await _bucketService.DeleteImageFromBucket($"{item.CaleRelativa}" , "tipuri_galerie");
                    }
                    else
                    {
                        _logger.LogInformation("The tip galerie has no images associated (bulk operation delete)");
                    }
                }
                await tipuriGalerieRepository.DeleteRangeAsync(tipuriGalerieToBeDeleted);
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

    public async Task<int> ModifyOrAddTipGalerie(TipuriGalerieDto tipuriGalerieDto, IFormFileCollection? image, int idTipGalerie,
        bool isAdded)
    {
        var tipuriGalerieRepository = _unitOfWork.Repository<TipuriGalerie>();
        IDbContextTransaction? addOrUpdateTransaction = null;

        try
        {
            addOrUpdateTransaction = await _unitOfWork.BeginTransactionAsync();

            if (isAdded)
            {
                // Check if the image name already exists in the database
                var imageNameAlreadyInDb = await tipuriGalerieRepository
                    .FindQueryable(tp => !string.IsNullOrEmpty(tipuriGalerieDto.CaleRelativa) 
                                        && tp.CaleRelativa == tipuriGalerieDto.CaleRelativa)
                    .FirstOrDefaultAsync();

                if (imageNameAlreadyInDb != null)
                {
                    throw new DuplicateNameException("Duplicate name for image");
                }

                // Add new material
                var newTipGalerie = new TipuriGalerie
                {
                    NumeTipGalerie = tipuriGalerieDto.NumeTipGalerieDto,
                    PretTipGalerie = tipuriGalerieDto.PretTipGalerieDto,
                    CaleRelativa = tipuriGalerieDto.CaleRelativa,
                    IncretireRejansa = tipuriGalerieDto.IncretireDto,
                    SePrindeCuInele = tipuriGalerieDto.SePrindeCuIneleDto,
                };

                if (image != null && !string.IsNullOrEmpty(newTipGalerie.CaleRelativa))
                {
                    using var imageStream = new MemoryStream();
                    await image[0].CopyToAsync(imageStream);
                    imageStream.Position = 0;
                    await _bucketAcces.AddOrUpdateToBucket(imageStream, "tipuri_galerie", newTipGalerie.CaleRelativa);
                }
             
                // await _bucketService.UploadImageToBucket(image, newTipGalerie.CaleRelativa, "tipuri_galerie");
                await tipuriGalerieRepository.AddAsync(newTipGalerie);
            }
            else
            {
                // Update existing tip galerie
                var tipGalerieToBeModified = await tipuriGalerieRepository
                    .FindQueryable(tp => tp.IdTipGalerie == idTipGalerie)
                    .FirstOrDefaultAsync();

                if (tipGalerieToBeModified == null)
                {
                    throw new NullReferenceException("tip galerie to be modified is null");
                }

                if (tipGalerieToBeModified.IsLocked)
                {
                    return -3;
                }

                var oldImageName = tipGalerieToBeModified.CaleRelativa;
                // case both null
                // Check if image name has changed
                if (tipuriGalerieDto.CaleRelativa != oldImageName)
                {
                    if (tipuriGalerieDto.CaleRelativa == null)
                    {
                        if (oldImageName is not null)
                        {
                            await _bucketAcces.DeleteFromBucket($"images/tipuri_galerie/{oldImageName}");

                            // await _bucketService.DeleteImageFromBucket(oldImageName, "tipuri_galerie");
                        }
                      
                    }
                    else if (oldImageName != image![0].FileName)
                    {
                        // Replace the image
                        if (oldImageName is not null)
                        {
                            await _bucketAcces.DeleteFromBucket($"images/tipuri_galerie/{oldImageName}");
                            using var imageStream = new MemoryStream();
                            await image[0].CopyToAsync(imageStream);
                            imageStream.Position = 0;
                            await _bucketAcces.AddOrUpdateToBucket(imageStream, "tipuri_galerie", image[0].FileName);
                            // await _bucketService.UploadImageToBucket(image, image[0].FileName, "tipuri_galerie");
                        }
                        else
                        {
                            await _bucketAcces.DeleteFromBucket($"images/tipuri_galerie/{oldImageName}");
                            using var imageStream = new MemoryStream();
                            await image[0].CopyToAsync(imageStream);
                            imageStream.Position = 0;
                            await _bucketAcces.AddOrUpdateToBucket(imageStream, "tipuri_galerie", image[0].FileName);
                        }
                    }
                }else if (tipuriGalerieDto.CaleRelativa == oldImageName  &&
                          oldImageName is null)
                {
                    _logger.LogInformation("Both new and old images null . ");
                }
                else
                {
                    _logger.LogInformation("Same image , no need for replacement");

                }

                _mapper.Map(tipuriGalerieDto, tipGalerieToBeModified);
                await tipuriGalerieRepository.UpdateAsync(tipGalerieToBeModified);
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