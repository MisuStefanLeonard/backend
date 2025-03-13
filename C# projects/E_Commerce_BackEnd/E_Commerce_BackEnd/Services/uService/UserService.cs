
using System.Security.Authentication;
using System.Security.Claims;
using AutoMapper;
using E_Commerce_BackEnd.Models.DTO;
using E_Commerce_BackEnd.Models.DTO.AdminRelatedDtos.Accounts;
using E_Commerce_BackEnd.Models.DTO.ClientOrdersDto;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ManopereDto.ManoperaForSet;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductOptionsDto;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.ProductPage.OptionsForCurtain;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ShoppingCartDtos;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.VouchereDtos;
using E_Commerce_BackEnd.Models.DTO.Recaptcha;
using E_Commerce_BackEnd.Models.Enums;
using E_Commerce_BackEnd.Models.UserRelatedModels;
using E_Commerce_BackEnd.Services.emailService;
using E_Commerce_BackEnd.Services.Helpers.AWS_Secret;
using E_Commerce_BackEnd.Services.Helpers.AWS_Secret.AWSBucket_CRUD;
using E_Commerce_BackEnd.Services.Helpers.UserHelpers;
using E_Commerce_BackEnd.Services.uMJMLService;
using E_Commerce_BackEnd.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using NuGet.Packaging;


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
    private readonly IBucketAcces _bucketAcces;
    private readonly IMjmlService _mjmlService;
    private const string ReCaptchaUrl = "https://www.google.com/recaptcha/api/siteverify";
    private const int Size = 30;
    private const int Size2 = 30;


    public UserService(IUnitOfWork unitOfWork, IMapper mapper,
        IEmailService emailService, ITokenService tokenService,
        IMemoryCache cache, ILogger<Conturi> logger, IConfiguration configuration, IBucketAcces bucketAcces, IMjmlService mjmlService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _emailService = emailService;
        _tokenService = tokenService;
        _cache = cache;
        _logger = logger;
        _configuration = configuration;
        _bucketAcces = bucketAcces;
        _mjmlService = mjmlService;
    }

    public async Task AddAccountAsync(Conturi newAccount)
    {
        IDbContextTransaction? transaction = null;

        try
        {
            transaction = await _unitOfWork.BeginTransactionAsync();
            var repository = _unitOfWork.Repository<Conturi>();
            var getDockerEnv = Environment.GetEnvironmentVariable("DOCKER");
            _logger.LogInformation("AICI");
            _logger.LogInformation("AICI");
            _logger.LogInformation("AICI");

            _logger.LogInformation(getDockerEnv);
            var siteUrl = getDockerEnv != "true" ? "http://localhost:3000" : "https://www.texxshop.ro";
            _logger.LogInformation(siteUrl);

            var token = UserHelpers.Token(Size, Size2, newAccount.Email!);
            var hashedPassword = UserHelpers.CryptPassword(newAccount.Parola!);

            ICollection<Adrese> newAdrese = new HashSet<Adrese>();
            var accountToCreate = new Conturi(newAccount.Nume, newAccount.Prenume, newAccount.Gen, newAccount.NrTelefon,
                newAccount.Username!, newAccount.Email!, hashedPassword, newAccount.DataCreare, token,
                newAccount.Verificat, newAccount.Rol, DateTime.UtcNow, newAdrese);
            
            await repository.AddAsync(accountToCreate);

            await _unitOfWork.CommitTransactionAsync(transaction);
            
            
            var url = await _bucketAcces.GenerateUrl("LogoTexx.png" , null);
            _logger.LogInformation(url);
            var insertLogo = url != null
                ? $"<mj-section>\n" +
                  $" <mj-column>\n" +
                  $"   <mj-image width=\"100px\" src=\"{url}\" alt=\"Company Logo\"/>\n" +
                  $" </mj-column>\n" +
                  $"</mj-section>"
                : "";
            
            var mjmlTemplate = $"<mjml>\n" +
                               $"  <mj-body>\n  " +
                               $"{insertLogo}" +
                               $"  <mj-section>\n   " +
                               $"   <mj-column>\n      " +
                               $"  <mj-text font-size=\"18px\" color=\"#F45E43\" font-family=\"helvetica\" align=\"center\">Confirmare cont / Account confirmation</mj-text>\n       " +
                               $" <mj-spacer></mj-spacer>\n " +
                               $"     </mj-column>\n" +
                               $"      <mj-column background-color=\"#a8a8a8\" border-radius=\"20px\" padding=\"20px\" width=\"100%\">\n " +
                               $"        <mj-text font-size=\"22px\" color=\"#F45E43\">\n " +
                               $"         RO\n" +
                               $"        </mj-text>\n " +
                               $"        <mj-text font-size=\"18px\" color=\"#333333\">\n " +
                               $"         Acest mail expira intr-o ora!\n" +
                               $"        </mj-text>\n " +
                               $"       <mj-text font-size=\"18px\" color=\"#333333\">\n  " +
                               $"        <strong>Confirmarea de cont nou.</strong>\n" +
                               $"        </mj-text>\n        <mj-text font-size=\"18px\" color=\"blue\">\n" +
                               $"          In cazul in care nu ati fost dvs. sau nu recunoasteti acest mail, NU dati click pe nimic! Contacti-ne in cel mai scurt timp la <strong >texx@yahoo.com</strong>\n" +
                               $"        </mj-text>\n" +
                               $"       \t<mj-text font-size=\"18px\" color=\"#333333\">\n" +
                               $"          Noul cod de reactivare. Da click pe acest link pentru a-ti activa contul\n" +
                               $"         </mj-text>\n" +
                               $"          <mj-button color=\"white\" background-color=\"black\">\n" +
                               $"           <a href=\"{siteUrl}/user/confirmare/{token}\">CLICK</a>\n" +
                               $"        </mj-button>\n" +
                               $"         <mj-text font-size=\"22px\" color=\"#F45E43\">\n" +
                               $"          EN\n " +
                               $"       </mj-text>\n   " +
                               $"        <mj-text font-size=\"18px\" color=\"#333333\">\n " +
                               $"        This mail expires in 1 hour!\n" +
                               $"        </mj-text>\n " +
                               $"     <mj-text font-size=\"18px\" color=\"#333333\">\n " +
                               $"         <strong>New confirmation request for account</strong>\n  " +
                               $"      </mj-text>\n  " +
                               $"      <mj-text font-size=\"18px\" color=\"blue\">\n " +
                               $"         If you did not request this confirmation , do not click ANYTHING! Contact us as fast as possible at <strong >texx@yahoo.com</strong>\n  " +
                               $"      </mj-text>\n " +
                               $"      \t<mj-text font-size=\"18px\" color=\"#333333\">\n " +
                               $"         New reactivation code. Click on the button\n " +
                               $"        </mj-text>\n          <mj-button color=\"white\" background-color=\"black\">\n " +
                               $"          <a href=\"{siteUrl}/en/user/confirmare/{token}\">CLICK</a>\n" +
                               $"        </mj-button>\n\n" +
                               $"      </mj-column>\n" +
                               $"    </mj-section>\n" +
                               $"  </mj-body>\n" +
                               $"</mjml>";

            var convertToHtml = await _mjmlService.ConvertMjmlToHtml(mjmlTemplate);
            


            await _emailService.SendEmailAsync(accountToCreate.Email!,
                "Confirmare email / Email Confirmation ",
               convertToHtml!
            );




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

    public async Task<LoginDto?> LoginAccountAsync(LoginDto loginDto )
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
            var expiringTime = DateTime.UtcNow.AddDays(7);
            
            
            if (currentUser is not null && plainTextPassword == null)
            {
                _logger.LogInformation("GOOGLE AUTH");
                var tokenForGoogleUser = await _tokenService.GenerateJwtAccesToken(currentUser);
                var refreshTokenForGoogleUser = _tokenService.RefreshToken() ;
                loginDto.TokenProp = tokenForGoogleUser;
                loginDto.RoleProp = currentUser.Rol;
                loginDto.RefreshTokenProp = refreshTokenForGoogleUser;
                newRefreshToken = new RememberUser
                {
                    IdCont = currentUser.IdCont,
                    SessionToken = refreshTokenForGoogleUser,
                    IssuedAt = DateTime.UtcNow,
                    ExpiresAt = expiringTime
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
            
            var findRefreshTokenInDb = await _unitOfWork.Repository<RememberUser>()
                .FindQueryable(token => token.IdCont == currentUser.IdCont)
                .FirstOrDefaultAsync();

            if (findRefreshTokenInDb != null)
            {
                var newExpirationTime = DateTime.UtcNow.AddDays(7);
                findRefreshTokenInDb.IssuedAt = DateTime.UtcNow;
                findRefreshTokenInDb.ExpiresAt = newExpirationTime;
                await rememberUserRepository.UpdateAsync(findRefreshTokenInDb);
                await _unitOfWork.CommitTransactionAsync(addRefreshTokenTransaction);
            
                var tokenForUser = await _tokenService.GenerateJwtAccesToken(currentUser);
                return new LoginDto
                {
                    TokenProp = tokenForUser,
                    RoleProp = currentUser.Rol,
                    RefreshTokenProp = findRefreshTokenInDb.SessionToken
                };
            }
            

            var tokenForCurrentUser = await _tokenService.GenerateJwtAccesToken(currentUser);
            var refreshTokenForCurrentUser = _tokenService.RefreshToken() ;
            loginDto.TokenProp = tokenForCurrentUser;
            loginDto.RoleProp = currentUser.Rol;
            loginDto.RefreshTokenProp = refreshTokenForCurrentUser;

           
            newRefreshToken = new RememberUser
            {
                IdCont = currentUser.IdCont,
                SessionToken = refreshTokenForCurrentUser,
                IssuedAt = DateTime.UtcNow,
                ExpiresAt = expiringTime
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
            var getDockerEnv = Environment.GetEnvironmentVariable("DOCKER");
            var siteUrl = getDockerEnv != "true" ? "http://localhost:3000" : "https://www.texxshop.ro";
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
                
                if (_cache.TryGetValue(cacheKey, out ConturiDto? _))
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

            var url = await _bucketAcces.GenerateUrl("LogoTexx.png" , null);
            var insertLogo = url != null
                ? $"<mj-section>\n" +
                  $" <mj-column>\n" +
                  $"   <mj-image width=\"100px\" src=\"{url}\" alt=\"Company Logo\"/>\n" +
                  $" </mj-column>\n" +
                  $"</mj-section>"
                : "";
            
            var mjmlTemplate = $@"
            <mjml>
              <mj-body>
                {insertLogo}
                <mj-section>
                  <mj-column>
                    <mj-text font-size='18px' color='#F45E43' font-family='helvetica' align='center'>
                      Schimbare Email Cont / Email Change Notification
                    </mj-text>
                    <mj-spacer></mj-spacer>
                  </mj-column>
                  <mj-column background-color='#a8a8a8' border-radius='20px' padding='20px' width='100%'>

                     <mj-text font-size='22px' color='#F45E43'>RO</mj-text>
                     <mj-text font-size='18px' color='#333333'><strong>Schimbare Email Cont</strong></mj-text>
                     <mj-text font-size='18px' color='black'>V-ați schimbat e-mail-ul contului dumneavoastră.</mj-text>
                     <mj-text font-size='18px' color='black'><strong>Noul email este:</strong> {updatedDto.Email}</mj-text>
                     <mj-button color='white' background-color='black'>
                       <a href='{siteUrl}/user/account/changeEmail/{token}?email={updatedDto.Email}&nume={updatedDto.Nume}&prenume={updatedDto.Prenume}&gen={updatedDto.Gen}&nrTelefon={updatedDto.NrTelefon}&username={updatedDto.Username}'>
                         Confirmă schimbarea email-ului
                       </a>
                     </mj-button>
                     <mj-text font-size='18px' color='black'>Toate ofertele și contactul vor fi realizate pe noul email setat.</mj-text>
                     <mj-text font-size='18px' color='black'>Vă așteptăm la cumpărături pe site-ul nostru!</mj-text>
                     <mj-text font-size='18px' color='red' font-weight='bold'>ATENȚIE!</mj-text>
                     <mj-text font-size='18px' color='black'>Dacă nu ați solicitat această schimbare, contactați-ne cât mai curând posibil.</mj-text>
                     <mj-text font-size='18px' font-weight='bold'>ACEST LINK VA EXPIRA ÎNTR-O ORĂ.</mj-text>

                     <mj-divider border-color='#F45E43' padding='20px 0'/>

                     <mj-text font-size='22px' color='#F45E43'>EN</mj-text>
                     <mj-text font-size='18px' color='#333333'><strong>Email Change Notification</strong></mj-text>
                     <mj-text font-size='18px' color='black'>You have changed your account email.</mj-text>
                     <mj-text font-size='18px' color='black'><strong>Your new email is:</strong> {updatedDto.Email}</mj-text>
                     <mj-button color='white' background-color='black'>
                       <a href='{siteUrl}/user/account/changeEmail/{token}?email={updatedDto.Email}&nume={updatedDto.Nume}&prenume={updatedDto.Prenume}&gen={updatedDto.Gen}&nrTelefon={updatedDto.NrTelefon}&username={updatedDto.Username}'>
                         Confirm Email Change
                       </a>
                     </mj-button>
                     <mj-text font-size='18px' color='red' font-weight='bold'>WARNING!</mj-text>
                     <mj-text font-size='18px' color='black'>If you did not request this change, please contact us as soon as possible.</mj-text>
                     <mj-text font-size='18px' font-weight='bold'>THIS LINK WILL EXPIRE IN ONE HOUR.</mj-text>

                  </mj-column>
                </mj-section>
              </mj-body>
            </mjml>";

            var convertToHtml = await _mjmlService.ConvertMjmlToHtml(mjmlTemplate);
            
           await _emailService.SendEmailAsync(updatedDto.Email!,
                "Schimbare email cont texx.ro / Email Change Notification from texx.ro",
                convertToHtml!
            );


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
                
            if (_cache.TryGetValue(cacheKey, out ConturiDto? _))
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
            var response = await client.PostAsync(ReCaptchaUrl, parameters);
            
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
                _logger.LogError($"reCAPTCHA verification failed. Error codes: {string.Join(", ", recaptchaResult!.ErrorCodes)}");
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
            var getDockerEnv = Environment.GetEnvironmentVariable("DOCKER");
            var siteUrl = getDockerEnv != "TRUE" ? "http://localhost:3000" : "https://www.texxshop.ro";
            
            var url = await _bucketAcces.GenerateUrl("LogoTexx.png" , null);
            var insertLogo = url != null
                ? $"<mj-section>\n" +
                  $" <mj-column>\n" +
                  $"   <mj-image width=\"100px\" src=\"{url}\" alt=\"Company Logo\"/>\n" +
                  $" </mj-column>\n" +
                  $"</mj-section>"
                : "";
            
            var mjmlTemplate = $@"
            <mjml>
              <mj-body>
                {insertLogo}
                <mj-section>
                  <mj-column>
                    <mj-text font-size='18px' color='#F45E43' font-family='helvetica' align='center'>
                      Resetare Parolă / Password Reset
                    </mj-text>
                    <mj-spacer></mj-spacer>
                  </mj-column>
                  <mj-column background-color='#a8a8a8' border-radius='20px' padding='20px' width='100%'>
                     <mj-text font-size='22px' color='#F45E43'>RO</mj-text>
                     <mj-text font-size='18px' color='#333333'>
                       <strong>Ați solicitat o resetare a parolei.</strong>
                     </mj-text>
                     <mj-text font-size='18px' color='blue'>
                       Dacă nu ați solicitat resetarea parolei sau nu recunoașteți acest e-mail, NU dați click pe nimic! Contactați-ne la <strong>texx@yahoo.com</strong>
                     </mj-text>
                     <mj-text font-size='18px' color='#333333'>
                       Dați click pe acest buton pentru a vă reseta parola. Linkul expiră într-o oră!
                     </mj-text>
                     <mj-button color='white' background-color='black'>
                       <a href='{siteUrl}/user/forgotpassword/{resetToken}'>RESETARE PAROLĂ</a>
                     </mj-button>
                     <mj-text font-size='22px' color='#F45E43'>EN</mj-text>
                     <mj-text font-size='18px' color='#333333'>
                       <strong>You have requested a password reset.</strong>
                     </mj-text>
                     <mj-text font-size='18px' color='blue'>
                       If you did not request this reset, do not click ANYTHING! Contact us as soon as possible at <strong>texx@yahoo.com</strong>
                     </mj-text>
                     <mj-text font-size='18px' color='#333333'>
                       Click on the button below to reset your password. The link expires in one hour!
                     </mj-text>
                     <mj-button color='white' background-color='black'>
                       <a href='{siteUrl}/en/user/forgotpassword/{resetToken}'>RESET PASSWORD</a>
                     </mj-button>
                  </mj-column>
                </mj-section>
              </mj-body>
            </mjml>";

            var convertToHtml = await _mjmlService.ConvertMjmlToHtml(mjmlTemplate);


            await _emailService.SendEmailAsync(accountToBeUpdated.Email!, "Resetare parola / Password reset",
                convertToHtml!);

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

            var newToken = UserHelpers.Token(Size, Size2, currentUser.Email!);
            currentUser.CodActivare = newToken;

            var currentDateTime = DateTime.UtcNow;
            currentUser.OraLinkConfirmare = currentDateTime;

            currentUser.Verificat = false;
            var getDockerEnv = Environment.GetEnvironmentVariable("DOCKER");
            var siteUrl = getDockerEnv != "TRUE" ? "http://localhost:3000" : "https://www.texxshop.ro";
            await conturiRepository.UpdateAsync(currentUser);
            await _unitOfWork.CommitTransactionAsync(transaction);
            
            var url = await _bucketAcces.GenerateUrl("LogoTexx.png" , null);
            var insertLogo = url != null
                ? $"<mj-section>\n" +
                  $" <mj-column>\n" +
                  $"   <mj-image width=\"100px\" alt=\"Company Logo\"/>\n" +
                  $" </mj-column>\n" +
                  $"</mj-section>"
                : "";
            
            

            var mjmlTemplate = $"<mjml>\n" +
                               $"  <mj-body>\n  " +
                               $"{insertLogo}" +
                               $"  <mj-section>\n   " +
                               $"   <mj-column>\n      " +
                               $"  <mj-text font-size=\"18px\" color=\"#F45E43\" font-family=\"helvetica\" align=\"center\">Confirmare cont / Account confirmation</mj-text>\n       " +
                               $" <mj-spacer></mj-spacer>\n " +
                               $"     </mj-column>\n" +
                               $"      <mj-column background-color=\"#a8a8a8\" border-radius=\"20px\" padding=\"20px\" width=\"100%\">\n " +
                               $"        <mj-text font-size=\"22px\" color=\"#F45E43\">\n " +
                               $"         RO\n" +
                               $"        </mj-text>\n " +
                               $"        <mj-text font-size=\"18px\" color=\"#333333\">\n " +
                               $"         Acest mail expira intr-o ora!\n" +
                               $"        </mj-text>\n " +
                               $"       <mj-text font-size=\"18px\" color=\"#333333\">\n  " +
                               $"        <strong>Ati solicitat o noua confirmare a contului.</strong>\n" +
                               $"        </mj-text>\n        <mj-text font-size=\"18px\" color=\"blue\">\n" +
                               $"          In cazul in care nu ati fost dvs. sau nu recunoasteti acest mail, NU dati click pe nimic! Contacti-ne in cel mai scurt timp la <strong >texx@yahoo.com</strong>\n" +
                               $"        </mj-text>\n" +
                               $"       \t<mj-text font-size=\"18px\" color=\"#333333\">\n" +
                               $"          Noul cod de reactivare. Da click pe acest link pentru a-ti activa contul\n" +
                               $"         </mj-text>\n" +
                               $"          <mj-button color=\"white\" background-color=\"black\">\n" +
                               $"           <a href=\"{siteUrl}/user/confirmare/{newToken}\">CLICK</a>\n" +
                               $"        </mj-button>\n" +
                               $"         <mj-text font-size=\"22px\" color=\"#F45E43\">\n" +
                               $"          EN\n " +
                               $"       </mj-text>\n   " +
                               $"        <mj-text font-size=\"18px\" color=\"#333333\">\n " +
                               $"        This mail expires in 1 hour!\n" +
                               $"        </mj-text>\n " +
                               $"     <mj-text font-size=\"18px\" color=\"#333333\">\n " +
                               $"         <strong>New confirmation request to activate account.</strong>\n  " +
                               $"      </mj-text>\n  " +
                               $"      <mj-text font-size=\"18px\" color=\"blue\">\n " +
                               $"         If you did not request this confirmation , do not click ANYTHING! Contact us as fast as possible at <strong >texx@yahoo.com</strong>\n  " +
                               $"      </mj-text>\n " +
                               $"      \t<mj-text font-size=\"18px\" color=\"#333333\">\n " +
                               $"         New reactivation code. Click on the button\n " +
                               $"        </mj-text>\n" +
                               $"          <mj-button color=\"white\" background-color=\"black\">\n " +
                               $"          <a href=\"{siteUrl}/en/user/confirmare/{newToken}\">CLICK</a>\n" +
                               $"        </mj-button>\n\n" +
                               $"      </mj-column>\n" +
                               $"    </mj-section>\n" +
                               $"  </mj-body>\n" +
                               $"</mjml>";

            var convertToHtml = await _mjmlService.ConvertMjmlToHtml(mjmlTemplate);
            
            await _emailService.SendEmailAsync(currentUser.Email!, "Link nou de confirmare / New confirmation link",
                convertToHtml!);

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
        _logger.LogInformation("Gathering client data");
        var accountData = await _unitOfWork.Repository<Conturi>()
            .FindQueryable(a => a.IdCont == accountId)
            .Include(a => a.AdreseConturi)!
            .ThenInclude(df => df.DetaliuFactura)
                .Include(a => a.AdreseConturi)!
            .ThenInclude(l => l.Locatie)
            .AsSplitQuery()
                .FirstOrDefaultAsync();

        if (accountData == null)
        {
            return null;
        }
        
        var contDataToDto = _mapper.Map<ConturiDtoForModification>(accountData);
        var adreseToDto = _mapper.Map<IList<AdreseDto>>(accountData.AdreseConturi);
        contDataToDto.AdreseClient.AddRange(adreseToDto);
        
        
        var clientOrders = await _unitOfWork.Repository<Conturi>()
                .FindQueryable(account => account.IdCont == accountId)
                .SelectMany(address => address.AdreseConturi!)
                .SelectMany(order => order.AdreseLivrarePeComanda!)
                .AsSplitQuery()
                .Select(order => new ClientOrder
                {
                    Items = order.PcComenzi!
                        .GroupBy(group => group.IdentificatorSet != "21" ? group.IdentificatorSet : group.IdProduseCuComenzi.ToString())
                        .Select(item => new GroupedCartItems
                        {
                            Key = item.Key,
                            CartItems = item.Select(product => new CartItems
                            {
                                IdProdus = product.Produs.IdProdus,
                                IdSet = product.Set!.IdSet,
                                NumeSet = product.Set!.NumeSetJson.NumeRomana,
                                CodProdus = product.Produs.CodProdus,
                                NumeProdus =  product.Produs.NumeProdusJson.NumeRomana,
                                TipProdus = product.Produs.TipulProdusuluiJson.TipProdusRomana,
                                CuloareSelectata = new CuloriDto
                                {
                                    IdCuloare = product.PcCuloare.IdCuloare,
                                    NumeCuloareDto = product.PcCuloare.NumeCuloareJson.CuloareRomana,
                                    CodCuloareDto = product.PcCuloare.CodCuloare.CodCuloare!,
                                    JustAdded = false,
                                    ImaginiProdusDto = product.Produs.PProduseCuCulori!
                                        .FirstOrDefault(pc => pc.ImagProduseCuCulori!.Count > 0)!
                                        .ImagProduseCuCulori!.Select(imag => new ImagesDto
                                        {
                                            CaleImagineDto = imag.CaleImagine!,
                                            FisierInBucketDto = imag.FisierInBucket,
                                            PresignedUrl = "empty",
                                            JustAdded = false,
                                            IdProdusCuCuloareDto = 0
                                        }).Take(1).OrderBy(c => c.CaleImagineDto)
                                        .ToList()
                                },
                                DimensiuneSelectata = new DimensiuniDto
                                {
                                    IdDimensiune = product.PcDimensiune!.IdDimensiune,
                                    LungimeDto = product.PcManopera!.NumeManoperaJson!.NumeRomana != "STAN" ?  product.PcDimensiune.Lungime : ((int)(product.PcManopera.MaterialFolosit * 100)).ToString(),
                                    LatimeDto = product.PcDimensiune.Lungime,
                                    RecomandarePat = product.PcDimensiune.RecomandarePat,
                                    PretDto = 0,
                                    PretRedusDto = 0,
                                    JustAdded = false,
                                    PerdeaEstePerecheDto = product.PcManopera!.NumeManoperaJson.NumeRomana == "STAN" ? null : product.PcDimensiune.PerdeaEstePereche 
                                        
                                },
                                SelectedManopera = product.Produs.TipulProdusuluiJson.TipProdusRomana == "perdea" || product.Produs.TipulProdusuluiJson.TipProdusRomana == "draperie" ? new StandardManopereOnSet
                                {
                                    IdManopera = product.PcManopera!.IdManopera,
                                    NumeManopera = product.PcManopera.NumeManoperaJson.NumeRomana,
                                    MetruTotalFolosit = product.PcManopera.MaterialFolosit,
                                    InaltimeMaxima = product.PcManopera.InaltimeMaxima,
                                    TipInel = product.PcManopera.InelPrindereLaManopera != null ? new TipIneleDto
                                    {
                                        IdInelPrindere = product.PcManopera.InelPrindereLaManopera.IdInel,
                                        NumeTipInel = product.PcManopera.InelPrindereLaManopera.CuloareInelJson.CuloareRomana,
                                        CaleRelativa = product.PcManopera.InelPrindereLaManopera.CaleRelativa,
                                        PresignedUrl = "empty"
                                    } : null,
                                    TipGalerie = new TipRejansaDto
                                    {
                                        IdRejansa = product.PcManopera.TipGalerieLaManopera.IdTipGalerie,
                                        NumeTipRejansa = product.PcManopera.TipGalerieLaManopera.NumeTipGalerieJson.NumeRomana,
                                        PretTipRejansa = product.PcManopera.TipGalerieLaManopera.PretTipGalerie
                                           ,
                                        IncretireRejansa = product.PcManopera.TipGalerieLaManopera.IncretireRejansa,
                                        CaleRelativa = product.PcManopera.TipGalerieLaManopera.CaleRelativa,
                                        PresignedUrl = "empty",
                                        SePrindeCuInele = product.PcManopera.TipGalerieLaManopera.SePrindeCuInele
                                    },
                                    TipLinie = new TipLinieDto
                                    {
                                        IdTipLinie = product.PcManopera.TipLinieLaManopera.IdTipLinie,
                                        NumeTipCusaturaColt = product.PcManopera.TipLinieLaManopera.NumeTipLinieJson.NumeRomana,
                                        PretTipCusaturaColt =  product.PcManopera.TipLinieLaManopera.PretPeTipLinie,
                                        CaleRelativa = product.PcManopera.TipLinieLaManopera.CaleRelativa,
                                        PresignedUrl = "empty"
                                    }
                                } : null,
                                LungimeCeruta = product.PcManopera != null ?
                                    product.PcManopera.NumeManoperaJson.NumeRomana == "STAN" ? product.PcManopera.MaterialFolosit.ToString() : "empty"
                                : "not_perdea",
                                InaltimeCeruta = product.IdSet != null ? product.InaltimeAleasaPentruSet : "not_set",
                                PretCurent =  product.PretCumparat ,
                                Cantitate = product.NrBucati,
                                IdentificatorSet = item.Key  
                            }).ToList()
                        }).ToList(),
                    ClientDeliveryAddress = new AdreseDto
                    {
                        AliasDto = order.CAdresaLivrare.Alias,
                        TipAdresaDto = TipAdrese.Livrare,
                        BlocDto = order.CAdresaLivrare.Bloc,
                        NrBlocDto = order.CAdresaLivrare.NrBloc,
                        StradaDto = order.CAdresaLivrare.Strada,
                        NrStradaDto = order.CAdresaLivrare.NrStrada,
                        OrasDto = order.CAdresaLivrare.Locatie.Oras!,
                        JudetDto = order.CAdresaLivrare.Locatie.Judet!,
                        CodPostalDto = order.CAdresaLivrare.Locatie.CodPostal!,
                        IsDeletedDto = order.CAdresaLivrare.IsDeleted,
                        CifDto = null,
                        NumeFirmaDto = null
                    },
                    ClientBillingAddress = new AdreseDto
                    {
                        AliasDto = order.CAdresaFacturare.Alias,
                        TipAdresaDto = TipAdrese.Facturare,
                        BlocDto = order.CAdresaFacturare.Bloc,
                        NrBlocDto = order.CAdresaFacturare.NrBloc,
                        StradaDto = order.CAdresaFacturare.Strada,
                        NrStradaDto = order.CAdresaFacturare.NrStrada,
                        OrasDto = order.CAdresaFacturare.Locatie.Oras!,
                        JudetDto = order.CAdresaFacturare.Locatie.Judet!,
                        CodPostalDto = order.CAdresaFacturare.Locatie.CodPostal!,
                        IsDeletedDto = order.CAdresaFacturare.IsDeleted,
                        CifDto = order.CAdresaFacturare.DetaliuFactura!.Cif,
                        NumeFirmaDto = order.CAdresaFacturare.DetaliuFactura!.Cif
                    },
                    UserOrderDetails = new UserPersonalInfo
                    {
                        Nume = order.NumePeComanda,
                        Prenume = order.PrenumePeComanda,
                        NrTelefon = order.NrTelefonPeComanda,
                        Email = order.EmailPeComanda
                    },
                    OrderDate = order.DataEmitereComanda,
                    OrderId = order.IdComanda,
                    OrderStatus = order.StatusComanda,
                    OrderPayment = order.TipPlata,
                    OrderTrackingString = order.AwbComanda,
                    IsCancelableDto = order.IsCancelable,
                    OrderVoucher = order.IdVoucher != null ? new VouchereDto
                    {
                        CodVoucherDto = order.VoucherPeComanda!.CodVoucher,
                        ReducereDto = order.VoucherPeComanda.Reducere,
                        DataExpirareDto = default
                    } : null,
                    PretTotal = 0,
                    TotalProduse = 0,
                }).ToListAsync();
        
            // decimal cartTotal = 0;
            // var totalProducts = 0;
        
            
            foreach (var order in clientOrders)
            {
                // _logger.LogInformation($"orderId => {order.OrderId}");
                foreach (var item in order.Items)
                {
                    var isSet = !int.TryParse(item.Key, out _);
                    var exit = false; // true if end or false if not
                    foreach (var cartItem in item.CartItems)
                    {
                        switch (isSet)
                        {
                            // if we found the product!
                            case false:
                                order.PretTotal += item.CartItems[0].Cantitate * item.CartItems[0].PretCurent;
                                order.TotalProduse += item.CartItems[0].Cantitate;
                                break;
                            // else we found a set , and we only count once!
                            case true when !exit:
                                order.PretTotal += item.CartItems[0].Cantitate * item.CartItems[0].PretCurent;
                                order.TotalProduse += item.CartItems[0].Cantitate;
                                exit = true;
                                break;
                        }

                        if (cartItem.CuloareSelectata.ImaginiProdusDto!.Count > 0)
                        {
                            foreach (var image in cartItem.CuloareSelectata.ImaginiProdusDto)
                            {
                                image.PresignedUrl = await _bucketAcces.GenerateUrl(image.CaleImagineDto, image.FisierInBucketDto);
                            }
                        }
                        
                        
                        if (cartItem.SelectedManopera == null) continue;
                        
                        if (cartItem.SelectedManopera.TipInel != null)
                        {
                            if(cartItem.SelectedManopera.TipInel.CaleRelativa == null) continue;
                            cartItem.SelectedManopera.TipInel.PresignedUrl = await
                                _bucketAcces.GenerateUrl(cartItem.SelectedManopera.TipInel.CaleRelativa!, "inele_prindere");
                        }
                        
                        if(cartItem.SelectedManopera.TipGalerie.CaleRelativa == null) continue;
                        cartItem.SelectedManopera.TipGalerie.PresignedUrl = await
                            _bucketAcces.GenerateUrl(cartItem.SelectedManopera.TipGalerie.CaleRelativa!, "tipuri_galerie");
                        
                        if(cartItem.SelectedManopera.TipLinie.CaleRelativa == null) continue;
                        cartItem.SelectedManopera.TipLinie.PresignedUrl = await
                            _bucketAcces.GenerateUrl(cartItem.SelectedManopera.TipLinie.CaleRelativa!, "tipuri_linie");
                    }
                }
            }

            contDataToDto.ComenziClient = clientOrders;
            return contDataToDto;
    }
}
