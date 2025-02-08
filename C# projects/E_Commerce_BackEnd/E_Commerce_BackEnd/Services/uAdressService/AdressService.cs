using AutoMapper;
using E_Commerce_BackEnd.Models.DTO;
using E_Commerce_BackEnd.Models.Enums;
using E_Commerce_BackEnd.Models.OrderRelatedModels;
using E_Commerce_BackEnd.Models.UserRelatedModels;
using E_Commerce_BackEnd.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;

namespace E_Commerce_BackEnd.Services.uAdressService;

public class AdressService : IAdressService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<Adrese> _logger;
    private readonly IMemoryCache _cache;
    private readonly IMapper _mapper;

    public AdressService(IUnitOfWork unitOfWork, ILogger<Adrese> logger, IMemoryCache cache, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _cache = cache;
        _mapper = mapper;
    }

    public async Task<IList<AdreseDto>?> GetAllUsersAdresses(int userId)
    {
        var getAllUserAddresses = await _cache.GetOrCreateAsync($"Adrese_{userId}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            var adressRepository =  _unitOfWork.Repository<Adrese>();
            var allUserAdressess = await adressRepository
                .FindQueryable(a => a.IdCont == userId )
                .Include(a => a.DetaliuFactura)
                .Include(a => a.Locatie)
                .ToListAsync();
            
            if (allUserAdressess.Count < 0)
            {
                return null; // no addreses
            }
            
            var convertToDto = _mapper.Map<IList<AdreseDto>>(allUserAdressess);
            return convertToDto;
        });
        
        return getAllUserAddresses;
        
    }
    
    // helper
    private async void AddNewAddressToCache(AdreseDto newAddressDto , int userId)
    {
       
        var cacheKey = $"Adrese_{userId}";
        
        var getUserAddresses = await _cache.GetOrCreateAsync($"Adrese_{userId}" , async entry => await GetAllUsersAdresses(userId));


        if (getUserAddresses == null) return;
        getUserAddresses.Add(newAddressDto);
            
        _cache.Set(cacheKey, getUserAddresses, TimeSpan.FromMinutes(5));
        // else
        // {
        //     // If not in cache, fetch from DB and set cache
        //     var allUserAddressesQueryable = await _unitOfWork.Repository<Adrese>().FindQueryableOfEntitiesAsync(
        //         a => a.IdCont == userId && a.IsDeleted == false,
        //         a => a.Locatie,
        //         a => a.DetaliuFactura!);
        //
        //     if (allUserAddressesQueryable == null)
        //     {
        //         return;
        //     }
        //
        //     var allUserAddresses = _mapper.Map<AdreseDto>(await allUserAddressesQueryable.ToListAsync()) ;
        //     
        //     _cache.Set(cacheKey, allUserAddresses, TimeSpan.FromMinutes(10));
        // }

    }

    public async Task<int> SaveAddress(AdreseDto adressDto, int userId)
    {
        IDbContextTransaction transaction = null!;
        try
        {
            transaction = await _unitOfWork.BeginTransactionAsync();
        
        
            var adreseRepository = _unitOfWork.Repository<Adrese>();
            var conturiRepository = _unitOfWork.Repository<Conturi>();
            var locationsRepository = _unitOfWork.Repository<Locatii>();
            var detaliiFacturaRepository = _unitOfWork.Repository<DetaliiFactura>();
            
            
            var currentUserLoggedIn = await conturiRepository.GetByIdAsync(userId);
            var currentLocatie = await locationsRepository.FindQueryable(a => a.Oras == adressDto.OrasDto &&
                a.Judet == adressDto.JudetDto && a.CodPostal == adressDto.CodPostalDto).FirstOrDefaultAsync();


            if (currentLocatie == null)
            {
                var newLocation = new Locatii
                (
                    adressDto.OrasDto,
                    adressDto.JudetDto,
                    adressDto.CodPostalDto,
                    new HashSet<Adrese>()
                );

                await locationsRepository.AddAsync(newLocation);
                await _unitOfWork.CommitAsync();

                currentLocatie = newLocation;
                
            }

            Adrese addressToBeSaved;

            if (adressDto.TipAdresaDto == TipAdrese.Livrare)
            {
                addressToBeSaved = new Adrese
                {
                    Alias = adressDto.AliasDto,
                    TipAdresa = adressDto.TipAdresaDto,
                    Bloc =  adressDto.BlocDto,
                    NrBloc = adressDto.NrBlocDto,
                    Strada =  adressDto.StradaDto,
                    NrStrada = adressDto.NrStradaDto,
                    IsDeleted = false,
                    IdLocatie = currentLocatie.IdLocatie,
                    IdCont = currentUserLoggedIn!.IdCont,
                    IdDetaliuFactura = null
                };
            }
            else
            {
                var billingDetalils = new DetaliiFactura
                {
                    Cif = adressDto.CifDto,
                    NumeFirma = adressDto.NumeFirmaDto,
                };
            

                await detaliiFacturaRepository.AddAsync(billingDetalils);
                await _unitOfWork.CommitAsync();

                addressToBeSaved = new Adrese
                {
                    Alias = adressDto.AliasDto,
                    TipAdresa = adressDto.TipAdresaDto,
                    Bloc = adressDto.BlocDto,
                    NrBloc = adressDto.NrBlocDto,
                    Strada = adressDto.StradaDto,
                    NrStrada = adressDto.NrStradaDto,
                    IsDeleted = false,
                    IdLocatie = currentLocatie.IdLocatie,
                    IdCont = currentUserLoggedIn!.IdCont,
                    IdDetaliuFactura = billingDetalils.IdDetaliu
                };
            }
            
            await adreseRepository.AddAsync(addressToBeSaved);
            await _unitOfWork.CommitTransactionAsync(transaction);
            
            AddNewAddressToCache(_mapper.Map<AdreseDto>(addressToBeSaved) , userId);
            
            _logger.LogInformation("Adress succesfully saved!");
            
            
            return 1;

        }
        catch (Exception e)
        {
            if (transaction == null!)
            {
                _logger.LogCritical("Adding adress failed , rolling back transaction");
                await _unitOfWork.RollBackTransactionAsync(transaction!);
            }
            _logger.LogError(e.StackTrace);
            return -1;
        }
       
    }
    
    public async Task<int> ModifyAddress(int userId, AdreseDto newAddressData)
    {
        IDbContextTransaction? modifyTransaction = null;
        try
        {
            modifyTransaction = await _unitOfWork.BeginTransactionAsync();
            
            
            var userAddresses = await _cache.GetOrCreateAsync($"Adrese_{userId}", async entry => await GetAllUsersAdresses(userId));

            if (userAddresses!.Count < 0)
            {
                return -3;
            }

            // db modification
            var addressToModifyInDb = await _unitOfWork.Repository<Adrese>()
                .FindQueryable(a => a.IdCont == userId
                                    && a.IdAdresa == newAddressData.IdAdresa)
                .FirstOrDefaultAsync();

            if (addressToModifyInDb == null)
            {
                return -3; // Address does not exist
            }

            _mapper.Map(newAddressData, addressToModifyInDb);
            if (!newAddressData.CifDto.IsNullOrEmpty() && !newAddressData.NumeFirmaDto.IsNullOrEmpty())
            {
                var isBillingDetailInDb = await _unitOfWork.Repository<DetaliiFactura>()
                    .FindQueryable(a => a.Cif == newAddressData.CifDto
                                        && a.NumeFirma!.ToLower() == newAddressData.NumeFirmaDto!.ToLower())
                    .FirstOrDefaultAsync();

                if (isBillingDetailInDb == null)
                {
                    var newBillingDetail = new DetaliiFactura
                    {
                        Cif = newAddressData.CifDto,
                        NumeFirma = newAddressData.NumeFirmaDto
                    };

                    await _unitOfWork.Repository<DetaliiFactura>().AddAsync(newBillingDetail);
                    await _unitOfWork.CommitAsync();
                    isBillingDetailInDb = newBillingDetail;
                }

                addressToModifyInDb.IdDetaliuFactura = isBillingDetailInDb.IdDetaliu;
            }
            
            var isLocationInDb = await _unitOfWork.Repository<Locatii>()
                .FindQueryable(l => l.Oras!.ToLower() == newAddressData.OrasDto.ToLower()
                                    &&  l.Judet!.ToLower() == newAddressData.JudetDto.ToLower()
                                    && l.CodPostal! == newAddressData.CodPostalDto)
                .FirstOrDefaultAsync();

            if (isLocationInDb == null)
            {
                var newLocation = new Locatii
                {
                    Oras = newAddressData.OrasDto,
                    Judet = newAddressData.JudetDto,
                    CodPostal = newAddressData.CodPostalDto
                };

                await _unitOfWork.Repository<Locatii>().AddAsync(newLocation);
                await _unitOfWork.CommitAsync();
                isLocationInDb = newLocation;
            }

            addressToModifyInDb.IdLocatie = isLocationInDb.IdLocatie;

            await _unitOfWork.Repository<Adrese>().UpdateAsync(addressToModifyInDb);
           
            
            
            
            // cache modification
            var addressToModify = userAddresses
                .FirstOrDefault(a => a.IdAdresa == newAddressData.IdAdresa);

            if (addressToModify == null)
            {
                return -3;
            }

            _mapper.Map(newAddressData, addressToModify);
            addressToModify.CodPostalDto = newAddressData.CodPostalDto;
            addressToModify.OrasDto = newAddressData.OrasDto;
            addressToModify.JudetDto = newAddressData.JudetDto;
            addressToModify.CifDto = newAddressData.CifDto;
            addressToModify.NumeFirmaDto = newAddressData.NumeFirmaDto;
            _cache.Set($"Adrese_{userId}", userAddresses, TimeSpan.FromMinutes(5));
            _logger.LogInformation($"Modified succesfully address on user id => {userId}");
            await _unitOfWork.CommitTransactionAsync(modifyTransaction);
            return 1;
        }
        catch (Exception e)
        {
            if (modifyTransaction != null)
            {
                await _unitOfWork.RollBackTransactionAsync(modifyTransaction);
                _logger.LogError("Rolling back modifying address transaction ");
            }
            _logger.LogError(e.StackTrace);
            return -1;
        }
    }
}