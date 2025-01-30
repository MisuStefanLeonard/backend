
using System.Security.Authentication;
using System.Security.Claims;
using AutoMapper;
using E_Commerce_BackEnd.Models.DTO;
using E_Commerce_BackEnd.Models.DTO.AdminRelatedDtos.Accounts;
using E_Commerce_BackEnd.Models.DTO.AdminRelatedDtos.OrdersDto;
using E_Commerce_BackEnd.Models.DTO.Recaptcha;
using E_Commerce_BackEnd.Models.OrderRelatedModels;
using E_Commerce_BackEnd.Models.UserRelatedModels;
using E_Commerce_BackEnd.Services.emailService;
using E_Commerce_BackEnd.Services.Helpers.AWS_Secret;
using E_Commerce_BackEnd.Services.Helpers.UserHelpers;
using E_Commerce_BackEnd.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;


namespace E_Commerce_BackEnd.Services.uService;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ITokenService _tokenService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<Conturi> _logger;
    private const string ReCaptchaURL = "https://www.google.com/recaptcha/api/siteverify";
    private const int Size = 30;
    private const int Size2 = 30;


    public UserService(IUnitOfWork unitOfWork, IMapper mapper,
        IEmailService emailService, ITokenService tokenService,
        IMemoryCache cache, ILogger<Conturi> logger, IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _emailService = emailService;
        _tokenService = tokenService;
        _cache = cache;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task AddAccountAsync(Conturi newAccount)
    {
        IDbContextTransaction? transaction = null;

        try
        {
            transaction = await _unitOfWork.BeginTransactionAsync();
            var repository = _unitOfWork.Repository<Conturi>();

            var token = UserHelpers.Token(Size, Size2, newAccount.Email!);
            var hashedPassword = UserHelpers.CryptPassword(newAccount.Parola!);

            ICollection<Adrese> newAdrese = new HashSet<Adrese>();
            var accountToCreate = new Conturi(newAccount.Nume, newAccount.Prenume, newAccount.Gen, newAccount.NrTelefon,
                newAccount.Username!, newAccount.Email!, hashedPassword, newAccount.DataCreare, token,
                newAccount.Verificat, newAccount.Rol, DateTime.UtcNow, newAdrese);
            
            await repository.AddAsync(accountToCreate);

            await _unitOfWork.CommitTransactionAsync(transaction);


            await _emailService.SendEmailAsync(accountToCreate.Email!, "Confirmation email from texx.ro",
                $"<a href='http://localhost:3000/user/confirmare/{token}'>" +
                "Da click pe acest link pentru a-ti activa contul</a>" +
                "<br><p>Acest mail va expira intr-o ora!</p> " +
                "<p>In caz de expirare, " +
                "aveti optiunea de a-l retrimite.</p>");



        }
        catch (DbUpdateException e)
        {
            if (transaction != null)
            {
                await _unitOfWork.RollBackTransactionAsync(transaction);
            }

            _logger.LogError("Someting happened when creating an account: Error Message:" + e.Message);
            _logger.LogError("Someting happened when creating an account: Stacktrace :" + e.StackTrace);
            throw;
        }

    }

    public async Task<int> LogoutAsync(string refreshToken)
    {
        IDbContextTransaction? deleteRefreshTokenTransaction = null;
        try
        {
            _logger.LogInformation(refreshToken);
            deleteRefreshTokenTransaction = await _unitOfWork.BeginTransactionAsync();
            var getRefreshTokenFromDb = await _unitOfWork.Repository<RememberUser>()
                .FindQueryable(token => token.SessionToken == refreshToken)
                .FirstOrDefaultAsync();

            if (getRefreshTokenFromDb == null)
            {
                return 1; // token already deleted | not present
            }

            await _unitOfWork.Repository<RememberUser>().DeleteAsync(getRefreshTokenFromDb);
            await _unitOfWork.CommitTransactionAsync(deleteRefreshTokenTransaction);
            return 1; // succes delete
        }
        catch (Exception e)
        {
            if (deleteRefreshTokenTransaction != null)
            {
                await _unitOfWork.RollBackTransactionAsync(deleteRefreshTokenTransaction);
                _logger.LogError(e.Message);
                _logger.LogError(e.StackTrace);

            }
            return -1;
        }
    }

    public async Task<string> GenerateRefreshToken()
    {
        var getToken = _tokenService.RefreshToken();
        await Task.CompletedTask;
        return getToken;
        
    }

    public async Task<Conturi?> CreateAccountBasedOnGoogleLogIn(IEnumerable<Claim> currentClaims)
    {
        IDbContextTransaction? transaction = null;
        try
        {
            transaction = await _unitOfWork.BeginTransactionAsync();

            var claimsList = currentClaims.ToList();
            
            var googleEmail = claimsList.FirstOrDefault(claim => claim.Type == ClaimTypes.Email)!.Value;
           
            var conturiRepository = _unitOfWork.Repository<Conturi>();

            var isUserInDb = await conturiRepository.FindQueryable(c => c.Email == googleEmail)
                .FirstOrDefaultAsync();

            if (isUserInDb != null)
            {
                await _unitOfWork.CommitTransactionAsync(transaction);
                return isUserInDb;
            }

            var surname = claimsList.FirstOrDefault(claim => claim.Type == ClaimTypes.Surname)!.Value;
            var givenname = claimsList.FirstOrDefault(claim => claim.Type == ClaimTypes.GivenName)!.Value;
            var googleGeneratedUserName = surname + givenname;

            ICollection<Adrese> newAdrese = new HashSet<Adrese>();

            var googleUser = new Conturi(givenname, surname, null, null,
                googleGeneratedUserName, googleEmail, null, DateTime.UtcNow,
                "google", true, "Client", DateTime.UtcNow, newAdrese);

            await conturiRepository.AddAsync(googleUser);
            await _unitOfWork.CommitTransactionAsync(transaction);

            return googleUser;
        }
        catch (Exception e)
        {
            if (transaction != null)
            {
                await _unitOfWork.RollBackTransactionAsync(transaction);
            }

            Console.WriteLine(e);
            return null;
        }

    }

    public async Task<LoginDto?> LoginAccountAsync(LoginDto loginDto)
    {
        IDbContextTransaction? addRefreshTokenTransaction = null;
        try
        {
            addRefreshTokenTransaction = await _unitOfWork.BeginTransactionAsync();
            var conturiRepository = _unitOfWork.Repository<Conturi>();
            var plainTextPassword = loginDto.ParolaProp;
            var currentUser =  loginDto.NumeProp.Contains('@')
                ? await conturiRepository.FindQueryable(user => user.Email == loginDto.NumeProp).FirstOrDefaultAsync() 
                : await conturiRepository.FindQueryable(user => user.Username == loginDto.NumeProp).FirstOrDefaultAsync();

            RememberUser newRefreshToken;
            var rememberUserRepository = _unitOfWork.Repository<RememberUser>();
            
            if (currentUser is not null && plainTextPassword == null)
            {
                _logger.LogInformation("GOOGLE AUTH");
                var tokenForGoogleUser = await _tokenService.GenerateJwtAccesToken(currentUser);
                var refreshTokenForGoogleUser = _tokenService.RefreshToken();
                loginDto.TokenProp = tokenForGoogleUser;
                loginDto.RoleProp = currentUser.Rol;
                loginDto.RefreshTokenProp = refreshTokenForGoogleUser;
                newRefreshToken = new RememberUser
                {
                    IdCont = currentUser.IdCont,
                    SessionToken = refreshTokenForGoogleUser,
                    IssuedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(7)
                };
                await rememberUserRepository.AddAsync(newRefreshToken);
                await _unitOfWork.CommitTransactionAsync(addRefreshTokenTransaction);
                return loginDto;
            }
            
           
            if (currentUser is null || !UserHelpers.VerifyCryptedPassword(plainTextPassword!, currentUser.Parola!))
            {
                throw new UnauthorizedAccessException("Account does not exist / Wrong password or username");
            }


            if (currentUser == null)
            {
                throw new InvalidCredentialException("Username or password are wrong");
            }

            if (currentUser.Verificat == false)
            {
                throw new UnauthorizedAccessException("Account not verified");
            }

            var tokenForCurrentUser = await _tokenService.GenerateJwtAccesToken(currentUser);
            var refreshTokenForCurrentUser = _tokenService.RefreshToken();
            loginDto.TokenProp = tokenForCurrentUser;
            loginDto.RoleProp = currentUser.Rol;
            loginDto.RefreshTokenProp = refreshTokenForCurrentUser;

           
            newRefreshToken = new RememberUser
            {
                IdCont = currentUser.IdCont,
                SessionToken = refreshTokenForCurrentUser,
                IssuedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            await rememberUserRepository.AddAsync(newRefreshToken);
            await _unitOfWork.CommitTransactionAsync(addRefreshTokenTransaction);
            return loginDto;
        }
        catch (InvalidCredentialException e)
        {
            if (addRefreshTokenTransaction != null)
            {
                await _unitOfWork.RollBackTransactionAsync(addRefreshTokenTransaction);
            }
            _logger.LogError("Invalid credentials");
            return null;
        }
        catch (UnauthorizedAccessException e)
        {
            if (addRefreshTokenTransaction != null)
            {
                await _unitOfWork.RollBackTransactionAsync(addRefreshTokenTransaction);
            }
            _logger.LogError("Account has not been activated ");
            return null;
        }
        catch (Exception e)
        {
            if (addRefreshTokenTransaction != null)
            {
                await _unitOfWork.RollBackTransactionAsync(addRefreshTokenTransaction);
            }
            _logger.LogError("General error occured");
            _logger.LogError(e.StackTrace);
            _logger.LogError(e.Message);

            return null;
        }



    }

    
    public async Task<ConturiDto> GetProfileDataAsync(int userId)
    {
        
        var watch = System.Diagnostics.Stopwatch.StartNew();

        var cacheKey = $"ProfileData_{userId}";

        if (_cache.TryGetValue(cacheKey, out ConturiDto? cachedData))
        {
            watch.Stop();
            _logger.LogInformation($"In cache it took {watch.ElapsedMilliseconds}");
            return cachedData!;
        }

        var repository = _unitOfWork.Repository<Conturi>();

        var currentAccountData = await repository.GetByIdAsync(userId);

        var currentAccountDto = _mapper.Map<ConturiDto>(currentAccountData);

        _cache.Set(cacheKey, currentAccountDto, TimeSpan.FromMinutes(10));

        return currentAccountDto;
    }

    public async Task<int> EmailChangingOrUpdatingUserDataAsync(int userId, ConturiDto updatedDto)
    {
        IDbContextTransaction? transaction = null;
        try
        {
            transaction = await _unitOfWork.BeginTransactionAsync();

            var repository = _unitOfWork.Repository<Conturi>();
            var currentUser = await repository.GetByIdAsync(userId);
            var oldEmail = currentUser!.Email;
            
            var newDataFromUser = new Conturi
            {
                IdCont = currentUser.IdCont,
                Email = updatedDto.Email!,
                Parola = currentUser.Parola,
                AdreseConturi = currentUser.AdreseConturi,
                CodActivare = currentUser.CodActivare,
                Verificat = true,
                Rol = currentUser.Rol,
                DataCreare = currentUser.DataCreare,
                Gen = updatedDto.Gen,
                NrTelefon = updatedDto.NrTelefon,
                Username = currentUser.Username,
                Nume = updatedDto.Nume,
                Prenume = updatedDto.Prenume,
                OraLinkConfirmare = currentUser.OraLinkConfirmare,

            };

            if (updatedDto.Email == currentUser.Email)
            {
                

                _mapper.Map(newDataFromUser, currentUser);

                await repository.UpdateAsync(currentUser);

                await _unitOfWork.CommitTransactionAsync(transaction);
                
                var cacheKey = $"ProfileData_{currentUser.IdCont}";
                
                if (_cache.TryGetValue(cacheKey, out ConturiDto? cachedData))
                {
                    var replaceOldCacheDto = _mapper.Map<ConturiDto>(newDataFromUser);

                    _cache.Set(cacheKey, replaceOldCacheDto, TimeSpan.FromMinutes(10));
                }
                else
                {
                    _logger.LogInformation("No data to update in the cache");
                }
                

                return 0;
            }
            
            var token = UserHelpers.Token(Size, Size2, oldEmail!);
            currentUser.CodActivare = token;
            currentUser.OraLinkConfirmare = DateTime.UtcNow;

            await _emailService.SendEmailAsync(updatedDto.Email!, "Schimbare email cont texx.ro",
                "V-ati schimbat e-mail-ul contului dumneavoastra" +
                $"<br><p>Noul email este : {updatedDto.Email}</p> " +
                $"<p><a href='http://localhost:3000/user/account/changeEmail/{token}?email={updatedDto.Email}&nume={updatedDto.Nume}&prenume={updatedDto.Prenume}&gen={updatedDto.Gen}&nrTelefon={updatedDto.NrTelefon}&username={updatedDto.Username}'>" +
                "Intrati pe acest link ca schimbarea sa aiba loc.</a></p>" +
                $"<p>Toate ofertele si contactul vor fi realizate pe mail-ul pe care l-ati setat</p>" +
                "<p>Va asteptam la cumparaturi la noi pe site!</p>" +
                "<p>ATENTIE! Daca nu ati fost dumneavoastra cel care a solicitat schimbare de mail, contactati-ne in cel mai scurt timp posibil!" +
                "ACEST MAIL VA EXPIRA INTR-O ORA. Repetati procesul daca acest mail a expirat");

            await repository.UpdateAsync(currentUser);

            await _unitOfWork.CommitTransactionAsync(transaction);

            return 1;
            

        }
        catch (DbUpdateException e)
        {
            if (transaction == null!)
            {
                await _unitOfWork.RollBackTransactionAsync(transaction!);
            }

            _logger.LogError(e.Message);
            return -1;
        }
    }

    public async Task<int> ModifyUserDataIfEmailHasChangedAsync(string token, ConturiDto updatedDto)
    {
        IDbContextTransaction transaction = null!;
        try
        {
            transaction = await _unitOfWork.BeginTransactionAsync();

            var conturiRepository = _unitOfWork.Repository<Conturi>();
            var currentUser = await conturiRepository.FindQueryable(user => user.CodActivare == token)
                .FirstOrDefaultAsync();

            if (currentUser == null)
            {
                return -1;
            }

            var now = DateTime.UtcNow;
            if ((now - currentUser.OraLinkConfirmare).TotalHours > 1)
            {
                return -1;
            }


            var newDataFromUser = new Conturi
            {
                IdCont = currentUser.IdCont,
                Email = updatedDto.Email!,
                Parola = currentUser.Parola,
                AdreseConturi = currentUser.AdreseConturi,
                CodActivare = currentUser.CodActivare,
                Verificat = true,
                Rol = currentUser.Rol,
                DataCreare = currentUser.DataCreare,
                Gen = updatedDto.Gen,
                NrTelefon = updatedDto.NrTelefon,
                Username = currentUser.Username,
                Nume = updatedDto.Nume,
                Prenume = updatedDto.Prenume,
                OraLinkConfirmare = currentUser.OraLinkConfirmare,

            };
            _mapper.Map(newDataFromUser, currentUser);

            await conturiRepository.UpdateAsync(currentUser);

            await _unitOfWork.CommitTransactionAsync(transaction);
            
            
            var cacheKey = $"ProfileData_{currentUser.IdCont}";
                
            if (_cache.TryGetValue(cacheKey, out ConturiDto? cachedData))
            {
                var replaceOldCacheDto = _mapper.Map<ConturiDto>(newDataFromUser);
                
                _cache.Set(cacheKey, replaceOldCacheDto, TimeSpan.FromMinutes(10));
            }
            else
            {
                _logger.LogInformation("No data to update in the cache");
            }

            return 1;

        }
        catch (DbUpdateException e)
        {
            if (transaction == null!)
            {
                await _unitOfWork.RollBackTransactionAsync(transaction!);
            }

            _logger.LogError(e.Message);
            return 0;


        }
    }

    public async Task<int> ContactAdmin(ContactDetails detaliiContact)
    {
        try
        {
            var key = _configuration.GetSection("reCAPTCHA").GetSection("secret").Value;
            using var client = new HttpClient();
            var parameters = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("secret", key!),
                new KeyValuePair<string, string>("response", detaliiContact.CaptchaToken),
                new KeyValuePair<string, string>("remoteip", string.Empty)
            });
            var response = await client.PostAsync(ReCaptchaURL, parameters);
            
            if (!response.IsSuccessStatusCode)
            {
               _logger.LogError($"Failed to verify reCAPTCHA. Status code: {response.StatusCode}");
                return 0;
            }
            
            var jsonResponse = await response.Content.ReadAsStringAsync();
            _logger.LogInformation(jsonResponse);
            var recaptchaResult = JsonConvert.DeserializeObject<RecaptchaResponse>(jsonResponse);

            // Check the success and score (optional)
            if (recaptchaResult is { Success: true } && recaptchaResult.Score >= 0.5)
            {
               _logger.LogInformation("reCAPTCHA verification succeeded.");
            }
            else
            {
                _logger.LogError($"reCAPTCHA verification failed. Error codes: {string.Join(", ", recaptchaResult.ErrorCodes)}");
                return 0;
            }
            
            var phoneNumber = detaliiContact.NrTelefon ?? "Nespecificat";
            var orderNumber = detaliiContact.NumarComanda ?? "Nespecificat";
            await _emailService.SendEmailAsync(detaliiContact.Email, $"{detaliiContact.MotivContact}",
                $"<p>E-mail : {detaliiContact.Email} </p>" +
                $"<p>Numar de telefon : {phoneNumber}</p>" +
                $"<p>Numar comanda : {orderNumber} </p>" +
                $"<p>Motiv contact : {detaliiContact.MotivContact} </p>" +
                $"<p>Descriere problema :{detaliiContact.Descriere} </p>");

            return 1;
        }
        catch (Exception e)
        {
            _logger.LogError($"Exception : {e.GetType()}");
            _logger.LogError(e.Message);
            _logger.LogError(e.StackTrace);

            return -1;
        }
      
    }
    
    public async Task<bool> CheckForgotPasswordTokenLifeTime(string token)
    {

        try
        {

            var conturiRepository = _unitOfWork.Repository<Conturi>();

            var user = await conturiRepository.FindQueryable(user => user.CodActivare == token)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                throw new DbUpdateException("User does not exist");
            }

            DateTime currentDateTime = DateTime.UtcNow;
            if ((currentDateTime - user.OraLinkConfirmare).TotalHours > 1)
            {
                throw new InvalidCredentialException("Forgot password token expired!");
            }

            return true;



        }
        catch (InvalidCredentialException e)
        {


            _logger.LogError(
                "Someting happened when checking the token(ForgotPassword)  (InvalidCredentials) :  Error Message:" +
                e.Message);
            _logger.LogError(
                "Someting happened when checking the token(ForgotPassword)  (InvalidCredentials):  Stacktrace:" +
                e.StackTrace);
            return false;
        }
    }

    public async Task<int> ForgotPasswordAsync(string email)
    {
        IDbContextTransaction? transaction = null;

        try
        {
            transaction = await _unitOfWork.BeginTransactionAsync();
            var conturiRepository = _unitOfWork.Repository<Conturi>();

            var accountToBeUpdated = await conturiRepository.FindQueryable(user => user.Email == email)
                .FirstOrDefaultAsync();

            if (accountToBeUpdated == null)
            {
                throw new DbUpdateException("Account not found");
            }

            var resetToken = UserHelpers.Token(Size, Size2, email);
            accountToBeUpdated.CodActivare = resetToken;
            accountToBeUpdated.OraLinkConfirmare = DateTime.UtcNow;

            await conturiRepository.UpdateAsync(accountToBeUpdated);
            await _unitOfWork.CommitTransactionAsync(transaction);

            await _emailService.SendEmailAsync(accountToBeUpdated.Email!, "Resetare parola de la texx.ro",
                $"<a href='http://localhost:3000/user/forgotpassword/{resetToken}'>" +
                "Dati click pe acest link pentru a va reseta parola.</a>" +
                "<br/>Linkul va expira intr-o ora!");

        }
        catch (DbUpdateException e)
        {
            if (transaction == null)
            {
                await _unitOfWork.RollBackTransactionAsync(transaction!);
            }

            _logger.LogError("Someting happened when changing password an account: Error Message:" + e.Message);
            _logger.LogError("Someting happened when changing password an account: StackTrace:" + e.StackTrace);
            return -1;
        }

        return 1;
    }

    public async Task<int> ChangePasswordAsync(ChangePasswordDto changePasswordDto)
    {
        IDbContextTransaction? transaction = null;
        try
        {

            transaction = await _unitOfWork.BeginTransactionAsync();
            var conturiRepository = _unitOfWork.Repository<Conturi>();


            // here I retrieve the user from the DB
            var currentUser = await conturiRepository
                .FindQueryable(user => user.CodActivare == changePasswordDto.TokenProp)
                .FirstOrDefaultAsync();

            if (currentUser == null)
            {
                throw new DbUpdateException("User not found");
            }

            var newPassword = UserHelpers.CryptPassword(changePasswordDto.NewHashedPasswordProp);


            var updatedUser = new Conturi(currentUser.Nume, currentUser.Prenume, currentUser.Gen, currentUser.NrTelefon,
                currentUser.Username!, currentUser.Email!, newPassword, currentUser.DataCreare,
                currentUser.CodActivare, currentUser.Verificat, currentUser.Rol, currentUser.OraLinkConfirmare,
                currentUser.AdreseConturi
            );


            _mapper.Map(updatedUser, currentUser);



            await conturiRepository.UpdateAsync(currentUser);
            await _unitOfWork.CommitTransactionAsync(transaction);

            return 1;
        }
        catch (DbUpdateConcurrencyException e)
        {
            if (transaction == null)
            {
                await _unitOfWork.RollBackTransactionAsync(transaction!);
            }

            _logger.LogError("Concurency update happened. Rolling back the transaction!" + e.Message);
            return -2;
        }
        catch (DbUpdateException e)
        {
            if (transaction == null)
            {
                await _unitOfWork.RollBackTransactionAsync(transaction!);
            }

            _logger.LogError(e.Message);
            return -1;
        }
        catch (InvalidCredentialException e)
        {
            _logger.LogError(e.Message);
            return 0;
        }
    }

    public async Task<bool> CheckPasswordInDbAsync(string token, string unHashedPassword)
    {
        var user = await _unitOfWork.Repository<Conturi>()
            .FindQueryable(user => user.CodActivare == token)
            .FirstOrDefaultAsync();

        if (user == null)
        {
            return false;
        }

        var storedHashedPassword = user.Parola; // Assuming 'Parola' stores the hashed password

        var isPasswordValid = BCrypt.Net.BCrypt.EnhancedVerify(unHashedPassword, storedHashedPassword);
        Console.WriteLine(isPasswordValid);
        return isPasswordValid;
    }

    public async Task<int> ResendConfirmationMailAsync(string token)
    {
        IDbContextTransaction? transaction = null;
        try
        {
            transaction = await _unitOfWork.BeginTransactionAsync();
            var conturiRepository = _unitOfWork.Repository<Conturi>();


            var currentUser = await conturiRepository.FindQueryable(user => user.CodActivare == token)
                .FirstOrDefaultAsync();

            if (currentUser == null)
            {
                throw new DbUpdateException("No user with this token in the database");
            }

            string newToken = UserHelpers.Token(Size, Size2, currentUser.Email!);
            currentUser.CodActivare = newToken;

            DateTime currentDateTime = DateTime.UtcNow;
            currentUser.OraLinkConfirmare = currentDateTime;

            currentUser.Verificat = false;

            await conturiRepository.UpdateAsync(currentUser);
            await _unitOfWork.CommitTransactionAsync(transaction);

            await _emailService.SendEmailAsync(currentUser.Email!, "New confirmation link from texx.ro",
                $"<a href='http://localhost:3000/user/confirmare/{token}'>"
                + "Noul cod de reactivare.Da click pe acest link pentru a-ti activa contul</a>" +
                "<br><p>Acest mail va expira intr-o ora!</p>" +
                "<p>In caz de expirare ," +
                "Aveti optiunea de a-l retrimite.</p>");

            return 1;
        }
        catch (DbUpdateException e)
        {
            if (transaction == null)
            {
                await _unitOfWork.RollBackTransactionAsync(transaction!);
            }

            Console.WriteLine(e);
            return -1;
        }

    }

    public async Task<int> AccountConfirmationAsync(string token)
    {

        IDbContextTransaction? transaction = null;

        try
        {

            transaction = await _unitOfWork.BeginTransactionAsync();
            var conturiRepository = _unitOfWork.Repository<Conturi>();

            var currentUser = await conturiRepository.FindQueryable(user => user.CodActivare == token)
                .FirstOrDefaultAsync();


            if (currentUser == null)
            {
                throw new DbUpdateException("No user with this token");
            }

            DateTime currentDateTime = DateTime.UtcNow;
            if ((currentDateTime - currentUser.OraLinkConfirmare).TotalHours > 1)
            {
                throw new InvalidCredentialException("Confirmation token expired!");
            }

            currentUser.Verificat = true;

            await conturiRepository.UpdateAsync(currentUser);

            await _unitOfWork.CommitTransactionAsync(transaction);

            return 1;

        }
        catch (DbUpdateException e)
        {
            if (transaction == null)
            {
                await _unitOfWork.RollBackTransactionAsync(transaction!);

            }

            _logger.LogError(
                "Someting happened when retrieving the user for the confirmation token: Error Message:" + e.Message);
            _logger.LogError(
                "Someting happened when retrieving the user for the confirmation token: Error Message:" + e.Message);
            return 2;
        }
        catch (InvalidCredentialException e)
        {
            _logger.LogError(e.Message);
            return 3;
        }
    }

   

    public async Task<Conturi?> GetAccountByEmailAsync(string email)
    {
        return await _unitOfWork.Repository<Conturi>().FindQueryable(user => user.Email == email)
            .FirstOrDefaultAsync();
    }

    public async Task<Conturi?> GetAccountByUsernameAsync(string username)
    {
        return await _unitOfWork.Repository<Conturi>().FindQueryable(user => user.Username == username)
            .FirstOrDefaultAsync();
    }

    public async Task<IList<ConturiDisplayDto>?> GetAccountForAdmin()
    {
        var conturiRepository = _unitOfWork.Repository<Conturi>();

        var getAllAcounts = await conturiRepository.GetAllAsync();

        var accountsToDtoList = _mapper.Map<IList<ConturiDisplayDto>>(getAllAcounts);

        return accountsToDtoList;
    }

    public async Task<ConturiDtoForModification?> GetAccountData(int accountId)
    {
        var accountsRepository = _unitOfWork.Repository<Conturi>();
        var adressesRepository = _unitOfWork.Repository<Adrese>();
        var productsOnOrdersRepository = _unitOfWork.Repository<ProduseCuComenzi>();
        var accountData = await accountsRepository
            .FindQueryable(a => a.IdCont == accountId)
            .Include(a => a.AdreseConturi)!
            .ThenInclude(df => df.DetaliuFactura)
            .Include(a => a.AdreseConturi)!
                .ThenInclude(l => l.Locatie)
            .FirstAsync();
        // the address and the info for the current client
        var contDataToDto = _mapper.Map<ConturiDtoForModification>(accountData);
        var adreseToDto = _mapper.Map<IList<AdreseDto>>(accountData.AdreseConturi);
        foreach (var adressDto in adreseToDto)
        {
            contDataToDto.AdreseClient.Add(adressDto);
        }
        // now processing orders of the client

        var ordersOfTheCurrentClient = await adressesRepository
            .FindQueryable(a => a.IdCont == accountId)
            // .Include(a => a.AdreseFacturarePeComanda)
            .Include(a => a.AdreseLivrarePeComanda)
            .Include(a => a.DetaliuFactura)
            .Include(a => a.Locatie)
            .AsSplitQuery()
            .ToListAsync();
        

        var ordersList = new List<OrdersDisplayDto>();

        foreach (var address in ordersOfTheCurrentClient)
        {
            // Log the count of orders for both billing and delivery addresses
           

            // Process delivery addresses
            if (address.AdreseLivrarePeComanda!.Count > 0)
            {
                _logger.LogTrace("Processing delivery addresses to DTO");
                foreach (var order in address.AdreseLivrarePeComanda)
                {
                    var newOrdersDisplayDto = _mapper.Map<OrdersDisplayDto>(order);
                    var adresaLivrareToDto = _mapper.Map<AdreseDto>(order.CAdresaLivrare);
                    
                    // Check if billing address is the same as delivery address
                    if (order.CAdresaFacturare.IdAdresa == order.CAdresaLivrare.IdAdresa)
                    {
                        // Billing and delivery addresses are the same, so reuse the delivery address DTO
                        newOrdersDisplayDto.AdresaLivrareDto = adresaLivrareToDto;
                        newOrdersDisplayDto.AdresaFacturareDto = adresaLivrareToDto;
                    }
                    else
                    {
                        // Billing and delivery addresses are different, map separately
                        var adresaFacturareToDto = _mapper.Map<AdreseDto>(order.CAdresaFacturare);
                        newOrdersDisplayDto.AdresaLivrareDto = adresaLivrareToDto;
                        newOrdersDisplayDto.AdresaFacturareDto = adresaFacturareToDto;
                    }

                    ordersList.Add(newOrdersDisplayDto);
                }
            }
            else
            {
                _logger.LogInformation("No orders for this client");
            }

            
        }
        
        
        foreach (var order in ordersList)
        {
            // de facut proiectie!
            _logger.LogInformation("Proceesing  order");
            var allProductsOnCurrentOrder = await productsOnOrdersRepository
                .FindQueryable(pc => pc.IdComanda == order.IdComandaDto)
                    .Select(ord => new ProductsOnOrdersDto
                {
                    IdProdusDto = ord.Produs.IdProdus,
                    CodProdusDto = ord.Produs.CodProdus,
                    NumeProdusDto = ord.Produs.NumeProdus!,
                    FataReversibilaDto = ord.Produs.FataReversibila,
                    NumeProducatorDto = ord.Produs.Producator!.NumeProducator,
                    TipulProdusuluiDto = ord.Produs.TipulProdusului,
                    NumeSetDto = ord.Set!.NumeSet,
                    InaltimeSetDto = ord.InaltimeAleasaPentruSet,
                    PretBazaDto = ord.PretCumparat,
                    NumeCuloareDto = ord.PcCuloare.NumeCuloare,
                    CodCuloareDto = ord.PcCuloare.CodCuloare.CodCuloare!,
                    LungimeDto = ord.PcDimensiune!.Lungime,
                    LatimeDto = ord.PcDimensiune.Latime,
                    RecomandarePatDto = ord.PcDimensiune.RecomandarePat,
                    PerdeaEstePerecheDto = ord.PcDimensiune.PerdeaEstePereche,
                    TipGalerieCusaturaDto = ord.PcManopera!.TipGalerieLaManopera.NumeTipGalerie,
                    IncretireRejansaDto = ord.PcManopera.TipGalerieLaManopera.IncretireRejansa,
                    PretTipGalerieCusaturaDto = ord.PcManopera.PretCurentTipGalerie,
                    TipLinieCusaturaDto = ord.PcManopera.TipLinieLaManopera.NumeTipLinie,
                    PretTipLinieCusaturaDto = ord.PcManopera.PretCurentTipLinie,
                    InelePrindereDto = ord.PcManopera.InelPrindereLaManopera!.CuloareInel,
                    TotalMetruMaterial = ord.PcDimensiune == null ? 0 : ord.PcManopera.MaterialFolosit,
                    VoucherFolositDto = ord.Comanda.VoucherPeComanda != null,
                    CodVoucherFolositDto = ord.Comanda.VoucherPeComanda!.CodVoucher,
                    ReducereVoucherDto =  ord.Comanda.VoucherPeComanda!.Reducere * 100,
                    NrBucatiDto = ord.NrBucati
                }).ToListAsync();
    
            

            var orderedSets = allProductsOnCurrentOrder
                .AsQueryable()
                .Where(p => p.NumeSetDto != null)
                .OrderBy(p => p.NumeSetDto)
                .GroupBy(p => p.NumeSetDto)
                .ToList();
            
            var simpleProducts = allProductsOnCurrentOrder
                .AsQueryable()
                .Where(p => p.NumeSetDto == null)
                .OrderBy(p => p.NumeSetDto)
                .ToList();

            order.ProduseCuComenziDto = simpleProducts;
            order.Seturi = orderedSets;
            
            foreach (var product in order.ProduseCuComenziDto)
            {
                if (!string.Equals(product.TipulProdusuluiDto , "perdea") && !string.Equals(product.TipulProdusuluiDto , "draperie") 
                                                                          && product.NumeSetDto == null)
                {
                    order.PretTotalComanda += product.PretBazaDto * product.NrBucatiDto;
                }

                if (product.TipulProdusuluiDto is not ("perdea" or "draperie")) continue;
                var totalMetri = product.TotalMetruMaterial;
            
                var pretManoperaMaterial = product.PretBazaDto * totalMetri;
                var pretManoperaCusatura = product.PretTipGalerieCusaturaDto * totalMetri;
                var pretTipLinieCusatura = product.PretTipLinieCusaturaDto * totalMetri;
                var totalManopera = pretManoperaMaterial + pretManoperaCusatura + pretTipLinieCusatura;
                order.PretTotalComanda += totalManopera;
            }

            foreach (var set in order.Seturi)
            {
                foreach (var product in set)
                {
                    order.PretTotalComanda += product.PretBazaDto * product.NrBucatiDto;
                    break;
                }
                
            }

        }
        
        foreach (var orderDto in ordersList)
        {
            
            contDataToDto.ComenziClient.Add(orderDto);
        }
        
        return contDataToDto;
    }
}
