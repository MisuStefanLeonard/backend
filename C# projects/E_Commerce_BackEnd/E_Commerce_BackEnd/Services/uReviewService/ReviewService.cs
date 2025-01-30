using System.IdentityModel.Tokens.Jwt;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.ReviewsDto;
using E_Commerce_BackEnd.Models.ProductRelatedModels;
using E_Commerce_BackEnd.Models.UserRelatedModels;
using E_Commerce_BackEnd.Services.Helpers.AWS_Secret;
using E_Commerce_BackEnd.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.IdentityModel.Tokens;
using Sqids;

namespace E_Commerce_BackEnd.Services.uReviewService;

public class ReviewService : IReviewService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly ILogger<ReviewService> _logger;
    private readonly SqidsEncoder<int> _sqidsEncoder;

    public ReviewService(IUnitOfWork unitOfWork, ILogger<ReviewService> logger, ITokenService tokenService, SqidsEncoder<int> sqidsEncoder)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _tokenService = tokenService;
        _sqidsEncoder = sqidsEncoder;
    }

    public async Task<KeyValuePair<int, string>> PostReview(ReviewReceivedDto review , string token, string refreshToken)
    {
        IDbContextTransaction? insertTransaction = null;
        try
        {
            insertTransaction = await _unitOfWork.BeginTransactionAsync();
            var isSet = false;
            Produse productToBeReviewed = new();
            Seturi setToBeReviewed = new();
            if (review.IdSet != null)
            {
                if (_sqidsEncoder.Decode(review.IdSet) is [var decodedId]
                    && review.IdSet == _sqidsEncoder.Encode(decodedId))
                {
                    isSet = true;
                    review.IdSet = decodedId.ToString();
                }
                else
                {
                    throw new Exception("Invalid id decoding of set");
                }
                
            }
            
            if (!isSet)
            {
                var productsRepository = _unitOfWork.Repository<Produse>();
                productToBeReviewed = await productsRepository
                    .FindQueryable(product => product.CodProdus == review.CodProdus.ToUpper() 
                                              && product.ActivInMagazin
                                              && !product.IsDeleted)
                    .FirstAsync();
            }
            else
            {
                var setsRepository = _unitOfWork.Repository<Seturi>();
                setToBeReviewed = await setsRepository
                    .FindQueryable(set => set.IdSet == int.Parse(review.IdSet!)
                                          && set.SetActivInMagazin
                                          && !set.IsDeleted)
                    .FirstAsync();
            }
           
            var usersRepository = _unitOfWork.Repository<Conturi>();
            
            var userClaims = await _tokenService.TokenValidation(token,refreshToken);
           
            var userId = int.Parse(userClaims.Item1!
                .FindFirst(claim => claim.Type == "user_id")!.Value.ToString());
           
            var userAccount = await usersRepository
                .FindQueryable(user => user.IdCont == userId)
                .FirstAsync();
            
            var reviewsRepository = _unitOfWork.Repository<Reviews>();

            Reviews reviewToBePosted;

            if (!isSet)
            {
                reviewToBePosted = new Reviews
                {
                    IdProdus = productToBeReviewed.IdProdus,
                    IdSet = null,
                    IdCont = userAccount.IdCont,
                    NumarStele = review.StarsReview,
                    TextRecenzie = review.TextReview
                };
            }
            else
            {
                reviewToBePosted = new Reviews
                {
                    IdProdus = null,
                    IdSet = setToBeReviewed.IdSet,
                    IdCont = userAccount.IdCont,
                    NumarStele = review.StarsReview,
                    TextRecenzie = review.TextReview
                };
            }
            

            await reviewsRepository.AddAsync(reviewToBePosted);
            await _unitOfWork.CommitTransactionAsync(insertTransaction);


            return new KeyValuePair<int, string>(1, "Review adaugat cu success");


        }
        catch (Exception e)
        {
            if (insertTransaction != null)
            {
                if (e is ArgumentNullException or InvalidOperationException)
                {
                    _logger.LogError(e.Message);
                    _logger.LogError(e.StackTrace);

                    await _unitOfWork.RollBackTransactionAsync(insertTransaction);
                    return new KeyValuePair<int, string>(0, "Produsul/Setul nu a fost gasit ");
                }
                _logger.LogError(e.Message);
                _logger.LogError(e.StackTrace);
                await _unitOfWork.RollBackTransactionAsync(insertTransaction);
                return new KeyValuePair<int, string>(-1, "Eroare generala");
            }
            
        }

        return new KeyValuePair<int, string>(2, "Reached here , not good");

    }
}