using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ShoppingCartDtos;
using E_Commerce_BackEnd.Models.ProductRelatedModels;
using E_Commerce_BackEnd.Services.Helpers.AWS_Secret;
using E_Commerce_BackEnd.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Sqids;

namespace E_Commerce_BackEnd.Services.uShoppingCartService;

public class CartService : ICartService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CartService> _logger;
    private readonly SqidsEncoder<int> _sqidsEncoder;


    public CartService(IUnitOfWork unitOfWork, ILogger<CartService> logger, SqidsEncoder<int> sqidsEncoder)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _sqidsEncoder = sqidsEncoder;
    }

    public async Task<KeyValuePair<int, string>> AddOrUpdateCart(int idCont,SetOnCartDto? setOnCartItem , ProductOnCartDto? productOnCart)
    {
        IDbContextTransaction? addOrUpdateTransaction = null;
        try
        {
            
            addOrUpdateTransaction = await _unitOfWork.BeginTransactionAsync();
            var cartRepository = _unitOfWork.Repository<CosCumparaturi>();

            if (setOnCartItem != null)
            {
                var setToBeAdded = new List<CosCumparaturi>();
                var setToBeUpdated = new List<CosCumparaturi>();
                if (_sqidsEncoder.Decode(setOnCartItem.EncodedIdSet) is [var decodedId]
                    && setOnCartItem.EncodedIdSet == _sqidsEncoder.Encode(decodedId))
                {
                    
                    foreach (var productInSet in setOnCartItem.ProductsInCart)
                    {
                        var isSetAlreadyInCart = await cartRepository
                            .FindQueryable(c => c.IdCont == idCont
                                                && c.IdProdus == productInSet.IdProdus
                                                && c.IdCuloare == productInSet.IdCuloare
                                                && c.IdDimensiune == productInSet.IdDimensiune
                                                && c.IdManopera == productInSet.IdManopera
                                                && c.IdSet == decodedId)
                            .FirstOrDefaultAsync();

                        if (isSetAlreadyInCart == null)
                        {
                            var newItemInCart = new CosCumparaturi
                            {
                                CantitateProdus = 1,
                                PretProdus = productInSet.PretCurent,
                                IdCont = idCont,
                                IdProdus = productInSet.IdProdus,
                                IdCuloare = productInSet.IdCuloare,
                                IdDimensiune = productInSet.IdDimensiune,
                                IdSet = decodedId,
                                IdManopera = productInSet.IdManopera
                            };

                            setToBeAdded.Add(newItemInCart);
                           
                        }
                        else
                        {
                            isSetAlreadyInCart.CantitateProdus++;
                            setToBeUpdated.Add(isSetAlreadyInCart);
                        }
                       
                        
                    }

                    if (setToBeAdded.Count > 0)
                    {
                        await cartRepository.AddRangeAsync(setToBeAdded);
                        await _unitOfWork.CommitTransactionAsync(addOrUpdateTransaction);
                        return new KeyValuePair<int, string>(1, "Succesfully added (set) item to cart");
                    }

                    if (setToBeUpdated.Count > 0)
                    {
                        await cartRepository.AddRangeAsync(setToBeUpdated);
                        await _unitOfWork.CommitTransactionAsync(addOrUpdateTransaction);
                        return new KeyValuePair<int, string>(2, "Succesfully incremented (set) item quantity");
                    }
                }
                else
                {
                    return new KeyValuePair<int, string>(0, "Set id could not be decoded");
                }
            }

            if (productOnCart != null)
            {
                var isProductAlreadyInCart = await cartRepository
                    .FindQueryable(c => c.IdCont == idCont
                                        && c.IdProdus == productOnCart.IdProdus
                                        && c.IdCuloare == productOnCart.IdCuloare
                                        && c.IdDimensiune == productOnCart.IdDimensiune
                                        && c.IdManopera == productOnCart.IdManopera
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
                        IdDimensiune = productOnCart.IdDimensiune,
                        IdSet = null,
                        IdManopera = productOnCart.IdManopera
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
    
    public async Task<int> DeleteFromCart(int idCont, SetOnCartDto? setOnCartItemOnDelete,ProductOnCartDto? productOnCartToDelete)
    {
       IDbContextTransaction? deleteOrUpdateTransaction = null;
        try
        {
            
            deleteOrUpdateTransaction = await _unitOfWork.BeginTransactionAsync();
            var cartRepository = _unitOfWork.Repository<CosCumparaturi>();


            if (setOnCartItemOnDelete != null)
            {
                var setToBeRemoved = new List<CosCumparaturi>();
                var setToBeUpdated = new List<CosCumparaturi>();
                if (_sqidsEncoder.Decode(setOnCartItemOnDelete.EncodedIdSet) is [var decodedId]
                    && setOnCartItemOnDelete.EncodedIdSet == _sqidsEncoder.Encode(decodedId))
                {
                    
                    foreach (var productInSet in setOnCartItemOnDelete.ProductsInCart)
                    {
                        var isSetAlreadyInCart = await cartRepository
                            .FindQueryable(c => c.IdCont == idCont
                                                && c.IdProdus == productInSet.IdProdus
                                                && c.IdCuloare == productInSet.IdCuloare
                                                && c.IdDimensiune == productInSet.IdDimensiune
                                                && c.IdManopera == productInSet.IdManopera
                                                && c.IdSet == decodedId)
                            .FirstOrDefaultAsync();

                        if (isSetAlreadyInCart != null)
                        {
                            switch (isSetAlreadyInCart.CantitateProdus)
                            {
                                case 1:
                                    setToBeRemoved.Add(isSetAlreadyInCart);
                                    break;
                                case > 1:
                                    isSetAlreadyInCart.CantitateProdus--;
                                    setToBeUpdated.Add(isSetAlreadyInCart);
                                    break;
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException("Set variation could not be found. Throwing exception");
                        }
                        
                    }

                    if (setToBeRemoved.Count > 0)
                    {
                        await cartRepository.DeleteRangeAsync(setToBeRemoved);
                        await _unitOfWork.CommitTransactionAsync(deleteOrUpdateTransaction);
                        _logger.LogInformation("Succesfully removed set from cart");
                        return 1;
                    }

                    if (setToBeUpdated.Count > 0)
                    {
                        await cartRepository.UpdateRangeAsync(setToBeUpdated);
                        await _unitOfWork.CommitTransactionAsync(deleteOrUpdateTransaction);
                        _logger.LogInformation("Succesfully decremented set quantity from cart");
                        return 2;
                    }
                }
                else
                {
                   throw new InvalidOperationException("Set encoded id could not be decoded");
                }
            }

            if (productOnCartToDelete != null)
            {
                var isProductAlreadyInCart = await cartRepository
                    .FindQueryable(c => c.IdCont == idCont
                                        && c.IdProdus == productOnCartToDelete.IdProdus
                                        && c.IdCuloare == productOnCartToDelete.IdCuloare
                                        && c.IdDimensiune == productOnCartToDelete.IdDimensiune
                                        && c.IdManopera == productOnCartToDelete.IdManopera
                                        && c.IdSet == null)
                    .FirstOrDefaultAsync();

                if (isProductAlreadyInCart != null)
                {
                    switch (isProductAlreadyInCart.CantitateProdus)
                    {
                        case 1:
                            await cartRepository.DeleteAsync(isProductAlreadyInCart);
                            await _unitOfWork.CommitTransactionAsync(deleteOrUpdateTransaction);
                            _logger.LogInformation("Succesfully removed product from cart");
                            return 1;
                        case > 1:
                            isProductAlreadyInCart.CantitateProdus--;
                            await cartRepository.UpdateAsync(isProductAlreadyInCart);
                            await _unitOfWork.CommitTransactionAsync(deleteOrUpdateTransaction);
                            _logger.LogInformation("Succesfully decremented product quantity from cart");
                            return 2;
                    }
                }
                else
                {
                    throw new InvalidOperationException("Product variation could not be found. Throwing exception");
                }

            }
            _logger.LogError("Reacher here. Not good!");
            return 0;
        }
        catch (Exception e)
        {
            if (deleteOrUpdateTransaction != null)
            {
                await _unitOfWork.RollBackTransactionAsync(deleteOrUpdateTransaction);
            }

            if (e is InvalidOperationException)
            {
                return -2;
            }
            
            _logger.LogError($"EXCEPTION: {e.GetType()}");
            _logger.LogError($"ERROR : {e.Message}" );
            return -1;
        }
    }
}