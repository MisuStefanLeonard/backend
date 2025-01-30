using System.Security.Claims;
using E_Commerce_BackEnd.Models.DTO;
using E_Commerce_BackEnd.Models.DTO.AdminRelatedDtos.Accounts;
using E_Commerce_BackEnd.Models.OrderRelatedModels;
using E_Commerce_BackEnd.Models.UserRelatedModels;

namespace E_Commerce_BackEnd.Services.uService;

public interface IUserService
{
    #region LogIn/Register
    public Task<LoginDto?> LoginAccountAsync(LoginDto loginDto); // 
    public Task AddAccountAsync(Conturi newAccount); //
    public Task<int> LogoutAsync(string refreshToken); // 
    public Task<string> GenerateRefreshToken(); // 
    #endregion
    
    #region CRUD

    public Task<Conturi?> CreateAccountBasedOnGoogleLogIn(IEnumerable<Claim> claims); // 
    #endregion

    #region UserProfile

    public Task<ConturiDto> GetProfileDataAsync(int userId); //
    public Task<int> EmailChangingOrUpdatingUserDataAsync(int userId,ConturiDto updatedDto); //

    public Task<int> ModifyUserDataIfEmailHasChangedAsync (string token,ConturiDto updatedDto); //
    

    #endregion
    
    #region userHelperMethods

    public Task<int> ContactAdmin(ContactDetails detaliiContact);
    public Task<bool> CheckForgotPasswordTokenLifeTime(string token); // 
    /// <summary>
    /// Method where the user enters his/her email and the new request for the changing password is sent via mail
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public Task<int> ForgotPasswordAsync(string email); // 
    /// <summary>
    /// The method where the user changed his password .
    /// </summary>
    /// <param name="changePasswordDto">the Dto for changing the password</param>
    ///
    /// <returns></returns>
    public Task<int> ChangePasswordAsync(ChangePasswordDto changePasswordDto); // 

    /// <summary>
    /// The method where we check if the password is the same as the old one
    /// </summary>
    /// <param name="token">token dummy to check in db</param>
    /// <param name="unHashedPassword">unhashed password</param>
    /// <returns></returns>
    public Task<bool> CheckPasswordInDbAsync(string token , string unHashedPassword); // 
    /// <summary>
    /// The method where you can send the confirmation link again.
    /// </summary>
    /// <param name="email">Email of the user</param>
    /// <returns></returns>
    public Task<int> ResendConfirmationMailAsync(string email); // 
    /// <summary>
    /// The method where you confirm your account
    /// </summary>
    /// <param name="token">token assigned to the created user</param>
    /// <returns></returns>
    public Task<int> AccountConfirmationAsync(string token); // 
    
    #endregion

    #region auxiliaryMethods
    
    public Task<Conturi?> GetAccountByEmailAsync(string email);
    public Task<Conturi?> GetAccountByUsernameAsync(string username);
    #endregion

    #region Admin
    public Task<IList<ConturiDisplayDto>?> GetAccountForAdmin();
    public Task<ConturiDtoForModification?> GetAccountData(int accountId);


    #endregion

}